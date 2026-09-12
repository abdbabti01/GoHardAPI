using System.Data;
using GoHardAPI.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GoHardAPI.Services
{
    /// <summary>
    /// The single, shared mechanism for keeping a <c>MealLog</c>'s and its
    /// <c>MealEntry</c> rows' cached nutrition totals (calories, protein, carbohydrates,
    /// fat, fiber, sodium) durably CONSISTENT with the <c>FoodItem</c> rows that are
    /// their actual source of truth - across every writer that mutates food items
    /// (<c>FoodItemsController</c>'s add/edit/quantity/replace/delete/consume actions,
    /// <c>MealEntriesController</c>'s consume/delete actions, the explicit
    /// <c>MealLogsController</c> recalc endpoint, and <c>ChatController</c>'s meal-plan
    /// apply endpoints).
    ///
    /// <para><b>Why the content change and the totals recompute must be ONE atomic
    /// unit, not two separate transactions</b> - an earlier version of this fix
    /// committed the food-item content change in one transaction and then recomputed
    /// totals in a SEPARATE, later, independently-retried transaction. That reintroduced
    /// exactly the failure mode it was meant to close: a crash (or an exhausted retry
    /// budget) between the two steps left committed food rows with a stale, never-
    /// corrected total, and returning an error for the second step did nothing to undo
    /// or repair the first. There is no repair job and no reliance on some later
    /// unrelated request to "happen to" fix it - the only correct fix is for the content
    /// mutation and the totals recompute to commit or roll back TOGETHER, as a single
    /// PostgreSQL transaction.</para>
    ///
    /// <para><b>Why retrying the WHOLE operation (not just the totals tail) is safe</b> -
    /// a transaction that PostgreSQL aborts with a confirmed <c>40001</c>
    /// (serialization_failure) or <c>40P01</c> (deadlock_detected) is GUARANTEED to have
    /// been rolled back in full - nothing from that attempt persisted, including any
    /// earlier steps of the same request (a nutrition-goal upsert, a food-item
    /// delete-then-insert, a planned-value delta). Re-running the entire operation from a
    /// completely fresh read is therefore exactly as safe as if the failed attempt had
    /// never been made - no partial re-application, no double-counted delta. This is also
    /// why only these two specific, confirmed-abort SQLSTATEs are ever retried: any OTHER
    /// exception (a dropped connection during commit, for instance) leaves the outcome
    /// genuinely uncertain, and blindly retrying a possibly-already-committed operation
    /// risks duplicating it - so it is never retried here, and instead propagates as a
    /// real failure.</para>
    ///
    /// <para><b>The guarantee this provides:</b> for every caller that uses
    /// <see cref="ExecuteAtomicallyAsync{T}"/>, either (a) the content change AND the
    /// resulting totals are durably committed together, correct as of that attempt's
    /// fresh read, or (b) nothing is committed at all - never content without its
    /// corresponding totals, regardless of how many attempts were exhausted or what
    /// concurrent writer was racing it.</para>
    /// </summary>
    /// <summary>
    /// The outcome of one <see cref="MealLogTotalsRecalculator.ExecuteAtomicallyAsync{T}"/>
    /// attempt delegate: either the target entity/entities were found and the mutation
    /// (plus its totals recompute) was staged for commit, or nothing was found and there
    /// is nothing to commit - the caller should report its own "not found" response
    /// without having mutated anything.
    /// </summary>
    public readonly struct MealMutationOutcome<T>
    {
        public bool Found { get; private init; }
        public T? Value { get; private init; }

        public static MealMutationOutcome<T> NotFound() => new() { Found = false, Value = default };
        public static MealMutationOutcome<T> Success(T value) => new() { Found = true, Value = value };
    }

    public static class MealLogTotalsRecalculator
    {
        /// <summary>
        /// Generous for a conflict window measured in milliseconds around a handful of
        /// row writes; exhausting this would mean the same meal log was contended three
        /// times in a row, which has not been observed even under the deliberately
        /// adversarial concurrency tests this class exists to satisfy.
        /// </summary>
        public const int DefaultMaxAttempts = 3;

        /// <summary>
        /// Thrown when every attempt of <see cref="ExecuteAtomicallyAsync{T}"/> hit a
        /// confirmed, retryable PostgreSQL conflict. By construction, when this is
        /// thrown, NOTHING from this call was committed - every attempt's transaction
        /// (the failing last one included) was rolled back before this propagates - so
        /// there is no content-committed-without-totals state to worry about; the caller
        /// only needs to report a clean, safe failure to its own caller.
        /// </summary>
        public sealed class ConcurrencyRetriesExhaustedException : Exception
        {
            public ConcurrencyRetriesExhaustedException(Exception innerException)
                : base("Exhausted all retry attempts after repeated PostgreSQL serialization conflicts.", innerException)
            {
            }
        }

        /// <summary>
        /// Runs <paramref name="attempt"/> as a single atomic unit: a fresh
        /// <see cref="IsolationLevel.Serializable"/> transaction per try, with
        /// <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.Clear"/>
        /// called before every attempt (including the first) so each one reads the
        /// database's CURRENT state rather than reusing any entity another part of the
        /// same request might already track. <paramref name="attempt"/> is responsible
        /// for its OWN complete unit of work for one try - re-reading whatever it needs,
        /// re-validating ownership/existence, applying its content mutation, and staging
        /// (via <see cref="StageRecalculationAsync"/>) whichever meal log(s) it touched -
        /// but must NOT call <see cref="TrainingContext.SaveChangesAsync(CancellationToken)"/>
        /// or commit/roll back the transaction itself; this method does both, once, after
        /// <paramref name="attempt"/> returns.
        ///
        /// On a confirmed <c>40001</c>/<c>40P01</c> conflict, the transaction is rolled
        /// back and the ENTIRE <paramref name="attempt"/> is retried from a fresh read -
        /// never just a sub-step of it. If every attempt is exhausted,
        /// <see cref="ConcurrencyRetriesExhaustedException"/> is thrown - and by
        /// construction nothing from any attempt was left committed.
        /// </summary>
        public static async Task<T> ExecuteAtomicallyAsync<T>(
            TrainingContext context,
            Func<CancellationToken, Task<T>> attempt,
            int maxAttempts = DefaultMaxAttempts,
            CancellationToken cancellationToken = default)
        {
            for (var attemptNumber = 1; attemptNumber <= maxAttempts; attemptNumber++)
            {
                // Discard anything this context already tracks (and its property values,
                // which may be stale relative to the database) so this attempt's reads are
                // genuinely fresh - critical on a retry, so it sees whatever a concurrent
                // winner just committed; harmless on the first attempt.
                context.ChangeTracker.Clear();

                using var transaction = await context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);
                try
                {
                    var result = await attempt(cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch (Exception ex) when (IsSerializationFailure(ex) && attemptNumber < maxAttempts)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    // Retry: the next attempt's ChangeTracker.Clear() + fresh read above
                    // will see whatever the winning side just committed, so redoing the
                    // whole operation converges on a correct result - no merge logic
                    // needed because it is a full, atomic redo, not a partial patch.
                }
                catch (Exception ex) when (IsSerializationFailure(ex))
                {
                    // Final attempt also hit a confirmed, safely-retryable conflict - but
                    // attempts are exhausted. The transaction above has already rolled
                    // back everything from this attempt (including any content mutation
                    // `attempt` performed), so there is nothing partially committed.
                    await transaction.RollbackAsync(cancellationToken);
                    throw new ConcurrencyRetriesExhaustedException(ex);
                }
            }

            // Unreachable: the loop above always returns or throws.
            throw new InvalidOperationException("MealLogTotalsRecalculator retry loop exited without returning or throwing.");
        }

        /// <summary>
        /// Stages (via the change tracker - does NOT itself call
        /// <c>SaveChangesAsync</c>) a recomputation of <paramref name="mealLogId"/>'s
        /// <c>MealEntry</c> totals (every entry, consumed or not - see remarks) and the
        /// parent <c>MealLog</c>'s own totals (summed from consumed entries only), from a
        /// query issued INSIDE the caller's ambient transaction/attempt. Must be called
        /// from within an <see cref="ExecuteAtomicallyAsync{T}"/> attempt, after any
        /// content mutation for this meal log has already been applied (and, if the
        /// caller needs generated IDs from it, already saved via an intermediate
        /// <c>SaveChangesAsync</c> within the SAME attempt/transaction) - so this query
        /// observes that mutation via EF Core's own change tracking and read-your-own-
        /// writes within the transaction, not by re-reading from a different connection.
        /// A missing meal log (already deleted, or never existed) is a no-op.
        ///
        /// Recomputed for every entry, consumed or not: a consumed entry's FoodItems are
        /// never touched by the meal-plan apply endpoints (they skip a consumed entry's
        /// CONTENT entirely, separately from this recompute), so recomputing its total
        /// here is a harmless no-op that just re-derives the same value from its
        /// unchanged rows. For an ordinary food edit, a consumed entry's own FoodItems CAN
        /// legitimately be edited (correcting an already-logged meal), and its total must
        /// reflect that edit.
        /// </summary>
        public static async Task StageRecalculationAsync(
            TrainingContext context,
            int mealLogId,
            CancellationToken cancellationToken = default)
        {
            var mealLog = await context.MealLogs
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                .FirstOrDefaultAsync(ml => ml.Id == mealLogId, cancellationToken);

            if (mealLog == null)
            {
                return;
            }

            foreach (var entry in mealLog.MealEntries)
            {
                entry.RecalculateTotals();
            }

            // MealLog totals only ever count consumed meals.
            mealLog.RecalculateTotals(consumedOnly: true);
        }

        /// <summary>
        /// True for a transient, safe-to-retry PostgreSQL conflict: <c>40001</c>
        /// (serialization_failure - the expected Serializable-isolation conflict this
        /// class is built around) or <c>40P01</c> (deadlock_detected - possible in
        /// principle since a meal log's entries are updated with no fixed lock order, so
        /// two concurrent attempts for the same meal log could issue their per-row
        /// updates in different orders). Both are PostgreSQL's own explicit,
        /// synchronous-protocol confirmation that the transaction was fully rolled back
        /// and nothing was corrupted - fundamentally different from an exception whose
        /// commit outcome is uncertain (e.g. a dropped connection), which must NEVER be
        /// retried blindly and is deliberately not matched here.
        /// </summary>
        public static bool IsSerializationFailure(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current is PostgresException pg && (pg.SqlState == "40001" || pg.SqlState == "40P01"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
