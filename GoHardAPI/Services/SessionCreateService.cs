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
        /// The keyed operation was canceled. Nothing created. HTTP 409 <c>operation_canceled</c>.
        /// DORMANT in P1 — nothing writes <c>SessionCreateOperation.CanceledAt</c> until the
        /// P2 DELETE-by-operation-key endpoint.
        /// </summary>
        Canceled,

        /// <summary>The keyed operation completed once but its Session no longer exists. Never recreated. HTTP 410.</summary>
        Gone,

        /// <summary>The operation record is in an indeterminate state. Fail closed — no second Session. HTTP 409 <c>operation_incomplete</c>.</summary>
        Incomplete,
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

        public async Task<SessionCreateOutcome> CreateAsync(
            int userId, SessionCreateRequestDto request, CancellationToken cancellationToken)
        {
            // ---- Legacy / unkeyed: creation behavior + program-ownership validation ----------
            if (request.ClientOperationId is not { } key)
            {
                if (!await ProgramLinkageOwnedAsync(
                        userId, request.ProgramId, request.ProgramWorkoutId, cancellationToken))
                {
                    return SessionCreateOutcome.ProgramNotFound;
                }

                var session = request.ToNewSession(userId);
                _context.Sessions.Add(session);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex) when (IsProgramForeignKeyViolation(ex))
                {
                    // A program was deleted between the check and the insert (TOCTOU).
                    // Never surface a raw FK exception as a 500.
                    _context.ChangeTracker.Clear();
                    return SessionCreateOutcome.ProgramNotFound;
                }
                return SessionCreateOutcome.Created(session);
            }

            // A non-relational provider (InMemory, only in unit tests) cannot run a real
            // transaction or advisory lock. Degrade to a plain keyed insert so the unit
            // path still works; production is always relational.
            if (!_context.Database.IsRelational())
            {
                return await CreateKeyedWithoutTransactionAsync(userId, key, request, cancellationToken);
            }

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await RunKeyedAttemptAsync(userId, key, request, cancellationToken);
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
            int userId, Guid key, SessionCreateRequestDto request, CancellationToken cancellationToken)
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
                // FIRST keyed write only. Validate program linkage here, inside the lock,
                // before creating anything. Replay branches below never reach this and so
                // never revalidate or mutate the canonical first write.
                if (!await ProgramLinkageOwnedAsync(
                        userId, request.ProgramId, request.ProgramWorkoutId, cancellationToken))
                {
                    await tx.RollbackAsync(cancellationToken);
                    return SessionCreateOutcome.ProgramNotFound;
                }

                var session = request.ToNewSession(userId);
                var record = new SessionCreateOperation
                {
                    UserId = userId,
                    ClientOperationId = key,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.SessionCreateOperations.Add(record);
                _context.Sessions.Add(session);
                try
                {
                    await _context.SaveChangesAsync(cancellationToken); // both rows; session.Id assigned
                }
                catch (Exception ex) when (IsProgramForeignKeyViolation(ex))
                {
                    // Program deleted between the check and the insert (TOCTOU). Roll the
                    // whole attempt back — no operation row, no session — and return 404,
                    // never a raw FK 500.
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
                // DORMANT in P1 (no producer of CanceledAt); reachable only via a P2 cancel
                // endpoint or a direct DB write.
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
            int userId, Guid key, SessionCreateRequestDto request, CancellationToken cancellationToken)
        {
            var op = await _context.SessionCreateOperations
                .FirstOrDefaultAsync(o => o.UserId == userId && o.ClientOperationId == key, cancellationToken);

            if (op is not null)
            {
                // Replay: never revalidate program linkage, never mutate.
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

            if (!await ProgramLinkageOwnedAsync(
                    userId, request.ProgramId, request.ProgramWorkoutId, cancellationToken))
            {
                return SessionCreateOutcome.ProgramNotFound;
            }

            var session = request.ToNewSession(userId);
            var record = new SessionCreateOperation
            {
                UserId = userId,
                ClientOperationId = key,
                CreatedAt = DateTime.UtcNow,
            };
            _context.SessionCreateOperations.Add(record);
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);
            record.SessionId = session.Id;
            record.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return SessionCreateOutcome.Created(session);
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
