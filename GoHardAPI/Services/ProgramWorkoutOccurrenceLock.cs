using System.Buffers.Binary;
using System.Security.Cryptography;
using GoHardAPI.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GoHardAPI.Services
{
    /// <summary>
    /// Transaction-scoped advisory lock keyed by ONE <c>ProgramWorkout.Id</c> - the single
    /// serialization point every write that reads-then-decides on that occurrence's state
    /// must take before doing so, so a concurrent pair of them (skip vs. start, skip vs.
    /// complete, complete vs. skip, or two skips) can never interleave into an
    /// inconsistent committed result.
    ///
    /// <para>Without this, "check no in-progress/completed session exists, then mark
    /// skipped" (or the reverse: session creation checking "is this workout skipped")
    /// is a classic TOCTOU race - a session can be created between the check and the
    /// write, or a skip can land between a session-create's ownership check and its
    /// insert. Every caller below acquires this lock as its very first step inside its
    /// own transaction, before any read of the ProgramWorkout or its linked Session, so
    /// whichever caller acquires the lock first runs its entire read-decide-write to
    /// completion (commit or rollback) before the other reads anything.</para>
    ///
    /// <para>Callers: <see cref="SessionCreateService"/> (generic <c>POST /sessions</c>
    /// when linked to a ProgramWorkout, and the from-program-workout create),
    /// <c>ProgramsController.SkipWorkout</c> / <c>CompleteWorkout</c> / <c>UnskipWorkout</c>,
    /// and <c>SessionsController.UpdateSessionStatus</c> when the session carries a
    /// <c>ProgramWorkoutId</c> and the transition touches Skipped. All five acquire the
    /// SAME lock key for the same workout id, so they serialize against each other
    /// regardless of which endpoint / service class each lives in.</para>
    ///
    /// <para>Uses a distinct SHA-256 salt from <c>SessionCreateService.DeriveAdvisoryLockKey</c>
    /// (which locks per <c>(UserId, ClientOperationId)</c>) so the two lock-key spaces
    /// practically never collide - a collision would only cost a tiny, bounded amount of
    /// unrelated concurrency, never correctness (every read after either lock is still
    /// scoped by exact ids).</para>
    /// </summary>
    public static class ProgramWorkoutOccurrenceLock
    {
        private const string Salt = "programworkout-occurrence-lock";

        /// <summary>
        /// Acquires the lock for <paramref name="workoutId"/>, or does nothing when
        /// <paramref name="workoutId"/> is null (no shared occurrence resource to
        /// protect - e.g. a freeform session with no program linkage). Must be called
        /// inside an already-open transaction; the lock is released automatically on
        /// commit or rollback. No-ops on a non-relational provider (SQLite in unit
        /// tests only - production is always relational, matching
        /// <see cref="SessionCreateService"/>'s own convention).
        /// </summary>
        public static async Task AcquireAsync(
            TrainingContext context, int? workoutId, CancellationToken cancellationToken)
        {
            if (workoutId is not { } id)
            {
                return;
            }

            if (context.Database.IsNpgsql())
            {
                var lockKey = DeriveLockKey(id);
                await context.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock({0})", new object[] { lockKey }, cancellationToken);
                return;
            }

            if (context.Database.IsSqlServer())
            {
                var resource = $"programworkout-occurrence:{id}";
                await context.Database.ExecuteSqlRawAsync(
                    @"DECLARE @lockResult int;
                      EXEC @lockResult = sp_getapplock @Resource = {0}, @LockMode = 'Exclusive',
                          @LockOwner = 'Transaction', @LockTimeout = -1;
                      IF @lockResult < 0 THROW 51000, 'sp_getapplock did not grant the program-workout occurrence lock', 1;",
                    new object[] { resource }, cancellationToken);
                return;
            }

            // SQLite / other: no advisory-lock primitive available. The surrounding
            // transaction is still real, but concurrency here is NOT evidence of
            // production behavior - see the PostgreSQL integration tests.
        }

        internal static long DeriveLockKey(int workoutId)
        {
            var saltBytes = System.Text.Encoding.UTF8.GetBytes(Salt);
            Span<byte> buffer = stackalloc byte[64];
            var written = 0;
            saltBytes.CopyTo(buffer);
            written += saltBytes.Length;
            BinaryPrimitives.WriteInt32BigEndian(buffer[written..], workoutId);
            written += 4;

            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(buffer[..written], hash);
            return BinaryPrimitives.ReadInt64LittleEndian(hash);
        }
    }
}
