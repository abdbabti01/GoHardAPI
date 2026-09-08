using System.Buffers.Binary;
using System.Security.Cryptography;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GoHardAPI.Services
{
    public enum SessionCreateResult
    {
        /// <summary>A new Session (and, for keyed requests, its operation record) was created. HTTP 201.</summary>
        Created,

        /// <summary>A prior keyed operation already created this Session; the canonical one is returned unchanged. HTTP 200.</summary>
        ReplayedExisting,

        /// <summary>
        /// A supplied program / program-workout link is missing OR owned by another user
        /// (indistinguishable on purpose). Nothing created. HTTP 404 <c>program_not_found</c>.
        /// </summary>
        ProgramNotFound,

        /// <summary>
        /// The keyed operation was canceled via
        /// <c>DELETE /api/v1/sessions/by-operation/{clientOperationId}</c>
        /// (<see cref="SessionCreateService.CancelCreateAsync"/>). Nothing created.
        /// HTTP 409 <c>operation_canceled</c>.
        /// </summary>
        Canceled,

        /// <summary>The keyed operation completed once but its Session no longer exists. Never recreated. HTTP 410.</summary>
        Gone,

        /// <summary>The operation record is in an indeterminate state. Fail closed — no second Session. HTTP 409 <c>operation_incomplete</c>.</summary>
        Incomplete,

        /// <summary>
        /// A keyed <c>from-program-workout</c> CREATE whose source
        /// <c>ProgramWorkout.ExercisesJson</c> could not be parsed. Nothing created, no
        /// tombstone written (a corrected retry can still succeed).
        /// HTTP 400 <c>program_workout_data_invalid</c>. Only reachable from
        /// <see cref="SessionCreateService.CreateFromProgramWorkoutAsync"/>.
        /// </summary>
        ProgramWorkoutDataInvalid,

        /// <summary>
        /// A keyed <c>from-program-workout</c> CREATE whose supplied
        /// <c>ClientOperationId</c> is the empty GUID — a value the by-operation
        /// cancellation endpoint refuses, so it must never identify a created Session.
        /// Rejected before any persistence. HTTP 400 <c>invalid_operation_key</c>. Only
        /// reachable from <see cref="SessionCreateService.CreateFromProgramWorkoutAsync"/>
        /// (the generic <c>POST /api/v1/sessions</c> contract is intentionally unchanged
        /// here).
        /// </summary>
        InvalidOperationKey,
    }

    /// <summary>Outcome of <see cref="SessionCreateService.CreateAsync"/>.</summary>
    public sealed record SessionCreateOutcome(SessionCreateResult Result, Session? Session, string? ErrorCode)
    {
        public static SessionCreateOutcome Created(Session s) => new(SessionCreateResult.Created, s, null);
        public static SessionCreateOutcome Replayed(Session s) => new(SessionCreateResult.ReplayedExisting, s, null);
        public static readonly SessionCreateOutcome ProgramNotFound =
            new(SessionCreateResult.ProgramNotFound, null, SessionCreateErrorCodes.ProgramNotFound);
        public static readonly SessionCreateOutcome Canceled =
            new(SessionCreateResult.Canceled, null, SessionCreateErrorCodes.OperationCanceled);
        public static readonly SessionCreateOutcome Gone =
            new(SessionCreateResult.Gone, null, SessionCreateErrorCodes.OperationTargetDeleted);
        public static readonly SessionCreateOutcome Incomplete =
            new(SessionCreateResult.Incomplete, null, SessionCreateErrorCodes.OperationIncomplete);
        public static readonly SessionCreateOutcome ProgramWorkoutDataInvalid =
            new(SessionCreateResult.ProgramWorkoutDataInvalid, null, SessionCreateErrorCodes.ProgramWorkoutDataInvalid);
        public static readonly SessionCreateOutcome InvalidOperationKey =
            new(SessionCreateResult.InvalidOperationKey, null, SessionCancelErrorCodes.InvalidOperationKey);
    }

    /// <summary>
    /// Server-side foundation for keyed Session CREATE.
    ///
    /// <para><b>Legacy request (no <c>ClientOperationId</c>)</b> — preserves the historical
    /// behavior: one <c>INSERT</c>, always a new Session, HTTP 201.</para>
    ///
    /// <para><b>Keyed request</b> — idempotent per <c>(authenticated UserId, ClientOperationId)</c>:</para>
    /// <list type="number">
    ///   <item>open an explicit transaction;</item>
    ///   <item>take a single transaction-scoped operation lock (PostgreSQL
    ///     <c>pg_advisory_xact_lock</c>, SQL Server <c>sp_getapplock</c> owned by the
    ///     transaction) before touching the operation record or any Session;</item>
    ///   <item>look the operation record up by <b>both</b> the JWT-derived UserId and the key,
    ///     then run the state machine (no record / canceled / completed-and-present /
    ///     completed-but-gone / indeterminate).</item>
    /// </list>
    /// First writer wins: a conflicting replay body is ignored and never overwrites the
    /// canonical Session.
    /// </summary>
    public class SessionCreateService
    {
        private readonly TrainingContext _context;
        private readonly ILogger<SessionCreateService> _logger;

        // Bounded retry — only for genuinely retryable serialization failures
        // (PostgreSQL 40P01 deadlock, SQL Server 1205 deadlock victim) and for the
        // unique-index race that can only occur if the advisory lock did not serialize
        // two callers (23505 / 2601 / 2627). Validation failures and arbitrary
        // exceptions are never retried.
        private const int MaxAttempts = 3;

        public SessionCreateService(TrainingContext context, ILogger<SessionCreateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Produces the genuine first-write result for a keyed CREATE. It runs INSIDE the
        /// operation lock + transaction and ONLY when no operation row yet exists for
        /// <c>(UserId, ClientOperationId)</c>: it returns either the Session to persist —
        /// with any child graph already attached through navigation collections — or a
        /// terminal failure outcome. It must not open a transaction, take a lock, or call
        /// SaveChanges. Replay paths never invoke it, so the canonical write is never
        /// re-validated or re-materialized.
        ///
        /// <para>Exception: the from-program-workout factory
        /// (<see cref="ProgramWorkoutFirstWriteAsync"/>) may issue one direct
        /// <c>ExecuteUpdateAsync</c> statement — never <c>SaveChanges</c>, never a new
        /// transaction/lock — via <see cref="ProgramWorkoutExerciseOccurrences.EnsurePersistedAsync"/>
        /// to durably fill in any missing exercise <c>occurrenceKey</c> on the source
        /// <c>ProgramWorkout</c> before materializing. It participates in the caller's
        /// already-open ambient transaction, so it rolls back with everything else on failure,
        /// and its compare-and-swap guard means a lost race is a no-op, not a conflicting
        /// write.</para>
        /// </summary>
        private delegate Task<KeyedFirstWrite> FirstWriteFactory(CancellationToken cancellationToken);

        private readonly record struct KeyedFirstWrite(Session? Session, SessionCreateOutcome? Failure)
        {
            public static KeyedFirstWrite Ok(Session session) => new(session, null);
            public static KeyedFirstWrite Fail(SessionCreateOutcome outcome) => new(null, outcome);
        }

        // virtual: lets the test project substitute a counting spy to prove a
        // rate-limited request never reaches this method. No behavioral effect.
        public virtual async Task<SessionCreateOutcome> CreateAsync(
            int userId, SessionCreateRequestDto request, CancellationToken cancellationToken)
        {
            // ---- Legacy / unkeyed: creation behavior + program-ownership validation ----------
            if (request.ClientOperationId is not { } key)
            {
                return await CreateUnkeyedAsync(
                    userId, ct => GenericFirstWriteAsync(userId, request, ct), cancellationToken);
            }

            return await RunKeyedWithRetryAsync(
                userId, key, ct => GenericFirstWriteAsync(userId, request, ct), cancellationToken);
        }

        /// <summary>First-write step for the generic <c>POST /api/v1/sessions</c>.</summary>
        private async Task<KeyedFirstWrite> GenericFirstWriteAsync(
            int userId, SessionCreateRequestDto request, CancellationToken cancellationToken)
        {
            if (!await ProgramLinkageOwnedAsync(
                    userId, request.ProgramId, request.ProgramWorkoutId, cancellationToken))
            {
                return KeyedFirstWrite.Fail(SessionCreateOutcome.ProgramNotFound);
            }

            return KeyedFirstWrite.Ok(request.ToNewSession(userId));
        }

        /// <summary>
        /// Unkeyed create: run the first-write factory, one save, HTTP 201. No transaction,
        /// no lock, no operation row — the historical behavior. A raced program delete
        /// (TOCTOU FK violation) is converted to <c>program_not_found</c>, never a raw 500.
        /// </summary>
        private async Task<SessionCreateOutcome> CreateUnkeyedAsync(
            int userId, FirstWriteFactory firstWrite, CancellationToken cancellationToken)
        {
            var first = await firstWrite(cancellationToken);
            if (first.Failure is not null)
            {
                return first.Failure;
            }

            _context.Sessions.Add(first.Session!);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (IsProgramForeignKeyViolation(ex))
            {
                _context.ChangeTracker.Clear();
                return SessionCreateOutcome.ProgramNotFound;
            }
            return SessionCreateOutcome.Created(first.Session!);
        }

        /// <summary>
        /// Keyed / legacy CREATE from a program workout — the durable-protocol counterpart of
        /// <see cref="CreateAsync"/> for <c>POST /api/v1/sessions/from-program-workout</c>.
        ///
        /// <para><b>No key</b> — legacy behavior: validate program / workout ownership,
        /// materialize the Session + Exercises, one save, HTTP 201. No operation row.</para>
        ///
        /// <para><b>Keyed</b> — idempotent per <c>(authenticated UserId, ClientOperationId)</c>
        /// on the <b>same</b> <see cref="SessionCreateOperation"/> registry, the <b>same</b>
        /// transaction-owned operation lock (<see cref="AcquireOperationLockAsync"/>) and the
        /// <b>same</b> state machine as keyed <see cref="CreateAsync"/>. The genuine first
        /// write materializes the Session, its Exercises and the completed operation row
        /// atomically; a replay returns the canonical Session untouched and never re-reads the
        /// ProgramWorkout or creates a second child graph. Cancellation via
        /// <see cref="CancelCreateAsync"/> wins in every accepted ordering.</para>
        ///
        /// <para>A supplied <c>ClientOperationId</c> of <see cref="Guid.Empty"/> is rejected
        /// (<see cref="SessionCreateResult.InvalidOperationKey"/>) before any persistence: the
        /// by-operation cancellation endpoint refuses the empty GUID, so a Session must never
        /// be created under a key that can never be cancelled. A <c>null</c> key still takes
        /// the unkeyed compatibility path; it is never treated as an empty key and vice
        /// versa.</para>
        /// </summary>
        public virtual async Task<SessionCreateOutcome> CreateFromProgramWorkoutAsync(
            int userId, CreateSessionFromProgramWorkoutDto request, CancellationToken cancellationToken)
        {
            if (request.ClientOperationId is not { } key)
            {
                return await CreateUnkeyedAsync(
                    userId, ct => ProgramWorkoutFirstWriteAsync(userId, request, ct), cancellationToken);
            }

            if (key == Guid.Empty)
            {
                // Reject before touching the database. The empty GUID is not a usable
                // idempotency key: DELETE /api/v1/sessions/by-operation/{key} refuses it,
                // so a Session created under it could never be cancelled by operation key.
                return SessionCreateOutcome.InvalidOperationKey;
            }

            return await RunKeyedWithRetryAsync(
                userId, key, ct => ProgramWorkoutFirstWriteAsync(userId, request, ct), cancellationToken);
        }

        /// <summary>
        /// First-write step for a from-program-workout CREATE: validates that BOTH the
        /// request's <c>ProgramId</c> and the referenced <c>ProgramWorkout</c>'s parent
        /// program belong to <paramref name="userId"/> (missing OR foreign collapse to the
        /// same non-disclosing <see cref="SessionCreateOutcome.ProgramNotFound"/>), then
        /// materializes the Session + Exercises. Read-only apart from the returned,
        /// not-yet-added entity graph.
        /// </summary>
        private async Task<KeyedFirstWrite> ProgramWorkoutFirstWriteAsync(
            int userId, CreateSessionFromProgramWorkoutDto request, CancellationToken cancellationToken)
        {
            var workout = await _context.ProgramWorkouts
                .AsNoTracking()
                .Include(pw => pw.Program)
                .FirstOrDefaultAsync(pw => pw.Id == request.ProgramWorkoutId, cancellationToken);

            // Missing workout, a workout with no parent, or a parent owned by another user:
            // one indistinguishable outcome, so this is never a cross-user existence oracle.
            if (workout?.Program is null || workout.Program.UserId != userId)
            {
                return KeyedFirstWrite.Fail(SessionCreateOutcome.ProgramNotFound);
            }

            // request.ProgramId is stamped onto the Session verbatim (the legacy contract
            // trusts the request over a possibly-stale ProgramWorkout.ProgramId); it must
            // still be a program this user owns.
            var requestedProgramOwned = await _context.Programs
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.ProgramId && p.UserId == userId, cancellationToken);
            if (!requestedProgramOwned)
            {
                return KeyedFirstWrite.Fail(SessionCreateOutcome.ProgramNotFound);
            }

            // Occurrence identity must be durable BEFORE the Session exists, even if no GET
            // ever preceded this create: fill in and persist any missing occurrenceKey values
            // on the source workout now (compare-and-swap; never clobbers a concurrent edit),
            // then materialize from that normalized text. Build() itself stays read-only and
            // never invents a key on its own.
            workout.ExercisesJson = await ProgramWorkoutExerciseOccurrences.EnsurePersistedAsync(
                _context, workout, cancellationToken);

            try
            {
                return KeyedFirstWrite.Ok(
                    ProgramWorkoutSessionMaterializer.Build(userId, workout, request.ProgramId));
            }
            catch (ProgramWorkoutSessionMaterializer.ExercisesJsonFormatException)
            {
                return KeyedFirstWrite.Fail(SessionCreateOutcome.ProgramWorkoutDataInvalid);
            }
        }

        /// <summary>
        /// Shared keyed-CREATE driver: the non-relational degrade path plus the bounded
        /// retry loop around <see cref="RunKeyedAttemptAsync"/>. The only thing that varies
        /// between the generic and the from-program-workout entry points is
        /// <paramref name="firstWrite"/>.
        /// </summary>
        private async Task<SessionCreateOutcome> RunKeyedWithRetryAsync(
            int userId, Guid key, FirstWriteFactory firstWrite, CancellationToken cancellationToken)
        {
            // A non-relational provider (InMemory, only in unit tests) cannot run a real
            // transaction or advisory lock. Degrade to a plain keyed insert so the unit
            // path still works; production is always relational.
            if (!_context.Database.IsRelational())
            {
                return await CreateKeyedWithoutTransactionAsync(userId, key, firstWrite, cancellationToken);
            }

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await RunKeyedAttemptAsync(userId, key, firstWrite, cancellationToken);
                }
                catch (Exception ex) when (attempt < MaxAttempts && IsRetryable(ex))
                {
                    _logger.LogWarning(ex,
                        "Retryable failure on keyed session create (attempt {Attempt}/{Max}) for user {UserId}, operation {OperationId}",
                        attempt, MaxAttempts, userId, key);
                    await Task.Delay(20 * attempt, cancellationToken);
                }
            }
        }

        private async Task<SessionCreateOutcome> RunKeyedAttemptAsync(
            int userId, Guid key, FirstWriteFactory firstWrite, CancellationToken cancellationToken)
        {
            // Each attempt starts from a clean slate. A prior attempt that failed on a
            // retryable error (e.g. the unique-index race) leaves its Added Session +
            // operation entities tracked after the rolled-back transaction; without this
            // clear the next attempt would re-Add fresh copies and the SaveChanges would
            // try to insert two operation rows for the same (UserId, ClientOperationId).
            _context.ChangeTracker.Clear();

            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

            // (2) One transaction-scoped operation lock BEFORE reading or writing anything.
            await AcquireOperationLockAsync(userId, key, cancellationToken);

            // (3) Look up the operation record by BOTH the authenticated UserId and the key.
            // A key belonging to user A must never be visible under user B's JWT.
            var op = await _context.SessionCreateOperations
                .FirstOrDefaultAsync(o => o.UserId == userId && o.ClientOperationId == key, cancellationToken);

            if (op is null)
            {
                // FIRST keyed write only. The factory validates ownership and builds the
                // Session (+ any child graph) here, inside the lock. Replay branches below
                // never reach it and so never revalidate or re-materialize the canonical
                // first write.
                var first = await firstWrite(cancellationToken);
                if (first.Failure is not null)
                {
                    await tx.RollbackAsync(cancellationToken);
                    _context.ChangeTracker.Clear();
                    return first.Failure;
                }

                var session = first.Session!;
                // The owner-scoped partial unique index on Sessions (UserId, ClientOperationId)
                // is the database-level backstop against a double-create should the advisory
                // lock ever fail to serialize two callers (23505 -> retry -> replay).
                session.ClientOperationId = key;

                var record = new SessionCreateOperation
                {
                    UserId = userId,
                    ClientOperationId = key,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.SessionCreateOperations.Add(record);
                if (_context.Entry(session).State == EntityState.Detached)
                {
                    _context.Sessions.Add(session);
                }
                try
                {
                    // Session + any child Exercises/Sets + the operation row, one transaction.
                    await _context.SaveChangesAsync(cancellationToken); // session.Id assigned
                }
                catch (Exception ex) when (IsProgramForeignKeyViolation(ex))
                {
                    // Program / workout deleted between the check and the insert (TOCTOU).
                    // Roll the whole attempt back — no operation row, no session, no
                    // children — and return 404, never a raw FK 500.
                    await tx.RollbackAsync(cancellationToken);
                    _context.ChangeTracker.Clear();
                    return SessionCreateOutcome.ProgramNotFound;
                }

                record.SessionId = session.Id;
                record.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                await tx.CommitAsync(cancellationToken);
                return SessionCreateOutcome.Created(session);
            }

            if (op.CanceledAt is not null)
            {
                // Cancelled via CancelCreateAsync (or a direct DB write). The shared
                // operation lock guarantees that if cancel committed first, this CREATE
                // observes CanceledAt and creates nothing.
                await tx.RollbackAsync(cancellationToken);
                return SessionCreateOutcome.Canceled;
            }

            if (op.CompletedAt is not null)
            {
                // Completed once. Return the canonical Session if it is still there and
                // still owned by this user; otherwise it is gone for good — never recreate.
                var canonical = op.SessionId is { } sid
                    ? await _context.Sessions
                        .FirstOrDefaultAsync(s => s.Id == sid && s.UserId == userId, cancellationToken)
                    : null;

                await tx.RollbackAsync(cancellationToken);
                return canonical is not null
                    ? SessionCreateOutcome.Replayed(canonical)
                    : SessionCreateOutcome.Gone;
            }

            // Present, not canceled, not completed. Under correct advisory-lock
            // serialization this is unreachable (the creating transaction commits the
            // operation row and the Session together or rolls both back). Fail closed:
            // do NOT create a second Session.
            _logger.LogError(
                "Indeterminate SessionCreateOperation {OperationRowId} for user {UserId}, key {OperationId}: " +
                "SessionId={SessionId}, CompletedAt=null, CanceledAt=null",
                op.Id, userId, key, op.SessionId);
            await tx.RollbackAsync(cancellationToken);
            return SessionCreateOutcome.Incomplete;
        }

        private async Task<SessionCreateOutcome> CreateKeyedWithoutTransactionAsync(
            int userId, Guid key, FirstWriteFactory firstWrite, CancellationToken cancellationToken)
        {
            var op = await _context.SessionCreateOperations
                .FirstOrDefaultAsync(o => o.UserId == userId && o.ClientOperationId == key, cancellationToken);

            if (op is not null)
            {
                // Replay: never revalidate, never re-materialize, never mutate.
                if (op.CanceledAt is not null) return SessionCreateOutcome.Canceled;
                if (op.CompletedAt is not null)
                {
                    var canonical = op.SessionId is { } sid
                        ? await _context.Sessions.FirstOrDefaultAsync(
                            s => s.Id == sid && s.UserId == userId, cancellationToken)
                        : null;
                    return canonical is not null
                        ? SessionCreateOutcome.Replayed(canonical)
                        : SessionCreateOutcome.Gone;
                }
                return SessionCreateOutcome.Incomplete;
            }

            var first = await firstWrite(cancellationToken);
            if (first.Failure is not null)
            {
                return first.Failure;
            }

            var session = first.Session!;
            session.ClientOperationId = key;
            var record = new SessionCreateOperation
            {
                UserId = userId,
                ClientOperationId = key,
                CreatedAt = DateTime.UtcNow,
            };
            _context.SessionCreateOperations.Add(record);
            if (_context.Entry(session).State == EntityState.Detached)
            {
                _context.Sessions.Add(session);
            }
            await _context.SaveChangesAsync(cancellationToken);
            record.SessionId = session.Id;
            record.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return SessionCreateOutcome.Created(session);
        }

        /// <summary>
        /// Cancels the keyed Session CREATE identified by <paramref name="key"/> for the
        /// authenticated <paramref name="userId"/>. Idempotent and safe to retry.
        ///
        /// <para>Once this returns (the work is committed):</para>
        /// <list type="bullet">
        ///   <item>a durable <see cref="SessionCreateOperation"/> tombstone exists for
        ///     <c>(userId, key)</c> with <see cref="SessionCreateOperation.CanceledAt"/> set;</item>
        ///   <item>no Session produced by that operation remains — an already-created Session
        ///     and its owned children (<c>Exercises</c> → <c>ExerciseSets</c>) are removed via
        ///     the established cascade;</item>
        ///   <item>a concurrent or later keyed CREATE with the same key creates nothing:
        ///     <see cref="RunKeyedAttemptAsync"/> observes <c>CanceledAt</c> and returns
        ///     <see cref="SessionCreateResult.Canceled"/>.</item>
        /// </list>
        ///
        /// <para>Cancel and CREATE serialize on the <b>same</b> transaction-scoped operation
        /// lock (<see cref="AcquireOperationLockAsync"/>): PostgreSQL
        /// <c>pg_advisory_xact_lock</c> / SQL Server <c>sp_getapplock</c>, both owned by the
        /// transaction. Cancellation therefore wins in every accepted ordering across
        /// multiple API instances and independent PostgreSQL transactions — there is no
        /// process-local lock.</para>
        ///
        /// <para>Lookup and mutation are scoped by <paramref name="userId"/>. A key owned by
        /// another user is invisible here and that user's tombstone / Session is never
        /// touched; the caller cannot learn whether such a key exists. A key this user has
        /// never used still gets a tombstone written, so a cancel that arrives before the
        /// CREATE is honored.</para>
        ///
        /// <para>The empty GUID is not a real idempotency key: it is rejected without a
        /// database write.</para>
        /// </summary>
        public virtual async Task CancelCreateAsync(int userId, Guid key, CancellationToken cancellationToken)
        {
            if (key == Guid.Empty)
            {
                return;
            }

            // A non-relational provider (InMemory, unit tests only) has no real transaction
            // or advisory lock. Production is always relational.
            if (!_context.Database.IsRelational())
            {
                await CancelCreateWithoutTransactionAsync(userId, key, cancellationToken);
                return;
            }

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await RunCancelAttemptAsync(userId, key, cancellationToken);
                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts && IsRetryable(ex))
                {
                    _logger.LogWarning(ex,
                        "Retryable failure on keyed session-create cancel (attempt {Attempt}/{Max}) for user {UserId}, operation {OperationId}",
                        attempt, MaxAttempts, userId, key);
                    await Task.Delay(20 * attempt, cancellationToken);
                }
            }
        }

        private async Task RunCancelAttemptAsync(int userId, Guid key, CancellationToken cancellationToken)
        {
            // Same clean-slate contract as RunKeyedAttemptAsync: a prior attempt that failed
            // on a retryable error leaves Added/Modified entities tracked after its
            // rolled-back transaction.
            _context.ChangeTracker.Clear();

            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

            // The one transaction-scoped lock the keyed CREATE also takes. Whichever of
            // create / cancel acquires it first runs to completion before the other reads
            // anything, so no check-then-act race can leave a Session after cancellation.
            await AcquireOperationLockAsync(userId, key, cancellationToken);

            var op = await _context.SessionCreateOperations
                .FirstOrDefaultAsync(o => o.UserId == userId && o.ClientOperationId == key, cancellationToken);

            if (op is null)
            {
                // Cancel reached the server before any CREATE (or this user never used this
                // key). Persist the tombstone now so a later keyed CREATE is refused.
                _context.SessionCreateOperations.Add(new SessionCreateOperation
                {
                    UserId = userId,
                    ClientOperationId = key,
                    CreatedAt = DateTime.UtcNow,
                    CanceledAt = DateTime.UtcNow,
                });
                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return;
            }

            if (op.CanceledAt is null)
            {
                op.CanceledAt = DateTime.UtcNow;
            }

            // If the operation already produced a Session, delete it and its owned children.
            // Loading the child graph makes the cascade deterministic on every provider; the
            // database ON DELETE CASCADE (Exercises → ExerciseSets) is the backstop. The
            // Session lookup is itself owner-scoped — a corrupted cross-user SessionId
            // pointer must never let one user delete another user's Session.
            if (op.SessionId is { } sessionId)
            {
                var session = await _context.Sessions
                    .Include(s => s.Exercises)
                        .ThenInclude(e => e.ExerciseSets)
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

                if (session is not null)
                {
                    _context.Sessions.Remove(session);
                }
            }

            // The operation row itself is NEVER removed:
            // FK_SessionCreateOperations_Sessions_SessionId is ON DELETE SET NULL, so
            // deleting the Session only blanks the pointer. The CanceledAt tombstone
            // survives to keep this key permanently unusable for CREATE.
            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        private async Task CancelCreateWithoutTransactionAsync(
            int userId, Guid key, CancellationToken cancellationToken)
        {
            var op = await _context.SessionCreateOperations
                .FirstOrDefaultAsync(o => o.UserId == userId && o.ClientOperationId == key, cancellationToken);

            if (op is null)
            {
                _context.SessionCreateOperations.Add(new SessionCreateOperation
                {
                    UserId = userId,
                    ClientOperationId = key,
                    CreatedAt = DateTime.UtcNow,
                    CanceledAt = DateTime.UtcNow,
                });
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            if (op.CanceledAt is null)
            {
                op.CanceledAt = DateTime.UtcNow;
            }

            if (op.SessionId is { } sessionId)
            {
                var session = await _context.Sessions
                    .Include(s => s.Exercises)
                        .ThenInclude(e => e.ExerciseSets)
                    .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);

                if (session is not null)
                {
                    _context.Sessions.Remove(session);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Acquires the single transaction-owned lock that serializes all keyed writes for
        /// one <c>(UserId, ClientOperationId)</c>. Released automatically when the
        /// transaction commits or rolls back.
        /// </summary>
        private async Task AcquireOperationLockAsync(int userId, Guid key, CancellationToken cancellationToken)
        {
            if (_context.Database.IsNpgsql())
            {
                // Production semantics: a deterministic 64-bit key derived from
                // UserId + ClientOperationId. A hash collision can only make two
                // *unrelated* operations wait on the same lock — a tiny, bounded loss of
                // concurrency. It can never merge their state: every operation-record and
                // Session lookup after the lock is still filtered by the exact
                // (UserId, ClientOperationId) / (SessionId, UserId), so two distinct keys
                // always resolve to two distinct operation rows and two distinct Sessions.
                var lockKey = DeriveAdvisoryLockKey(userId, key);
                await _context.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock({0})", new object[] { lockKey }, cancellationToken);
                return;
            }

            if (_context.Database.IsSqlServer())
            {
                // sp_getapplock with @LockOwner = 'Transaction' is released on commit/rollback,
                // exactly like pg_advisory_xact_lock. @LockTimeout = -1 waits instead of
                // failing, so ordering — not wall-clock — decides the winner. A negative
                // return code means the lock was NOT granted; fail loudly rather than
                // proceed unserialized (the unique indexes would still prevent a
                // double-create, but silently losing the lock hides a real problem).
                var resource = $"sessioncreate:{userId}:{key:N}";
                await _context.Database.ExecuteSqlRawAsync(
                    @"DECLARE @lockResult int;
                      EXEC @lockResult = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive',
                          @LockOwner = 'Transaction', @LockTimeout = -1;
                      IF @lockResult < 0 THROW 51000, 'sp_getapplock did not grant the session-create lock', 1;",
                    new object[] { resource }, cancellationToken);
                return;
            }

            // SQLite / other: no advisory-lock primitive. The surrounding transaction plus
            // the composite unique index still make the outcome correct; concurrency here is
            // NOT evidence of production behavior (see the PostgreSQL integration tests).
        }

        internal static long DeriveAdvisoryLockKey(int userId, Guid operationId)
        {
            Span<byte> buffer = stackalloc byte[4 + 16];
            BinaryPrimitives.WriteInt32BigEndian(buffer, userId);
            operationId.TryWriteBytes(buffer[4..]);

            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(buffer, hash);
            return BinaryPrimitives.ReadInt64LittleEndian(hash);
        }

        private static bool IsRetryable(Exception exception)
        {
            for (var e = exception; e is not null; e = e.InnerException)
            {
                switch (e)
                {
                    // PostgreSQL: 40P01 deadlock_detected; 23505 unique_violation (index race).
                    // 40001 (serialization_failure) is intentionally NOT treated as retryable —
                    // it does not occur under READ COMMITTED, which is what we run.
                    case PostgresException pg when pg.SqlState is "40P01" or "23505":
                        return true;

                    // SQL Server: 1205 deadlock victim; 2601/2627 unique-index / PK violation.
                    case SqlException sql when SqlNumbers(sql).Any(n => n is 1205 or 2601 or 2627):
                        return true;
                }

                // SQLite (tests only — production is PostgreSQL). Matched by type name +
                // message so the API project takes no compile-time dependency on
                // Microsoft.Data.Sqlite. A UNIQUE violation here is the index-race backstop
                // and is safe to retry into a replay.
                if (e.GetType().FullName is "Microsoft.Data.Sqlite.SqliteException"
                    && e.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<int> SqlNumbers(SqlException ex)
        {
            yield return ex.Number;
            foreach (SqlError error in ex.Errors)
            {
                yield return error.Number;
            }
        }

        /// <summary>
        /// Validates a supplied program link against the JWT-derived <paramref name="userId"/>
        /// ONLY. A null link is always valid. Read-only; never mutates. Returns false
        /// (→ 404 <c>program_not_found</c>, identical for missing and foreign) when:
        /// <list type="bullet">
        ///   <item><paramref name="programId"/> is supplied and no <see cref="Models.Program"/>
        ///     with that id is owned by the user;</item>
        ///   <item><paramref name="programWorkoutId"/> is supplied and no
        ///     <see cref="Models.ProgramWorkout"/> with that id exists whose parent Program
        ///     is owned by the user;</item>
        ///   <item>both are supplied and the workout's <c>ProgramId</c> differs from
        ///     <paramref name="programId"/>.</item>
        /// </list>
        /// Uses scalar predicates only, so it behaves identically on Npgsql, SQL Server,
        /// SQLite and the EF InMemory provider.
        /// </summary>
        private async Task<bool> ProgramLinkageOwnedAsync(
            int userId, int? programId, int? programWorkoutId, CancellationToken cancellationToken)
        {
            if (programWorkoutId is { } workoutId)
            {
                var workout = await _context.ProgramWorkouts
                    .AsNoTracking()
                    .Where(w => w.Id == workoutId)
                    .Select(w => new { w.ProgramId })
                    .FirstOrDefaultAsync(cancellationToken);

                // Always run BOTH lookups regardless of whether the workout row exists, so
                // "no such workout id anywhere" and "workout exists but its program is
                // foreign" take the identical code path / query count — no cross-user
                // existence-timing oracle over the global ProgramWorkout id space. A
                // missing workout uses a sentinel program id (-1) that no row can own.
                var candidateProgramId = workout?.ProgramId ?? -1;

                var parentOwned = await _context.Programs
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == candidateProgramId && p.UserId == userId, cancellationToken);

                if (workout is null || !parentOwned)
                {
                    return false;
                }

                if (programId is { } explicitProgramId && workout.ProgramId != explicitProgramId)
                {
                    return false;
                }

                return true;
            }

            if (programId is { } programOnlyId)
            {
                return await _context.Programs
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == programOnlyId && p.UserId == userId, cancellationToken);
            }

            return true;
        }

        /// <summary>
        /// True when <paramref name="exception"/> (or an inner) is a foreign-key violation
        /// naming a Program / ProgramWorkout constraint — a program deleted between
        /// <see cref="ProgramLinkageOwnedAsync"/> and the insert (TOCTOU). Such a case is
        /// converted to <c>program_not_found</c> (404), never surfaced as a raw 500. The
        /// <c>UserId</c> FK is not client-settable (it comes from a validated JWT), so it is
        /// deliberately out of scope here.
        /// </summary>
        private static bool IsProgramForeignKeyViolation(Exception exception)
        {
            for (var e = exception; e is not null; e = e.InnerException)
            {
                if (e is PostgresException pg
                    && pg.SqlState == "23503"
                    && (pg.ConstraintName?.Contains("Program", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    return true;
                }

                if (e is SqlException sql
                    && SqlNumbers(sql).Any(n => n == 547)
                    && sql.Message.Contains("Program", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // SQLite (tests only). It does not name the constraint; the only
                // client-settable FKs on the insert are ProgramId / ProgramWorkoutId
                // (UserId is a validated JWT), so a FK failure here is a raced program delete.
                if (e.GetType().FullName is "Microsoft.Data.Sqlite.SqliteException"
                    && e.Message.Contains("FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
