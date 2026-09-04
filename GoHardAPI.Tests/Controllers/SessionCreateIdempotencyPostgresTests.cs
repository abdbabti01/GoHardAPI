using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Keyed Session CREATE proven against a <b>real PostgreSQL</b> container with multiple
    /// independent Npgsql connections. Serialization is proven by real overlapping
    /// transactions and by observing <c>pg_locks</c> — never by wall-clock sleeps (a short
    /// timeout only guards against a hang; it is not the ordering mechanism).
    /// </summary>
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "PostgresIntegration")]
    public class SessionCreateIdempotencyPostgresTests
    {
        private readonly PostgresFixture _pg;
        private static int _userSeq = 100_000;

        public SessionCreateIdempotencyPostgresTests(PostgresFixture pg)
        {
            _pg = pg;
        }

        private static int NextUserId() => Interlocked.Increment(ref _userSeq);

        // ---- helpers ------------------------------------------------------------------------

        private async Task SeedUserAsync(int id)
        {
            await using var c = _pg.NewRawConnection();
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO \"Users\" (\"Id\", \"Name\", \"Email\") VALUES (@id, @n, @e)";
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("n", $"user{id}");
            cmd.Parameters.AddWithValue("e", $"user{id}@example.com");
            await cmd.ExecuteNonQueryAsync();
        }

        private static SessionCreateRequestDto Req(string name, Guid? key = null) => new()
        {
            Name = name,
            Status = SessionStatus.Draft,
            Date = DateTime.UtcNow,
            ClientOperationId = key,
        };

        private async Task<SessionCreateOutcome> CreateAsync(int userId, SessionCreateRequestDto dto)
        {
            await using var ctx = _pg.NewContext();
            var svc = new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance);
            return await svc.CreateAsync(userId, dto, CancellationToken.None);
        }

        private async Task<long> ScalarAsync(string sql)
        {
            await using var c = _pg.NewRawConnection();
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0L);
        }

        private Task<long> SessionCountAsync(int userId) =>
            ScalarAsync($"SELECT count(*) FROM \"Sessions\" WHERE \"UserId\" = {userId}");

        private Task<long> OperationCountAsync(int userId) =>
            ScalarAsync($"SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = {userId}");

        // ---- 1: new keyed CREATE -> 201, one Session, one operation row --------------------

        [DockerRequiredFact]
        public async Task NewKeyedCreate_Returns201_CreatesExactlyOneSessionAndOneOperationRow()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var key = Guid.NewGuid();

            var outcome = await CreateAsync(userId, Req("first", key));

            Assert.Equal(SessionCreateResult.Created, outcome.Result);
            Assert.NotNull(outcome.Session);
            Assert.Equal(1, await SessionCountAsync(userId));
            Assert.Equal(1, await OperationCountAsync(userId));
            Assert.Equal(1, await ScalarAsync(
                $"SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = {userId} " +
                $"AND \"ClientOperationId\" = '{key}' AND \"SessionId\" = {outcome.Session!.Id} " +
                $"AND \"CompletedAt\" IS NOT NULL AND \"CanceledAt\" IS NULL"));
        }

        // ---- 2: identical replay -> 200, same canonical Session ---------------------------

        [DockerRequiredFact]
        public async Task IdenticalReplay_Returns200_WithSameCanonicalSession()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var key = Guid.NewGuid();

            var first = await CreateAsync(userId, Req("canonical", key));
            var replay = await CreateAsync(userId, Req("canonical", key));

            Assert.Equal(SessionCreateResult.Created, first.Result);
            Assert.Equal(SessionCreateResult.ReplayedExisting, replay.Result);
            Assert.Equal(first.Session!.Id, replay.Session!.Id);
            Assert.Equal(1, await SessionCountAsync(userId));
        }

        // ---- 3: conflicting replay -> original canonical, nothing changed ----------------

        [DockerRequiredFact]
        public async Task ConflictingReplay_ReturnsOriginalCanonical_AndChangesNothing()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var key = Guid.NewGuid();

            var first = await CreateAsync(userId, Req("ORIGINAL", key));
            var replay = await CreateAsync(userId, Req("OVERWRITE-ATTEMPT", key));

            Assert.Equal(SessionCreateResult.ReplayedExisting, replay.Result);
            Assert.Equal(first.Session!.Id, replay.Session!.Id);
            Assert.Equal("ORIGINAL", replay.Session!.Name);
            Assert.Equal("ORIGINAL", await ScalarNameAsync(first.Session!.Id));
            Assert.Equal(1, await SessionCountAsync(userId));
        }

        private async Task<string> ScalarNameAsync(int sessionId)
        {
            await using var c = _pg.NewRawConnection();
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = $"SELECT \"Name\" FROM \"Sessions\" WHERE \"Id\" = {sessionId}";
            return (string)(await cmd.ExecuteScalarAsync())!;
        }

        // ---- 4: ten concurrent requests, same (UserId, key) -> exactly one Session -------

        [DockerRequiredFact]
        public async Task TenConcurrentSameKey_CreateExactlyOneSession_OneGets201_RestGet200()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var key = Guid.NewGuid();

            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                await start.Task;
                return await CreateAsync(userId, Req($"racer-{i}", key));
            }).ToArray();

            start.SetResult();
            var outcomes = await Task.WhenAll(tasks);

            Assert.Equal(1, outcomes.Count(o => o.Result == SessionCreateResult.Created));
            Assert.Equal(9, outcomes.Count(o => o.Result == SessionCreateResult.ReplayedExisting));
            Assert.Single(outcomes.Select(o => o.Session!.Id).Distinct());
            Assert.Equal(1, await SessionCountAsync(userId));
            Assert.Equal(1, await OperationCountAsync(userId));
        }

        // ---- 5: different keys -> different Sessions ------------------------------------

        [DockerRequiredFact]
        public async Task DifferentKeys_CreateDifferentSessions()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);

            var a = await CreateAsync(userId, Req("a", Guid.NewGuid()));
            var b = await CreateAsync(userId, Req("b", Guid.NewGuid()));

            Assert.NotEqual(a.Session!.Id, b.Session!.Id);
            Assert.Equal(2, await SessionCountAsync(userId));
            Assert.Equal(2, await OperationCountAsync(userId));
        }

        // ---- 6 + 7 + 16: same key under users A and B -> independent, isolated ----------

        [DockerRequiredFact]
        public async Task SameKeyUnderTwoUsers_CreatesIndependentSessions_AndBCannotReachAsData()
        {
            Assert.True(_pg.Available);
            var userA = NextUserId();
            var userB = NextUserId();
            await SeedUserAsync(userA);
            await SeedUserAsync(userB);
            var sharedKey = Guid.NewGuid();

            var a = await CreateAsync(userA, Req("A-canonical", sharedKey));
            var b = await CreateAsync(userB, Req("B-canonical", sharedKey));

            // Owner-scoped uniqueness: both keyed creates succeed under the shared key.
            Assert.Equal(SessionCreateResult.Created, a.Result);
            Assert.Equal(SessionCreateResult.Created, b.Result);
            Assert.NotEqual(a.Session!.Id, b.Session!.Id);
            Assert.Equal(userA, a.Session!.UserId);
            Assert.Equal(userB, b.Session!.UserId);

            // B presenting the shared key never returns A's Session, and A's row is untouched.
            var bReplay = await CreateAsync(userB, Req("B-again", sharedKey));
            Assert.Equal(SessionCreateResult.ReplayedExisting, bReplay.Result);
            Assert.Equal(b.Session!.Id, bReplay.Session!.Id);
            Assert.NotEqual(a.Session!.Id, bReplay.Session!.Id);
            Assert.Equal("A-canonical", await ScalarNameAsync(a.Session!.Id));
            Assert.Equal(1, await ScalarAsync(
                $"SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = {userA} AND \"ClientOperationId\" = '{sharedKey}'"));
        }

        // ---- 7b: canonical-Session lookup is itself owner-scoped (defense in depth) -----
        // If a (corrupted) operation row for user A pointed SessionId at user B's Session,
        // the "s.UserId == userId" filter must still refuse to hand B's Session to A.

        [DockerRequiredFact]
        public async Task CompletedOperation_PointingAtAnotherUsersSession_Returns410_NotThatSession()
        {
            Assert.True(_pg.Available);
            var userA = NextUserId();
            var userB = NextUserId();
            await SeedUserAsync(userA);
            await SeedUserAsync(userB);

            var bSession = (await CreateAsync(userB, Req("B-private", Guid.NewGuid()))).Session!;

            var key = Guid.NewGuid();
            await SeedOperationAsync(userA, key, sessionId: bSession.Id, completed: true, canceled: false);

            var outcome = await CreateAsync(userA, Req("A-replay", key));

            Assert.Equal(SessionCreateResult.Gone, outcome.Result);
            Assert.Null(outcome.Session);
            Assert.Equal("B-private", await ScalarNameAsync(bSession.Id)); // B's row untouched
            Assert.Equal(0, await SessionCountAsync(userA));
        }

        // ---- 8: canceled operation seeded -> 409, creates nothing ----------------------

        [DockerRequiredFact]
        public async Task CanceledOperation_Returns409_operation_canceled_AndCreatesNothing()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var key = Guid.NewGuid();
            await SeedOperationAsync(userId, key, sessionId: null, completed: false, canceled: true);

            var outcome = await CreateAsync(userId, Req("attempt", key));

            Assert.Equal(SessionCreateResult.Canceled, outcome.Result);
            Assert.Equal(SessionCreateErrorCodes.OperationCanceled, outcome.ErrorCode);
            Assert.Equal(0, await SessionCountAsync(userId));
        }

        // ---- 9: operation whose Session was deleted -> 410, never recreated ------------

        [DockerRequiredFact]
        public async Task CompletedOperationWhoseSessionWasDeleted_Returns410_AndNeverRecreates()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var key = Guid.NewGuid();

            var created = await CreateAsync(userId, Req("doomed", key));
            var sessionId = created.Session!.Id;

            // Draft reaper / cascade deletes the Session; ON DELETE SET NULL blanks the pointer.
            await ExecAsync($"DELETE FROM \"Sessions\" WHERE \"Id\" = {sessionId}");
            Assert.Equal(1, await ScalarAsync(
                $"SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = {userId} AND \"SessionId\" IS NULL AND \"CompletedAt\" IS NOT NULL"));

            var replay = await CreateAsync(userId, Req("resurrect?", key));

            Assert.Equal(SessionCreateResult.Gone, replay.Result);
            Assert.Equal(SessionCreateErrorCodes.OperationTargetDeleted, replay.ErrorCode);
            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(1, await OperationCountAsync(userId)); // operation row still there, still SessionId NULL
        }

        // ---- 10: legacy missing-key request -> current 201 behavior preserved ----------

        [DockerRequiredFact]
        public async Task LegacyMissingKey_PreservesCurrent201Behavior_AndWritesNoOperationRow()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);

            var a = await CreateAsync(userId, Req("legacy-1"));
            var b = await CreateAsync(userId, Req("legacy-2"));

            Assert.Equal(SessionCreateResult.Created, a.Result);
            Assert.Equal(SessionCreateResult.Created, b.Result);
            Assert.NotEqual(a.Session!.Id, b.Session!.Id);
            Assert.Equal(2, await SessionCountAsync(userId));
            Assert.Equal(0, await OperationCountAsync(userId));
            Assert.Equal(2, await ScalarAsync(
                $"SELECT count(*) FROM \"Sessions\" WHERE \"UserId\" = {userId} AND \"ClientOperationId\" IS NULL"));
        }

        // ---- 11: raw over-posting cannot assign Id/UserId/Version or create children ----

        [DockerRequiredFact]
        public async Task KeyedCreate_CannotAssignServerControlledColumns()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var key = Guid.NewGuid();

            var outcome = await CreateAsync(userId, Req("scalar-only", key));

            Assert.Equal(userId, outcome.Session!.UserId);
            Assert.Equal(1, outcome.Session!.Version);
            Assert.True(outcome.Session!.Id > 0);
            // No Exercises table exists in the fixture schema — proof the create never
            // touches a child graph.
            Assert.Equal(0, await ScalarAsync(
                "SELECT count(*) FROM information_schema.tables WHERE table_name = 'Exercises'"));
        }

        // ---- 17: the keyed create genuinely acquires the derived advisory lock ----------
        // No conflicting row is created here, so the ONLY thing that can serialize the
        // service against the externally-held lock is its own pg_advisory_xact_lock call
        // on the same derived key. Removing that call makes this test fail (the create
        // returns immediately), so the lock is load-bearing, not decorative.

        [DockerRequiredFact]
        public async Task KeyedCreate_WaitsOnTheDerivedAdvisoryLock_WhenItIsHeldElsewhere()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var key = Guid.NewGuid();
            var lockKey = SessionCreateService.DeriveAdvisoryLockKey(userId, key);
            var hi = (long)(uint)(lockKey >> 32);
            var lo = (long)(uint)(lockKey & 0xFFFFFFFFL);
            string LockRows(string granted) =>
                $"SELECT count(*) FROM pg_locks WHERE locktype = 'advisory' " +
                $"AND classid::bigint = {hi} AND objid::bigint = {lo} AND granted = {granted}";

            await using var holder = _pg.NewRawConnection();
            await holder.OpenAsync();
            await using var holderTx = await holder.BeginTransactionAsync();
            await ExecOnAsync(holder, holderTx, "SELECT pg_advisory_xact_lock(@k)", ("k", lockKey));
            Assert.Equal(1, await ScalarAsync(LockRows("true")));

            // The service must block purely on the advisory lock (no row conflict exists).
            var createTask = Task.Run(() => CreateAsync(userId, Req("was-blocked", key)));

            // Poll (bounded) for the create to become an observable WAITER on the same key.
            // The advisory lock — not the clock — is the ordering mechanism; the timeout
            // only bounds a hang.
            var deadline = DateTime.UtcNow.AddSeconds(8);
            long waiters = 0;
            while (DateTime.UtcNow < deadline && (waiters = await ScalarAsync(LockRows("false"))) < 1)
            {
                Assert.False(createTask.IsCompleted, "keyed create returned while the advisory lock was held elsewhere");
                await Task.Delay(100);
            }
            Assert.True(waiters >= 1, "expected the keyed create to be waiting on the derived advisory lock");
            Assert.False(createTask.IsCompleted);

            await holderTx.CommitAsync(); // release the lock

            var outcome = await createTask;
            Assert.Equal(SessionCreateResult.Created, outcome.Result);
            Assert.Equal(1, await SessionCountAsync(userId));
            Assert.Equal(1, await OperationCountAsync(userId));
        }

        // ---- 12: a transaction that rolls back leaves neither a Session nor an op row ---

        [DockerRequiredFact]
        public async Task ConcurrentRolledBackWork_LeavesNothingPartial_AndTheKeyedCreateStillSucceedsExactlyOnce()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var key = Guid.NewGuid();
            var lockKey = SessionCreateService.DeriveAdvisoryLockKey(userId, key);

            await using var holder = _pg.NewRawConnection();
            await holder.OpenAsync();
            await using var holderTx = await holder.BeginTransactionAsync();

            // Take the same advisory lock, insert op + session rows, then ROLL BACK.
            await ExecOnAsync(holder, holderTx, "SELECT pg_advisory_xact_lock(@k)", ("k", lockKey));
            await ExecOnAsync(holder, holderTx,
                "INSERT INTO \"SessionCreateOperations\" (\"UserId\", \"ClientOperationId\", \"CreatedAt\") VALUES (@u, @k, now())",
                ("u", userId), ("k", key));
            await ExecOnAsync(holder, holderTx,
                "INSERT INTO \"Sessions\" (\"UserId\", \"Date\", \"Status\", \"Version\") VALUES (@u, now(), 'draft', 1)",
                ("u", userId));

            var createTask = Task.Run(() => CreateAsync(userId, Req("after-rollback", key)));
            Assert.False(await Task.WhenAny(createTask, Task.Delay(2000)) == createTask,
                "keyed create ran before the other transaction released the lock");

            await holderTx.RollbackAsync();

            var outcome = await createTask;
            Assert.Equal(SessionCreateResult.Created, outcome.Result);
            Assert.Equal(1, await SessionCountAsync(userId));   // no leftover partial row
            Assert.Equal(1, await OperationCountAsync(userId)); // no leftover partial row
        }

        // ---- retry path: a retryable failure on the first attempt recovers to exactly one -

        [DockerRequiredFact]
        public async Task RetryableFailureOnFirstInsert_RecoversToExactlyOneSessionAndOneOperationRow()
        {
            Assert.True(_pg.Available);
            var userId = NextUserId();
            await SeedUserAsync(userId);
            var key = Guid.NewGuid();

            // Arm a one-shot BEFORE INSERT trigger that raises PostgreSQL 40P01 (deadlock,
            // retryable) on the FIRST insert into SessionCreateOperations, then never again
            // (nextval advances even across the aborted transaction).
            await ExecAsync(@"
                CREATE SEQUENCE _fail_once_seq;
                CREATE FUNCTION _fail_once() RETURNS trigger AS $fn$
                BEGIN
                    IF nextval('_fail_once_seq') = 1 THEN
                        RAISE EXCEPTION 'injected retryable failure' USING ERRCODE = '40P01';
                    END IF;
                    RETURN NEW;
                END; $fn$ LANGUAGE plpgsql;
                CREATE TRIGGER _fail_once_trg BEFORE INSERT ON ""SessionCreateOperations""
                    FOR EACH ROW EXECUTE FUNCTION _fail_once();");
            try
            {
                var outcome = await CreateAsync(userId, Req("survives-retry", key));

                Assert.Equal(SessionCreateResult.Created, outcome.Result);
                Assert.Equal(1, await SessionCountAsync(userId));
                Assert.Equal(1, await OperationCountAsync(userId));
                Assert.Equal(2, await ScalarAsync("SELECT last_value FROM _fail_once_seq")); // fired twice: fail, then succeed
            }
            finally
            {
                await ExecAsync(@"
                    DROP TRIGGER IF EXISTS _fail_once_trg ON ""SessionCreateOperations"";
                    DROP FUNCTION IF EXISTS _fail_once();
                    DROP SEQUENCE IF EXISTS _fail_once_seq;");
            }
        }

        // ---- helpers for raw seeding / exec -------------------------------------------------

        private async Task SeedOperationAsync(int userId, Guid key, int? sessionId, bool completed, bool canceled)
        {
            await using var c = _pg.NewRawConnection();
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText =
                "INSERT INTO \"SessionCreateOperations\" " +
                "(\"UserId\", \"ClientOperationId\", \"SessionId\", \"CompletedAt\", \"CanceledAt\", \"CreatedAt\") " +
                "VALUES (@u, @k, @s, @done, @cancel, now())";
            cmd.Parameters.AddWithValue("u", userId);
            cmd.Parameters.AddWithValue("k", key);
            cmd.Parameters.AddWithValue("s", (object?)sessionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("done", completed ? DateTime.UtcNow : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("cancel", canceled ? DateTime.UtcNow : (object)DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task ExecAsync(string sql)
        {
            await using var c = _pg.NewRawConnection();
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task ExecOnAsync(
            NpgsqlConnection conn, NpgsqlTransaction tx, string sql, params (string, object)[] ps)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            foreach (var (n, v) in ps)
            {
                cmd.Parameters.AddWithValue(n, v);
            }
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
