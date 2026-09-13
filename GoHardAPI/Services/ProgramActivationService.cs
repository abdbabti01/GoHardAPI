using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GoHardAPI.Services
{
    public enum ProgramActivationOutcome
    {
        Activated,
        AlreadyActive,
        NotDraft,
        NoWorkouts,
        NoExercises,
        Stale,
    }

    public sealed class ProgramActivationResult
    {
        public ProgramActivationOutcome Outcome { get; init; }
        public Models.Program? Program { get; init; }
        public string? Message { get; init; }
    }

    /// <summary>
    /// Single source of truth for turning a Draft <see cref="Models.Program"/> (created once,
    /// deterministically, from AI-generated or manually-entered structured data) into an Active
    /// one. Used by both the direct <c>Programs/{id}/activate</c> route and the AI-chat
    /// "Create Program" route (<c>ChatController.CreateProgramFromPlan</c>), so there is exactly
    /// one activation code path in the API and neither route re-runs AI or rebuilds workouts
    /// from scratch — the workouts were already fully materialized once, at draft-creation time.
    ///
    /// The Draft-to-Active transition is guarded by a conditional UPDATE (mirroring
    /// <c>ProgramsController.SwapWorkouts</c>'s existing raw-SQL pattern for atomic, race-safe
    /// state changes) rather than a new concurrency-token column: exactly one concurrent caller
    /// performs the transition, and every other caller (a duplicate tap, a retried lost
    /// response, or a genuine race) converges on the SAME activated program instead of erroring
    /// or creating a duplicate.
    /// </summary>
    public static class ProgramActivationService
    {
        public static async Task<ProgramActivationResult> ActivateAsync(
            TrainingContext context,
            Models.Program program,
            DateTime? startDateOverride,
            string? title,
            string? description,
            int? goalId,
            string? clientRevision = null,
            CancellationToken cancellationToken = default)
        {
            if (program.Status == ProgramStatus.Active.ToApiString())
            {
                return new ProgramActivationResult
                {
                    Outcome = ProgramActivationOutcome.AlreadyActive,
                    Program = program,
                };
            }

            if (program.Status != ProgramStatus.Draft.ToApiString())
            {
                return new ProgramActivationResult
                {
                    Outcome = ProgramActivationOutcome.NotDraft,
                    Message = "Only draft programs can be activated",
                };
            }

            await EnsureOccurrenceKeysHealedAsync(context, program.Workouts, cancellationToken);

            if (program.Workouts == null || program.Workouts.Count == 0)
            {
                return new ProgramActivationResult
                {
                    Outcome = ProgramActivationOutcome.NoWorkouts,
                    Message = "Cannot activate a program with no workouts",
                };
            }

            var workoutsWithExercises = program.Workouts
                .Where(w => !string.IsNullOrEmpty(w.ExercisesJson) && w.ExercisesJson != "[]")
                .ToList();

            if (workoutsWithExercises.Count == 0)
            {
                return new ProgramActivationResult
                {
                    Outcome = ProgramActivationOutcome.NoExercises,
                    Message = "Cannot activate a program with no exercises. Add exercises to at least one workout.",
                };
            }

            // Programs always start on Monday. Unlike the pre-Phase-2 ActivateDraftProgram, this
            // snap is unconditional: previously, omitting an override left the draft's placeholder
            // StartDate (and every workout's null ScheduledDate) untouched.
            var baseDate = (startDateOverride ?? program.StartDate).Date;
            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)baseDate.DayOfWeek + 7) % 7;
            var effectiveStartDate = baseDate.AddDays(daysUntilMonday);

            // The revision recheck, the conditional status flip, and the rest of the activation
            // fields must all commit or roll back together, under row locks held across the
            // whole span: a transaction around only the status flip and schedule fields does NOT
            // prove a concurrently-committed content edit can never become active. Without the
            // locking recheck below, this sequence is possible even with that transaction in
            // place: (1) the controller's pre-check reads program.Workouts and confirms the
            // client's revision matches; (2) a concurrent request commits an edit via
            // UpdateWorkout/AddWorkout/DeleteWorkout - a completely separate transaction that
            // this one's transaction boundary does nothing to exclude; (3) this method's own
            // transaction then commits Status=Active. The client that asked to activate the
            // reviewed content would get a success response while the row actually left Active
            // has the concurrently-edited content, never seen or approved. Locking the Program
            // row and then every ProgramWorkout row for it (below) closes that window: any
            // concurrent Add/Update/Delete on this program's workouts blocks until THIS
            // transaction ends, so the content re-read under lock is guaranteed to still be
            // whatever it was (or, if an edit was already in flight, its committed result -
            // correctly caught as stale) at the instant right before this transaction commits.
            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (!string.IsNullOrEmpty(clientRevision))
                {
                    var stillFresh = await IsRevisionStillFreshUnderLockAsync(
                        context, program, clientRevision, cancellationToken);

                    if (!stillFresh)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        return new ProgramActivationResult
                        {
                            Outcome = ProgramActivationOutcome.Stale,
                            Message = "This plan has changed since it was generated. Please refresh before activating.",
                        };
                    }
                }

                var rows = await context.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Programs\" SET \"Status\" = {0} WHERE \"Id\" = {1} AND \"Status\" = {2}",
                    ProgramStatus.Active.ToApiString(), program.Id, ProgramStatus.Draft.ToApiString());

                if (rows == 0)
                {
                    // Lost the race (or this is a duplicate/retried call after a prior request
                    // already won it): reload and return the SAME activated program rather than
                    // erroring or re-activating.
                    var current = await context.Programs
                        .Include(p => p.Workouts)
                        .AsNoTracking()
                        .FirstAsync(p => p.Id == program.Id, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    return new ProgramActivationResult
                    {
                        Outcome = ProgramActivationOutcome.AlreadyActive,
                        Program = current,
                    };
                }

                program.Status = ProgramStatus.Active.ToApiString();
                program.IsActive = true;
                program.StartDate = effectiveStartDate;
                program.EndDate = effectiveStartDate.AddDays(program.TotalWeeks * 7);

                if (!string.IsNullOrEmpty(title))
                {
                    program.Title = title;
                }
                if (!string.IsNullOrEmpty(description))
                {
                    program.Description = description;
                }
                if (goalId.HasValue)
                {
                    program.GoalId = goalId;
                }

                foreach (var workout in program.Workouts)
                {
                    workout.ScheduledDate = effectiveStartDate
                        .AddDays((workout.WeekNumber - 1) * 7 + (workout.DayNumber - 1))
                        .Date;
                }

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }

            return new ProgramActivationResult
            {
                Outcome = ProgramActivationOutcome.Activated,
                Program = program,
            };
        }

        public enum DraftFreshness
        {
            Fresh,
            Stale,
        }

        /// <summary>
        /// Deterministic content fingerprint of a draft's reviewable content — <see cref="Models.Program.TotalWeeks"/>
        /// plus, for every workout ordered by (week, day): whether it's a rest day, its name,
        /// type, description, estimated duration, warm-up/cool-down text, and its exact
        /// <see cref="ProgramWorkout.ExercisesJson"/>. Two calls against byte-identical content
        /// always produce the same value; ANY material edit — a regenerated plan, a workout
        /// added/removed/edited through <c>ProgramsController</c>'s ordinary workout endpoints, a
        /// different week count — changes it. Serialized as JSON (not delimiter-joined raw
        /// strings) so a free-text field containing a separator character can never shift a field
        /// boundary and mask a real edit. It deliberately does NOT include
        /// <see cref="Models.Program.StartDate"/>/<c>ScheduledDate</c>: the schedule's start date
        /// is meant to stay user-adjustable at confirmation time without invalidating the
        /// reviewed content identity.
        ///
        /// Requires <paramref name="program"/>.Workouts to be loaded (<c>Include(p =&gt; p.Workouts)</c>).
        /// No schema/persistence needed: this is computed fresh from current rows every time it's
        /// requested (at generation, at GET, and at activation), so "the client's remembered
        /// value" vs. "the server's current value" is exactly "what was shown" vs. "what exists
        /// now" — a real content comparison, not a proxy for one.
        /// </summary>
        public static string ComputeContentRevision(Models.Program program)
        {
            var ordered = (program.Workouts ?? Enumerable.Empty<ProgramWorkout>())
                .OrderBy(w => w.WeekNumber)
                .ThenBy(w => w.DayNumber)
                .Select(w => new WorkoutRevisionRow(
                    w.WeekNumber,
                    w.DayNumber,
                    w.IsRestDay,
                    w.WorkoutName,
                    w.WorkoutType,
                    w.Description,
                    w.EstimatedDuration,
                    w.WarmUp,
                    w.CoolDown,
                    w.ExercisesJson))
                .ToList();

            return ComputeContentRevisionCore(program.TotalWeeks, ordered);
        }

        /// <summary>
        /// The exact set of fields <see cref="ComputeContentRevision"/> hashes for one workout.
        /// Property names/order matter: this record's JSON serialization is the shared byte
        /// contract between hashing a tracked <see cref="Models.Program"/> (the normal path) and
        /// hashing rows read back manually under an explicit row lock
        /// (<see cref="IsRevisionStillFreshUnderLockAsync"/>) — both must serialize identically
        /// for identical content, or a real-but-differently-sourced read of the same data would
        /// wrongly look "changed".
        /// </summary>
        private sealed record WorkoutRevisionRow(
            int WeekNumber,
            int DayNumber,
            bool IsRestDay,
            string WorkoutName,
            string? WorkoutType,
            string? Description,
            int? EstimatedDuration,
            string? WarmUp,
            string? CoolDown,
            string ExercisesJson);

        private static string ComputeContentRevisionCore(int totalWeeks, List<WorkoutRevisionRow> orderedWorkouts)
        {
            // JSON-serialize the exact fields under review rather than concatenating raw strings
            // with a plain delimiter: JSON escapes embedded separators/newlines, so two
            // differently-shaped workouts can never serialize to the same bytes just because a
            // free-text field (e.g. WorkoutName) happens to contain '|' or '\n'.
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                TotalWeeks = totalWeeks,
                Workouts = orderedWorkouts,
            });

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash)[..32];
        }

        /// <summary>
        /// Re-verifies <paramref name="clientRevision"/> against this program's content one more
        /// time, but under row locks that block every concurrent writer of that content
        /// (<c>ProgramsController.AddWorkout</c>/<c>UpdateWorkout</c>/<c>DeleteWorkout</c>) until
        /// the CALLER's ambient transaction (which must already be open) commits or rolls back.
        /// This — not the transaction around the status flip alone — is what actually closes the
        /// gap between "the content was fresh when checked" and "the content is still what's
        /// about to go live": without it, a concurrent edit could commit in the window between an
        /// earlier, unlocked freshness check and this transaction's own commit, and this method
        /// would have no way to notice.
        ///
        /// Lock order is deliberately parent-then-children (<c>Programs</c> row first, then every
        /// <c>ProgramWorkouts</c> row for it, each acquired ONE AT A TIME in ascending <c>Id</c>
        /// order via separate round trips - never a single multi-row <c>FOR UPDATE</c>, see below)
        /// and permanent for the rest of this transaction. Locking the parent row specifically also
        /// blocks a concurrent AddWorkout's FK-referencing INSERT (which takes a FOR KEY SHARE lock
        /// on the Programs row to satisfy the foreign key), which a lock on only the (not-yet-
        /// existing) child row could never catch - so a workout added concurrently can never
        /// commit until after this transaction ends either.
        ///
        /// The per-row loop, not a single <c>WHERE "ProgramId" = @id FOR UPDATE</c> query, is
        /// deliberate: Postgres's row-locking (<c>LockRows</c>) plan node acquires locks in
        /// whatever order the underlying scan happens to visit rows in, which is NOT guaranteed to
        /// follow an <c>ORDER BY</c> on the same query (the sort, if any, runs above LockRows) and
        /// is otherwise an unspecified planner choice. <c>ProgramsController.SwapWorkouts</c> locks
        /// two ProgramWorkouts rows in caller-supplied (unsorted) <c>Workout1Id</c>/<c>Workout2Id</c>
        /// order; for this method's lock order to be guaranteed to agree with it (both ascending by
        /// Id) - the only way to make the two paths provably deadlock-free against each other -
        /// each row must be locked by its own explicit, ordered statement. See
        /// <c>ProgramsController.LockWorkoutRowsInIdOrderAsync</c>, which SwapWorkouts now calls for
        /// the matching half of this guarantee.
        ///
        /// Reads via raw ADO on the context's own connection/transaction (never EF's tracked-entity
        /// query path): a `FromSqlRaw` query for an already-tracked entity would silently return
        /// the STALE in-memory instance instead of the fresh, now-locked row, defeating the whole
        /// point of re-checking.
        /// </summary>
        private static async Task<bool> IsRevisionStillFreshUnderLockAsync(
            TrainingContext context,
            Models.Program program,
            string clientRevision,
            CancellationToken cancellationToken)
        {
            var connection = context.Database.GetDbConnection();
            var dbTransaction = context.Database.CurrentTransaction?.GetDbTransaction()
                ?? throw new InvalidOperationException(
                    $"{nameof(IsRevisionStillFreshUnderLockAsync)} requires an active ambient transaction.");

            await using (var lockProgram = connection.CreateCommand())
            {
                lockProgram.Transaction = dbTransaction;
                lockProgram.CommandText = "SELECT 1 FROM \"Programs\" WHERE \"Id\" = @id FOR UPDATE";
                AddParameter(lockProgram, "id", program.Id);
                await lockProgram.ExecuteScalarAsync(cancellationToken);
            }

            var workoutIds = new List<int>();
            await using (var listIds = connection.CreateCommand())
            {
                listIds.Transaction = dbTransaction;
                listIds.CommandText =
                    "SELECT \"Id\" FROM \"ProgramWorkouts\" WHERE \"ProgramId\" = @id ORDER BY \"Id\"";
                AddParameter(listIds, "id", program.Id);

                await using var idReader = await listIds.ExecuteReaderAsync(cancellationToken);
                while (await idReader.ReadAsync(cancellationToken))
                {
                    workoutIds.Add(idReader.GetInt32(0));
                }
            }

            // Lock (and read) each row one at a time, in ascending Id order - see the doc comment
            // above for why this cannot be a single multi-row query. A row that no longer exists by
            // the time it's locked (concurrently deleted and committed) is simply omitted: its
            // absence changes the computed revision on its own, correctly surfacing as stale.
            var lockedWorkouts = new List<WorkoutRevisionRow>();
            foreach (var workoutId in workoutIds)
            {
                await using var lockWorkout = connection.CreateCommand();
                lockWorkout.Transaction = dbTransaction;
                lockWorkout.CommandText =
                    "SELECT \"WeekNumber\", \"DayNumber\", \"IsRestDay\", \"WorkoutName\", \"WorkoutType\", " +
                    "\"Description\", \"EstimatedDuration\", \"WarmUp\", \"CoolDown\", \"ExercisesJson\" " +
                    "FROM \"ProgramWorkouts\" WHERE \"Id\" = @workoutId FOR UPDATE";
                AddParameter(lockWorkout, "workoutId", workoutId);

                await using var reader = await lockWorkout.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    lockedWorkouts.Add(new WorkoutRevisionRow(
                        WeekNumber: reader.GetInt32(0),
                        DayNumber: reader.GetInt32(1),
                        IsRestDay: reader.GetBoolean(2),
                        WorkoutName: reader.GetString(3),
                        WorkoutType: reader.IsDBNull(4) ? null : reader.GetString(4),
                        Description: reader.IsDBNull(5) ? null : reader.GetString(5),
                        EstimatedDuration: reader.IsDBNull(6) ? null : reader.GetInt32(6),
                        WarmUp: reader.IsDBNull(7) ? null : reader.GetString(7),
                        CoolDown: reader.IsDBNull(8) ? null : reader.GetString(8),
                        ExercisesJson: reader.GetString(9)));
                }
            }

            var ordered = lockedWorkouts.OrderBy(w => w.WeekNumber).ThenBy(w => w.DayNumber).ToList();
            var current = ComputeContentRevisionCore(program.TotalWeeks, ordered);
            return string.Equals(current, clientRevision, StringComparison.Ordinal);
        }

        private static void AddParameter(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        /// <summary>
        /// Decides whether a still-<see cref="ProgramStatus.Draft"/> program may be activated as
        /// the exact thing the caller reviewed.
        ///
        /// New-client contract (<paramref name="clientRevision"/> supplied): compares against the
        /// program's CURRENT <see cref="ComputeContentRevision"/> — a direct content check that
        /// catches every material change (regeneration, a workout edited/added/removed through
        /// any endpoint, a week-count change) and is immune to false positives from an unrelated
        /// later chat message, because it never looks at messages at all.
        ///
        /// Legacy-client fallback (no revision supplied — an old build that predates this field):
        /// falls back to the best-effort "any newer assistant reply in the linked conversation"
        /// heuristic this contract used to rely on exclusively. This is weaker: it cannot detect a
        /// same-conversation edit made through a non-chat endpoint, and it can false-positive on
        /// an unrelated later message. That trade-off is accepted only for callers that never had
        /// the chance to obtain/echo a revision in the first place; every current client always
        /// does, so this path is dead in practice and exists purely so an old, already-deployed
        /// build keeps working with its previous (imperfect) protection rather than losing all
        /// staleness protection outright.
        /// </summary>
        public static async Task<DraftFreshness> CheckDraftFreshnessAsync(
            TrainingContext context,
            Models.Program program,
            string? clientRevision,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(clientRevision))
            {
                var current = ComputeContentRevision(program);
                return string.Equals(current, clientRevision, StringComparison.Ordinal)
                    ? DraftFreshness.Fresh
                    : DraftFreshness.Stale;
            }

            if (program.SourceConversationId is null)
            {
                return DraftFreshness.Fresh;
            }

            var hasNewerMessage = await context.ChatMessages
                .AsNoTracking()
                .AnyAsync(
                    m => m.ConversationId == program.SourceConversationId
                        && m.Role == "assistant"
                        && m.CreatedAt > program.CreatedAt,
                    cancellationToken);

            return hasNewerMessage ? DraftFreshness.Stale : DraftFreshness.Fresh;
        }

        /// <summary>
        /// Shared occurrence-key self-heal for a program's workouts. Mirrors
        /// <c>ProgramsController.SelfHealWorkoutOccurrenceKeysAsync</c> exactly (same
        /// compare-and-swap safety dance via <see cref="ProgramWorkoutExerciseOccurrences.EnsurePersistedAsync"/>)
        /// so both that controller's read endpoints and this service's activation path share one
        /// implementation.
        /// </summary>
        public static async Task EnsureOccurrenceKeysHealedAsync(
            TrainingContext context,
            IEnumerable<ProgramWorkout>? workouts,
            CancellationToken cancellationToken = default)
        {
            if (workouts == null)
            {
                return;
            }

            foreach (var workout in workouts)
            {
                var normalized = await ProgramWorkoutExerciseOccurrences.EnsurePersistedAsync(
                    context, workout, cancellationToken);

                if (normalized == workout.ExercisesJson)
                {
                    continue;
                }

                workout.ExercisesJson = normalized;

                var entry = context.Entry(workout);
                if (entry.State != EntityState.Detached)
                {
                    entry.Property(w => w.ExercisesJson).OriginalValue = normalized;
                }
            }
        }
    }
}
