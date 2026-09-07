using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Delete-during-session-create convergence proven against a <b>real PostgreSQL</b>
    /// container. <see cref="SessionCreateService.CancelCreateAsync"/> and the keyed
    /// <see cref="SessionCreateService.CreateAsync"/> are exercised with independent Npgsql
    /// connections and real overlapping transactions; create/cancel ordering is forced with
    /// an externally held <c>pg_advisory_xact_lock</c> and observed through
    /// <c>pg_locks</c> — never with wall-clock sleeps (a bounded poll only guards a hang).
    ///
    /// Invariant under test, for every accepted ordering: once a cancellation commits, no
    /// Session created by that operation remains, the <c>CanceledAt</c> tombstone is durable,
    /// and a later keyed CREATE with the same key creates nothing
    /// (<see cref="SessionCreateResult.Canceled"/>).
    /// </summary>
    [Collection(SessionCancellationPostgresCollection.Name)]
    [Trait("Category", "PostgresIntegration")]
    public sealed class SessionCreateCancellationPostgresTests
    {
        private readonly SessionCancellationPostgresFixture _pg;

        public SessionCreateCancellationPostgresTests(SessionCancellationPostgresFixture pg)
        {
            _pg = pg;
        }

        // ---- helpers ---------------------------------------------------------------------

        private static SessionCreateRequestDto Req(string name, Guid? key = null) => new()
        {
            Name = name,
            Status = SessionStatus.Draft,
            Date = DateTime.UtcNow,
            ClientOperationId = key,
        };

        private async Task<SessionCreateOutcome> CreateAsync(int userId, Guid key, string name = "s")
        {
            await using var ctx = _pg.NewContext();
            var svc = new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance);
            return await svc.CreateAsync(userId, Req(name, key), CancellationToken.None);
        }

        private async Task CancelAsync(int userId, Guid key)
        {
            await using var ctx = _pg.NewContext();
            var svc = new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance);
            await svc.CancelCreateAsync(userId, key, CancellationToken.None);
        }

        private async Task<long> ScalarAsync(string sql, params (string, object)[] ps)
        {
            await using var c = _pg.NewRawConnection();
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
            return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0L);
        }

        private async Task ExecAsync(string sql, params (string, object)[] ps)
        {
            await using var c = _pg.NewRawConnection();
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
            await cmd.ExecuteNonQueryAsync();
        }

        private Task<long> SessionCountAsync(int userId) =>
            ScalarAsync("SELECT count(*) FROM \"Sessions\" WHERE \"UserId\" = @u", ("u", userId));

        private Task<long> OperationCountAsync(int userId) =>
            ScalarAsync("SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u", ("u", userId));

        private Task<long> CanceledOperationCountAsync(int userId) =>
            ScalarAsync(
                "SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL",
                ("u", userId));

        private async Task SeedExerciseGraphAsync(int sessionId)
        {
            await using var c = _pg.NewRawConnection();
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = @"
                WITH e AS (
                    INSERT INTO ""Exercises"" (""SessionId"", ""Name"", ""SortOrder"", ""Version"")
                    VALUES (@s, 'Squat', 0, 1) RETURNING ""Id"")
                INSERT INTO ""ExerciseSets"" (""ExerciseId"", ""SetNumber"", ""Reps"", ""IsCompleted"", ""Version"")
                SELECT e.""Id"", 1, 5, false, 1 FROM e;";
            cmd.Parameters.AddWithValue("s", sessionId);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Bounded poll for <paramref name="count"/> processes to be WAITING on the derived
        /// advisory lock. The lock — not the clock — is the ordering mechanism; the deadline
        /// only bounds a hang.
        /// </summary>
        private async Task WaitForAdvisoryWaitersAsync(long lockKey, int count)
        {
            var hi = (long)(uint)(lockKey >> 32);
            var lo = (long)(uint)(lockKey & 0xFFFFFFFFL);
            var sql =
                "SELECT count(*) FROM pg_locks WHERE locktype = 'advisory' " +
                $"AND classid::bigint = {hi} AND objid::bigint = {lo} AND granted = false";

            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline)
            {
                if (await ScalarAsync(sql) >= count) return;
                await Task.Delay(50);
            }

            Assert.Fail($"timed out waiting for {count} advisory-lock waiter(s) on key {lockKey}");
        }

        // ===== A. cancellation before CREATE reaches the server ===========================

        [DockerRequiredFact]
        public async Task CancelBeforeCreate_WritesTombstone_AndTheLaterCreateReturnsCanceled_CreatingNothing()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var key = Guid.NewGuid();

            await CancelAsync(userId, key);

            Assert.Equal(1, await OperationCountAsync(userId));
            Assert.Equal(1, await CanceledOperationCountAsync(userId));
            Assert.Equal(0, await SessionCountAsync(userId));

            var outcome = await CreateAsync(userId, key, "late");

            Assert.Equal(SessionCreateResult.Canceled, outcome.Result);
            Assert.Equal(SessionCreateErrorCodes.OperationCanceled, outcome.ErrorCode);
            Assert.Equal(0, await SessionCountAsync(userId));
        }

        // ===== C. cancellation after CREATE committed ====================================

        [DockerRequiredFact]
        public async Task CancelAfterCreate_DeletesSession_KeepsTombstone_AndTheRetryReturnsCanceled()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var key = Guid.NewGuid();

            var created = await CreateAsync(userId, key, "doomed");
            Assert.Equal(SessionCreateResult.Created, created.Result);
            Assert.Equal(1, await SessionCountAsync(userId));

            await CancelAsync(userId, key);

            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(1, await OperationCountAsync(userId));
            Assert.Equal(1, await ScalarAsync(
                "SELECT count(*) FROM \"SessionCreateOperations\" " +
                "WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL AND \"SessionId\" IS NULL",
                ("u", userId)));

            var retry = await CreateAsync(userId, key, "resurrect?");
            Assert.Equal(SessionCreateResult.Canceled, retry.Result);
            Assert.Equal(0, await SessionCountAsync(userId));
        }

        // ===== Session children are removed when cancellation follows committed creation ==

        [DockerRequiredFact]
        public async Task CancelAfterCreate_RemovesOwnedExercisesAndExerciseSets_ViaCascade()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var key = Guid.NewGuid();

            var created = await CreateAsync(userId, key, "with-children");
            var sessionId = created.Session!.Id;
            await SeedExerciseGraphAsync(sessionId);

            Assert.Equal(1, await ScalarAsync("SELECT count(*) FROM \"Exercises\" WHERE \"SessionId\" = @s", ("s", sessionId)));
            Assert.Equal(1, await ScalarAsync(
                "SELECT count(*) FROM \"ExerciseSets\" es JOIN \"Exercises\" e ON e.\"Id\" = es.\"ExerciseId\" WHERE e.\"SessionId\" = @s",
                ("s", sessionId)));

            await CancelAsync(userId, key);

            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(0, await ScalarAsync("SELECT count(*) FROM \"Exercises\" WHERE \"SessionId\" = @s", ("s", sessionId)));
            Assert.Equal(0, await ScalarAsync("SELECT count(*) FROM \"ExerciseSets\"")); // whole graph gone
            Assert.Equal(1, await CanceledOperationCountAsync(userId));
        }

        // ===== D. repeated cancellation =================================================

        [DockerRequiredFact]
        public async Task RepeatedCancellation_Succeeds_AndDoesNotMoveTheTombstone()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var key = Guid.NewGuid();

            await CreateAsync(userId, key, "s");
            await CancelAsync(userId, key);

            var firstCanceledAt = await ScalarAsync(
                "SELECT extract(epoch from \"CanceledAt\")::bigint FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u",
                ("u", userId));

            await CancelAsync(userId, key);
            await CancelAsync(userId, key);

            Assert.Equal(1, await OperationCountAsync(userId));
            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(firstCanceledAt, await ScalarAsync(
                "SELECT extract(epoch from \"CanceledAt\")::bigint FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u",
                ("u", userId)));
        }

        // ===== B. cancellation while CREATE is concurrent — forced ordering: cancel first =

        [DockerRequiredFact]
        public async Task CancelDuringCreate_CancelAcquiresLockFirst_CreateThenReturnsCanceled_NoSession()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var key = Guid.NewGuid();
            var lockKey = SessionCreateService.DeriveAdvisoryLockKey(userId, key);

            await using var holder = _pg.NewRawConnection();
            await holder.OpenAsync();
            await using var holderTx = await holder.BeginTransactionAsync();
            await using (var lockCmd = holder.CreateCommand())
            {
                lockCmd.Transaction = holderTx;
                lockCmd.CommandText = "SELECT pg_advisory_xact_lock(@k)";
                lockCmd.Parameters.AddWithValue("k", lockKey);
                await lockCmd.ExecuteNonQueryAsync();
            }

            // Cancel enters the wait queue first, then create.
            var cancelTask = Task.Run(() => CancelAsync(userId, key));
            await WaitForAdvisoryWaitersAsync(lockKey, 1);
            var createTask = Task.Run(() => CreateAsync(userId, key, "was-blocked"));
            await WaitForAdvisoryWaitersAsync(lockKey, 2);

            await holderTx.CommitAsync(); // release; FIFO -> cancel runs first, then create

            await cancelTask;
            var outcome = await createTask;

            Assert.Equal(SessionCreateResult.Canceled, outcome.Result);
            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(1, await CanceledOperationCountAsync(userId));
        }

        // ===== B. cancellation while CREATE is concurrent — forced ordering: create first =

        [DockerRequiredFact]
        public async Task CancelDuringCreate_CreateAcquiresLockFirst_CancelThenDeletesTheJustCreatedSession()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var key = Guid.NewGuid();
            var lockKey = SessionCreateService.DeriveAdvisoryLockKey(userId, key);

            await using var holder = _pg.NewRawConnection();
            await holder.OpenAsync();
            await using var holderTx = await holder.BeginTransactionAsync();
            await using (var lockCmd = holder.CreateCommand())
            {
                lockCmd.Transaction = holderTx;
                lockCmd.CommandText = "SELECT pg_advisory_xact_lock(@k)";
                lockCmd.Parameters.AddWithValue("k", lockKey);
                await lockCmd.ExecuteNonQueryAsync();
            }

            // Create enters the wait queue first, then cancel.
            var createTask = Task.Run(() => CreateAsync(userId, key, "created-then-canceled"));
            await WaitForAdvisoryWaitersAsync(lockKey, 1);
            var cancelTask = Task.Run(() => CancelAsync(userId, key));
            await WaitForAdvisoryWaitersAsync(lockKey, 2);

            await holderTx.CommitAsync(); // release; FIFO -> create runs first, then cancel

            var outcome = await createTask;
            await cancelTask;

            Assert.Equal(SessionCreateResult.Created, outcome.Result);
            // Cancellation still wins: the just-created Session and its op pointer are gone,
            // the tombstone remains.
            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(1, await ScalarAsync(
                "SELECT count(*) FROM \"SessionCreateOperations\" " +
                "WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL AND \"SessionId\" IS NULL",
                ("u", userId)));

            var retry = await CreateAsync(userId, key, "retry");
            Assert.Equal(SessionCreateResult.Canceled, retry.Result);
        }

        // ===== same operation key used by different users (owner-scoped uniqueness) =======

        [DockerRequiredFact]
        public async Task SameKeyTwoUsers_CancellingOne_LeavesTheOtherUsersOperationAndSessionIntact()
        {
            Assert.True(_pg.Available);
            var userA = await _pg.SeedUserAsync();
            var userB = await _pg.SeedUserAsync();
            var sharedKey = Guid.NewGuid();

            var a = await CreateAsync(userA, sharedKey, "A");
            var b = await CreateAsync(userB, sharedKey, "B");
            Assert.Equal(SessionCreateResult.Created, a.Result);
            Assert.Equal(SessionCreateResult.Created, b.Result);

            await CancelAsync(userA, sharedKey);

            Assert.Equal(0, await SessionCountAsync(userA));
            Assert.Equal(1, await CanceledOperationCountAsync(userA));

            // User B is untouched: Session present, operation not canceled, replay still works.
            Assert.Equal(1, await SessionCountAsync(userB));
            Assert.Equal(0, await CanceledOperationCountAsync(userB));
            var bReplay = await CreateAsync(userB, sharedKey, "B-again");
            Assert.Equal(SessionCreateResult.ReplayedExisting, bReplay.Result);
            Assert.Equal(b.Session!.Id, bReplay.Session!.Id);
        }

        // ===== foreign-user isolation and non-disclosure =================================

        [DockerRequiredFact]
        public async Task ForeignUserCancel_WritesOnlyItsOwnTombstone_NeverTouchesTheOwnersRowOrSession()
        {
            Assert.True(_pg.Available);
            var owner = await _pg.SeedUserAsync();
            var stranger = await _pg.SeedUserAsync();
            var key = Guid.NewGuid();

            var created = await CreateAsync(owner, key, "owner-session");
            var ownerSessionId = created.Session!.Id;

            // The stranger cancels a key they do not own. Non-disclosing: it just succeeds.
            await CancelAsync(stranger, key);

            // Owner's row + Session are completely untouched.
            Assert.Equal(1, await SessionCountAsync(owner));
            Assert.Equal(0, await CanceledOperationCountAsync(owner));
            Assert.Equal(ownerSessionId, await ScalarAsync(
                "SELECT \"SessionId\" FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u", ("u", owner)));

            // The owner's own keyed replay still returns the owner's canonical Session.
            var ownerReplay = await CreateAsync(owner, key, "owner-again");
            Assert.Equal(SessionCreateResult.ReplayedExisting, ownerReplay.Result);
            Assert.Equal(ownerSessionId, ownerReplay.Session!.Id);

            // The stranger got an independent tombstone under (stranger, key).
            Assert.Equal(1, await CanceledOperationCountAsync(stranger));
        }

        [DockerRequiredFact]
        public async Task Cancel_WithCorruptedCrossUserSessionPointer_NeverDeletesTheOtherUsersSession()
        {
            Assert.True(_pg.Available);
            var userA = await _pg.SeedUserAsync();
            var userB = await _pg.SeedUserAsync();

            var bSession = (await CreateAsync(userB, Guid.NewGuid(), "B-private")).Session!;

            // Hand-seed a corrupted operation row for A that points at B's Session.
            var key = Guid.NewGuid();
            await ExecAsync(
                "INSERT INTO \"SessionCreateOperations\" (\"UserId\", \"ClientOperationId\", \"SessionId\", \"CompletedAt\", \"CreatedAt\") " +
                "VALUES (@u, @k, @s, now(), now())",
                ("u", userA), ("k", key), ("s", bSession.Id));

            await CancelAsync(userA, key);

            // A's row is tombstoned, but B's Session is untouched (owner-scoped delete lookup).
            Assert.Equal(1, await CanceledOperationCountAsync(userA));
            Assert.Equal(1, await SessionCountAsync(userB));
            Assert.Equal("B-private", await ScalarStringAsync(
                "SELECT \"Name\" FROM \"Sessions\" WHERE \"Id\" = @id", ("id", bSession.Id)));
        }

        private async Task<string?> ScalarStringAsync(string sql, params (string, object)[] ps)
        {
            await using var c = _pg.NewRawConnection();
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
            var v2 = await cmd.ExecuteScalarAsync();
            return v2 is null or DBNull ? null : Convert.ToString(v2);
        }

        // ===== malformed / empty operation key ==========================================

        [DockerRequiredFact]
        public async Task EmptyOperationKey_IsRejected_WithoutAnyDatabaseWrite()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();

            await CancelAsync(userId, Guid.Empty);

            Assert.Equal(0, await OperationCountAsync(userId));
            Assert.Equal(0, await SessionCountAsync(userId));
        }

        // ===== rollback / failure behavior =============================================
        // A failure inside the cancel transaction must leave nothing partial: no tombstone,
        // and the pre-existing Session still present. A one-shot NON-retryable trigger error
        // on the operation-row UPDATE forces the transaction to roll back.

        [DockerRequiredFact]
        public async Task FailureInsideTheCancelTransaction_RollsBackCleanly_LeavingSessionAndNoTombstone()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var key = Guid.NewGuid();
            var created = await CreateAsync(userId, key, "s");
            var sessionId = created.Session!.Id;

            await ExecAsync(@"
                CREATE FUNCTION _boom() RETURNS trigger AS $fn$
                BEGIN RAISE EXCEPTION 'injected non-retryable failure' USING ERRCODE = '23514'; END;
                $fn$ LANGUAGE plpgsql;
                CREATE TRIGGER _boom_trg BEFORE DELETE ON ""Sessions""
                    FOR EACH ROW EXECUTE FUNCTION _boom();");
            try
            {
                await Assert.ThrowsAnyAsync<Exception>(() => CancelAsync(userId, key));

                // Nothing partial: Session still there, operation row NOT tombstoned.
                Assert.Equal(1, await SessionCountAsync(userId));
                Assert.Equal(0, await CanceledOperationCountAsync(userId));
                Assert.Equal(sessionId, await ScalarAsync(
                    "SELECT \"SessionId\" FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u", ("u", userId)));
            }
            finally
            {
                await ExecAsync(@"
                    DROP TRIGGER IF EXISTS _boom_trg ON ""Sessions"";
                    DROP FUNCTION IF EXISTS _boom();");
            }
        }

        // ===== tombstone durability (committed row survives a fresh connection/context) ==

        [DockerRequiredFact]
        public async Task Tombstone_IsDurable_AfterContextAndConnectionReopen_StillBlocksTheKey()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var key = Guid.NewGuid();

            await CreateAsync(userId, key, "s");
            await CancelAsync(userId, key);

            // Brand-new physical connection: the tombstone is committed, not session state.
            await using (var fresh = _pg.NewRawConnection())
            {
                await fresh.OpenAsync();
                await using var cmd = fresh.CreateCommand();
                cmd.CommandText =
                    "SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL";
                cmd.Parameters.AddWithValue("u", userId);
                Assert.Equal(1L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
            }

            // Brand-new context/service: the key is still permanently unusable for CREATE.
            var retry = await CreateAsync(userId, key, "reopened");
            Assert.Equal(SessionCreateResult.Canceled, retry.Result);
            Assert.Equal(0, await SessionCountAsync(userId));
        }

        // ===== no orphan Session remains after concurrent stress iterations =============

        [DockerRequiredFact]
        public async Task ConcurrentCreateAndCancelStress_NeverLeavesAnOrphanSession()
        {
            Assert.True(_pg.Available);

            for (var i = 0; i < 25; i++)
            {
                var userId = await _pg.SeedUserAsync();
                var key = Guid.NewGuid();

                var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var createTask = Task.Run(async () => { await start.Task; return await CreateAsync(userId, key, $"race-{i}"); });
                var cancelTask = Task.Run(async () => { await start.Task; await CancelAsync(userId, key); });

                start.SetResult();
                await Task.WhenAll(createTask, cancelTask);

                // Whichever won the advisory lock, cancellation wins the outcome.
                Assert.Equal(0, await SessionCountAsync(userId));
                Assert.Equal(1, await CanceledOperationCountAsync(userId));

                var afterwards = await CreateAsync(userId, key, "after");
                Assert.Equal(SessionCreateResult.Canceled, afterwards.Result);
                Assert.Equal(0, await SessionCountAsync(userId));
            }
        }
    }
}
