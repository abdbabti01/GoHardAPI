using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Idempotent <c>from-program-workout</c> Session CREATE proven against a <b>real
    /// PostgreSQL</b> container with real overlapping transactions, the real
    /// <c>pg_advisory_xact_lock</c> serialization, and the real
    /// <c>Exercises</c> → <c>ExerciseSets</c> cascade. Create/cancel ordering is forced with
    /// an externally held advisory lock and observed through <c>pg_locks</c> — never with
    /// wall-clock sleeps (a bounded poll only guards a hang).
    ///
    /// Invariants under test: a retry after a lost response returns the original Session with
    /// its exact child count and no duplicates; the operation registry is shared with generic
    /// <c>POST /api/v1/sessions</c>; cancellation via the by-operation endpoint wins in every
    /// accepted ordering, before / during / after commit, and removes the materialized
    /// children.
    /// </summary>
    [Collection(SessionCancellationPostgresCollection.Name)]
    [Trait("Category", "PostgresIntegration")]
    public sealed class SessionCreateFromProgramWorkoutPostgresTests
    {
        private readonly SessionCancellationPostgresFixture _pg;

        public SessionCreateFromProgramWorkoutPostgresTests(SessionCancellationPostgresFixture pg)
        {
            _pg = pg;
        }

        private const string ThreeExercisesJson = """
        [ { "name": "Squat", "rest": 120 }, { "name": "Bench", "rest": 90 }, { "name": "Row" } ]
        """;

        // ---- helpers --------------------------------------------------------------------

        private SessionCreateService NewService(TrainingContext ctx) =>
            new(ctx, NullLogger<SessionCreateService>.Instance);

        private async Task<(int programId, int workoutId)> SeedProgramWorkoutAsync(
            int ownerUserId, string exercisesJson = ThreeExercisesJson, string workoutName = "Day 1")
        {
            await using var ctx = _pg.NewContext();
            var program = new GoHardAPI.Models.Program
            {
                UserId = ownerUserId,
                Title = "P",
                StartDate = new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
            };
            ctx.Programs.Add(program);
            await ctx.SaveChangesAsync();

            var workout = new ProgramWorkout
            {
                ProgramId = program.Id,
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = workoutName,
                WorkoutType = "Strength",
                ExercisesJson = exercisesJson,
            };
            ctx.ProgramWorkouts.Add(workout);
            await ctx.SaveChangesAsync();
            return (program.Id, workout.Id);
        }

        private static CreateSessionFromProgramWorkoutDto Dto(int programId, int workoutId, Guid? key = null) => new()
        {
            ProgramId = programId,
            ProgramWorkoutId = workoutId,
            ClientOperationId = key,
        };

        private async Task<SessionCreateOutcome> CreateFromWorkoutAsync(
            int userId, int programId, int workoutId, Guid? key)
        {
            await using var ctx = _pg.NewContext();
            return await NewService(ctx).CreateFromProgramWorkoutAsync(
                userId, Dto(programId, workoutId, key), CancellationToken.None);
        }

        private async Task<SessionCreateOutcome> CreateGenericAsync(int userId, Guid key, string name = "generic")
        {
            await using var ctx = _pg.NewContext();
            return await NewService(ctx).CreateAsync(
                userId, new SessionCreateRequestDto { Name = name, Status = SessionStatus.Draft, Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);
        }

        private async Task CancelAsync(int userId, Guid key)
        {
            await using var ctx = _pg.NewContext();
            await NewService(ctx).CancelCreateAsync(userId, key, CancellationToken.None);
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

        private Task<long> ExerciseCountAsync(int sessionId) =>
            ScalarAsync("SELECT count(*) FROM \"Exercises\" WHERE \"SessionId\" = @s", ("s", sessionId));

        // User-scoped so the assertion is independent of other tests sharing the container.
        private Task<long> ExerciseCountForUserAsync(int userId) =>
            ScalarAsync(
                "SELECT count(*) FROM \"Exercises\" e JOIN \"Sessions\" s ON s.\"Id\" = e.\"SessionId\" WHERE s.\"UserId\" = @u",
                ("u", userId));

        private Task<long> ExerciseSetCountForUserAsync(int userId) =>
            ScalarAsync(
                "SELECT count(*) FROM \"ExerciseSets\" es " +
                "JOIN \"Exercises\" e ON e.\"Id\" = es.\"ExerciseId\" " +
                "JOIN \"Sessions\" s ON s.\"Id\" = e.\"SessionId\" WHERE s.\"UserId\" = @u",
                ("u", userId));

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

        // ===== 1. first keyed creation ================================================

        [DockerRequiredFact]
        public async Task FirstKeyedCreate_RealTransaction_CreatesOneSession_ThreeExercises_OneCompletedOperationRow()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            var outcome = await CreateFromWorkoutAsync(userId, p, w, key);

            Assert.Equal(SessionCreateResult.Created, outcome.Result);
            Assert.Equal(key, outcome.Session!.ClientOperationId);
            Assert.Equal(1, await SessionCountAsync(userId));
            Assert.Equal(3, await ExerciseCountAsync(outcome.Session!.Id));
            Assert.Equal(1, await ScalarAsync(
                "SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u AND \"ClientOperationId\" = @k " +
                "AND \"SessionId\" = @s AND \"CompletedAt\" IS NOT NULL AND \"CanceledAt\" IS NULL",
                ("u", userId), ("k", key), ("s", outcome.Session!.Id)));
        }

        // ===== 2. sequential retry ====================================================

        [DockerRequiredFact]
        public async Task SequentialRetry_SameSessionId_ExactChildCount_NoNewExercises()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            var first = await CreateFromWorkoutAsync(userId, p, w, key);
            var replay = await CreateFromWorkoutAsync(userId, p, w, key);

            Assert.Equal(SessionCreateResult.Created, first.Result);
            Assert.Equal(SessionCreateResult.ReplayedExisting, replay.Result);
            Assert.Equal(first.Session!.Id, replay.Session!.Id);
            Assert.Equal(1, await SessionCountAsync(userId));
            Assert.Equal(3, await ExerciseCountAsync(first.Session!.Id));
            Assert.Equal(1, await OperationCountAsync(userId));
        }

        // ===== 3. concurrent identical requests, separate contexts ====================

        [DockerRequiredFact]
        public async Task TenConcurrentIdenticalRequests_SeparateContexts_ExactlyOneSession_ThreeExercises_OneOperationRow()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tasks = Enumerable.Range(0, 10).Select(async _ =>
            {
                await start.Task;
                return await CreateFromWorkoutAsync(userId, p, w, key);
            }).ToArray();

            start.SetResult();
            var outcomes = await Task.WhenAll(tasks);

            Assert.Equal(1, outcomes.Count(o => o.Result == SessionCreateResult.Created));
            Assert.Equal(9, outcomes.Count(o => o.Result == SessionCreateResult.ReplayedExisting));
            var ids = outcomes.Select(o => o.Session!.Id).Distinct().ToList();
            Assert.Single(ids);
            Assert.Equal(1, await SessionCountAsync(userId));
            Assert.Equal(3, await ExerciseCountAsync(ids[0])); // NOT 30
            Assert.Equal(1, await OperationCountAsync(userId));
        }

        // ===== 4. materially different input, same key -> replay original =============

        [DockerRequiredFact]
        public async Task MateriallyDifferentInput_SameKey_ReplaysOriginal_NoSecondSession()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p1, w1) = await SeedProgramWorkoutAsync(userId, ThreeExercisesJson, "First");
            var (p2, w2) = await SeedProgramWorkoutAsync(userId, """[ {"name":"only"} ]""", "Second");
            var key = Guid.NewGuid();

            var first = await CreateFromWorkoutAsync(userId, p1, w1, key);
            var replay = await CreateFromWorkoutAsync(userId, p2, w2, key);

            Assert.Equal(SessionCreateResult.ReplayedExisting, replay.Result);
            Assert.Equal(first.Session!.Id, replay.Session!.Id);
            Assert.Equal(w1, replay.Session!.ProgramWorkoutId);
            Assert.Equal(1, await SessionCountAsync(userId));
            Assert.Equal(3, await ExerciseCountAsync(first.Session!.Id));
        }

        // ===== 5. cross-endpoint key reuse (shared registry) =========================

        [DockerRequiredFact]
        public async Task CrossEndpointKeyReuse_GenericThenProgramWorkout_ReplaysGenericSession_NoExercises()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            var generic = await CreateGenericAsync(userId, key);
            var pw = await CreateFromWorkoutAsync(userId, p, w, key);

            Assert.Equal(SessionCreateResult.Created, generic.Result);
            Assert.Equal(SessionCreateResult.ReplayedExisting, pw.Result);
            Assert.Equal(generic.Session!.Id, pw.Session!.Id);
            Assert.Equal(0, await ExerciseCountAsync(pw.Session!.Id));
            Assert.Equal(1, await SessionCountAsync(userId));
            Assert.Equal(1, await OperationCountAsync(userId));
        }

        [DockerRequiredFact]
        public async Task CrossEndpointKeyReuse_ProgramWorkoutThenGeneric_ReplaysProgramWorkoutSession_KeepsExercises()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            var pw = await CreateFromWorkoutAsync(userId, p, w, key);
            var generic = await CreateGenericAsync(userId, key);

            Assert.Equal(SessionCreateResult.Created, pw.Result);
            Assert.Equal(SessionCreateResult.ReplayedExisting, generic.Result);
            Assert.Equal(pw.Session!.Id, generic.Session!.Id);
            Assert.Equal(3, await ExerciseCountAsync(pw.Session!.Id));
            Assert.Equal(1, await SessionCountAsync(userId));
        }

        // ===== 6. different users, same key ==========================================

        [DockerRequiredFact]
        public async Task DifferentUsers_SameKey_CreateIndependentSessions()
        {
            Assert.True(_pg.Available);
            var userA = await _pg.SeedUserAsync();
            var userB = await _pg.SeedUserAsync();
            var (pa, wa) = await SeedProgramWorkoutAsync(userA);
            var (pb, wb) = await SeedProgramWorkoutAsync(userB);
            var shared = Guid.NewGuid();

            var a = await CreateFromWorkoutAsync(userA, pa, wa, shared);
            var b = await CreateFromWorkoutAsync(userB, pb, wb, shared);

            Assert.Equal(SessionCreateResult.Created, a.Result);
            Assert.Equal(SessionCreateResult.Created, b.Result);
            Assert.NotEqual(a.Session!.Id, b.Session!.Id);

            var bReplay = await CreateFromWorkoutAsync(userB, pb, wb, shared);
            Assert.Equal(SessionCreateResult.ReplayedExisting, bReplay.Result);
            Assert.Equal(b.Session!.Id, bReplay.Session!.Id);
            Assert.NotEqual(a.Session!.Id, bReplay.Session!.Id);
        }

        // ===== 7. foreign-owned program / workout ====================================

        [DockerRequiredFact]
        public async Task ForeignOwnedProgramWorkout_Keyed_ProgramNotFound_NothingWritten()
        {
            Assert.True(_pg.Available);
            var attacker = await _pg.SeedUserAsync();
            var victim = await _pg.SeedUserAsync();
            var (vp, vw) = await SeedProgramWorkoutAsync(victim);

            var outcome = await CreateFromWorkoutAsync(attacker, vp, vw, Guid.NewGuid());

            Assert.Equal(SessionCreateResult.ProgramNotFound, outcome.Result);
            Assert.Equal(0, await SessionCountAsync(attacker));
            Assert.Equal(0, await OperationCountAsync(attacker));
            Assert.Equal(0, await ExerciseCountAsync(-1));
            // victim's workout untouched
            Assert.Equal("Day 1", (await ScalarStringAsync(
                "SELECT \"WorkoutName\" FROM \"ProgramWorkouts\" WHERE \"Id\" = @w", ("w", vw))));
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

        // ===== 7b. empty-GUID operation key is rejected before any persistence =========

        [DockerRequiredFact]
        public async Task EmptyGuidKey_Returns_InvalidOperationKey_WritesNoSessionExerciseOrOperationRow()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);

            var outcome = await CreateFromWorkoutAsync(userId, p, w, Guid.Empty);

            Assert.Equal(SessionCreateResult.InvalidOperationKey, outcome.Result);
            Assert.Equal("invalid_operation_key", outcome.ErrorCode);
            Assert.Null(outcome.Session);
            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(0, await OperationCountAsync(userId));
            Assert.Equal(0, await ExerciseCountForUserAsync(userId));
            // A subsequent real key still works — the empty-key rejection left no residue.
            var ok = await CreateFromWorkoutAsync(userId, p, w, Guid.NewGuid());
            Assert.Equal(SessionCreateResult.Created, ok.Result);
        }

        // ===== 8. source workout changes after initial creation ======================

        [DockerRequiredFact]
        public async Task SourceWorkoutChangedAfterCreation_Replay_ReturnsOriginal_NeverRematerializes()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            var first = await CreateFromWorkoutAsync(userId, p, w, key);

            await ExecAsync(
                "UPDATE \"ProgramWorkouts\" SET \"ExercisesJson\" = @j, \"WorkoutName\" = 'RENAMED' WHERE \"Id\" = @w",
                ("j", """[ {"name":"X"},{"name":"Y"},{"name":"Z"},{"name":"W"} ]"""), ("w", w));

            var replay = await CreateFromWorkoutAsync(userId, p, w, key);

            Assert.Equal(SessionCreateResult.ReplayedExisting, replay.Result);
            Assert.Equal(first.Session!.Id, replay.Session!.Id);
            Assert.Equal(3, await ExerciseCountAsync(first.Session!.Id)); // not 4
            Assert.Equal("Day 1", await ScalarStringAsync(
                "SELECT \"Name\" FROM \"Sessions\" WHERE \"Id\" = @s", ("s", first.Session!.Id)));
        }

        [DockerRequiredFact]
        public async Task SourceWorkoutDeletedAfterCreation_Replay_StillReturnsOriginalSession()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            var first = await CreateFromWorkoutAsync(userId, p, w, key);
            var sessionId = first.Session!.Id;

            // Deleting the workout cascades to the Session (FK ON DELETE CASCADE) -> the
            // completed operation's SessionId is blanked (ON DELETE SET NULL) -> 410 Gone,
            // never recreated from the vanished template.
            await ExecAsync("DELETE FROM \"ProgramWorkouts\" WHERE \"Id\" = @w", ("w", w));

            var replay = await CreateFromWorkoutAsync(userId, p, w, key);
            Assert.Equal(SessionCreateResult.Gone, replay.Result);
            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(1, await OperationCountAsync(userId));
        }

        // ===== 9. cancellation before creation =======================================

        [DockerRequiredFact]
        public async Task CancelBeforeCreate_ThenKeyedProgramWorkoutCreate_ReturnsCanceled_CreatesNothing()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            await CancelAsync(userId, key);

            var outcome = await CreateFromWorkoutAsync(userId, p, w, key);
            Assert.Equal(SessionCreateResult.Canceled, outcome.Result);
            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(1, await ScalarAsync(
                "SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL",
                ("u", userId)));
        }

        // ===== 10. forced ordering: cancel acquires the lock first ===================

        [DockerRequiredFact]
        public async Task ForcedOrdering_CancelAcquiresLockFirst_ProgramWorkoutCreateReturnsCanceled_NoSessionNoExercises()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
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

            var cancelTask = Task.Run(() => CancelAsync(userId, key));
            await WaitForAdvisoryWaitersAsync(lockKey, 1);
            var createTask = Task.Run(() => CreateFromWorkoutAsync(userId, p, w, key));
            await WaitForAdvisoryWaitersAsync(lockKey, 2);

            await holderTx.CommitAsync(); // FIFO: cancel first, then create

            await cancelTask;
            var outcome = await createTask;

            Assert.Equal(SessionCreateResult.Canceled, outcome.Result);
            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(0, await ExerciseCountForUserAsync(userId));
            Assert.Equal(1, await ScalarAsync(
                "SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL",
                ("u", userId)));
        }

        // ===== 11. forced ordering: create acquires the lock first ===================

        [DockerRequiredFact]
        public async Task ForcedOrdering_CreateAcquiresLockFirst_CancelThenDeletesTheJustCreatedSessionAndExercises()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
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

            var createTask = Task.Run(() => CreateFromWorkoutAsync(userId, p, w, key));
            await WaitForAdvisoryWaitersAsync(lockKey, 1);
            var cancelTask = Task.Run(() => CancelAsync(userId, key));
            await WaitForAdvisoryWaitersAsync(lockKey, 2);

            await holderTx.CommitAsync(); // FIFO: create first, then cancel

            var outcome = await createTask;
            await cancelTask;

            Assert.Equal(SessionCreateResult.Created, outcome.Result);
            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(0, await ExerciseCountForUserAsync(userId));
            Assert.Equal(0, await ExerciseSetCountForUserAsync(userId));
            Assert.Equal(1, await ScalarAsync(
                "SELECT count(*) FROM \"SessionCreateOperations\" " +
                "WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL AND \"SessionId\" IS NULL", ("u", userId)));

            var retry = await CreateFromWorkoutAsync(userId, p, w, key);
            Assert.Equal(SessionCreateResult.Canceled, retry.Result);
        }

        // ===== 12. cancellation after creation removes children =======================

        [DockerRequiredFact]
        public async Task CancelAfterCreate_RemovesSessionExercisesAndSets_KeepsTombstone_RetryReturnsCanceled()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            var created = await CreateFromWorkoutAsync(userId, p, w, key);
            var sessionId = created.Session!.Id;
            Assert.Equal(3, await ExerciseCountAsync(sessionId));

            // Add a set under one of the materialized exercises to prove the deep cascade.
            await ExecAsync(
                "INSERT INTO \"ExerciseSets\" (\"ExerciseId\", \"SetNumber\", \"Reps\", \"IsCompleted\", \"Version\") " +
                "SELECT \"Id\", 1, 5, false, 1 FROM \"Exercises\" WHERE \"SessionId\" = @s LIMIT 1", ("s", sessionId));
            Assert.Equal(1, await ExerciseSetCountForUserAsync(userId));

            await CancelAsync(userId, key);

            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(0, await ScalarAsync("SELECT count(*) FROM \"Exercises\" WHERE \"SessionId\" = @s", ("s", sessionId)));
            Assert.Equal(0, await ExerciseSetCountForUserAsync(userId));
            Assert.Equal(1, await ScalarAsync(
                "SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL AND \"SessionId\" IS NULL",
                ("u", userId)));

            var retry = await CreateFromWorkoutAsync(userId, p, w, key);
            Assert.Equal(SessionCreateResult.Canceled, retry.Result);
            Assert.Equal(0, await SessionCountAsync(userId));
        }

        // ===== 12b. a stranger's cancel never touches the owner's materialized graph ==

        [DockerRequiredFact]
        public async Task ForeignUserCancel_LeavesOwnersProgramWorkoutSession_AndItsExercisesAndSets_Intact()
        {
            Assert.True(_pg.Available);
            var owner = await _pg.SeedUserAsync();
            var stranger = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(owner);
            var key = Guid.NewGuid();

            var created = await CreateFromWorkoutAsync(owner, p, w, key);
            var sessionId = created.Session!.Id;
            await ExecAsync(
                "INSERT INTO \"ExerciseSets\" (\"ExerciseId\", \"SetNumber\", \"Reps\", \"IsCompleted\", \"Version\") " +
                "SELECT \"Id\", 1, 5, false, 1 FROM \"Exercises\" WHERE \"SessionId\" = @s", ("s", sessionId));

            // The stranger cancels the same key value (owner-scoped: writes only its own tombstone).
            await CancelAsync(stranger, key);

            Assert.Equal(1, await SessionCountAsync(owner));
            Assert.Equal(3, await ExerciseCountAsync(sessionId));
            Assert.Equal(3, await ExerciseSetCountForUserAsync(owner));
            Assert.Equal(0, await ScalarAsync(
                "SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL",
                ("u", owner)));

            // The owner's own keyed retry still replays the canonical Session unchanged.
            var ownerReplay = await CreateFromWorkoutAsync(owner, p, w, key);
            Assert.Equal(SessionCreateResult.ReplayedExisting, ownerReplay.Result);
            Assert.Equal(sessionId, ownerReplay.Session!.Id);
            Assert.Equal(1, await ScalarAsync(
                "SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL",
                ("u", stranger)));
        }

        // ===== 13. retry after ordinary Session deletion =============================

        [DockerRequiredFact]
        public async Task RetryAfterOrdinarySessionDeletion_Returns410_NeverRecreated()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            var created = await CreateFromWorkoutAsync(userId, p, w, key);
            await ExecAsync("DELETE FROM \"Sessions\" WHERE \"Id\" = @s", ("s", created.Session!.Id));

            var retry = await CreateFromWorkoutAsync(userId, p, w, key);
            Assert.Equal(SessionCreateResult.Gone, retry.Result);
            Assert.Equal(0, await SessionCountAsync(userId));
            Assert.Equal(1, await OperationCountAsync(userId));
        }

        // ===== 14. rollback during child creation ====================================

        [DockerRequiredFact]
        public async Task RollbackDuringChildCreation_LeavesNothingPartial_NoOperationRow_ThenRetrySucceeds()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
            var key = Guid.NewGuid();

            // Non-retryable failure on the FIRST insert into Exercises forces the first-write
            // transaction (Session + Exercises + operation row) to roll back as a unit.
            await ExecAsync(@"
                CREATE FUNCTION _boom_ex() RETURNS trigger AS $fn$
                BEGIN RAISE EXCEPTION 'injected non-retryable failure' USING ERRCODE = '23514'; END;
                $fn$ LANGUAGE plpgsql;
                CREATE TRIGGER _boom_ex_trg BEFORE INSERT ON ""Exercises""
                    FOR EACH ROW EXECUTE FUNCTION _boom_ex();");
            try
            {
                await Assert.ThrowsAnyAsync<Exception>(() => CreateFromWorkoutAsync(userId, p, w, key));

                Assert.Equal(0, await SessionCountAsync(userId));
                Assert.Equal(0, await OperationCountAsync(userId));
                Assert.Equal(0, await ExerciseCountForUserAsync(userId));
            }
            finally
            {
                await ExecAsync(@"
                    DROP TRIGGER IF EXISTS _boom_ex_trg ON ""Exercises"";
                    DROP FUNCTION IF EXISTS _boom_ex();");
            }

            // No tombstone was written, so a retry is a fresh first write and succeeds.
            var retry = await CreateFromWorkoutAsync(userId, p, w, key);
            Assert.Equal(SessionCreateResult.Created, retry.Result);
            Assert.Equal(3, await ExerciseCountAsync(retry.Session!.Id));
            Assert.Equal(1, await OperationCountAsync(userId));
        }

        // ===== 15. advisory lock is load-bearing =====================================

        [DockerRequiredFact]
        public async Task KeyedProgramWorkoutCreate_WaitsOnTheDerivedAdvisoryLock_WhenHeldElsewhere()
        {
            Assert.True(_pg.Available);
            var userId = await _pg.SeedUserAsync();
            var (p, w) = await SeedProgramWorkoutAsync(userId);
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

            var createTask = Task.Run(() => CreateFromWorkoutAsync(userId, p, w, key));
            await WaitForAdvisoryWaitersAsync(lockKey, 1);
            Assert.False(createTask.IsCompleted, "keyed create returned while the advisory lock was held elsewhere");

            await holderTx.CommitAsync();

            var outcome = await createTask;
            Assert.Equal(SessionCreateResult.Created, outcome.Result);
            Assert.Equal(1, await SessionCountAsync(userId));
            Assert.Equal(3, await ExerciseCountAsync(outcome.Session!.Id));
        }

        // ===== 16. concurrent create + cancel stress =================================

        [DockerRequiredFact]
        public async Task ConcurrentCreateAndCancelStress_NeverLeavesAnOrphanSessionOrExercise()
        {
            Assert.True(_pg.Available);

            for (var i = 0; i < 15; i++)
            {
                var userId = await _pg.SeedUserAsync();
                var (p, w) = await SeedProgramWorkoutAsync(userId);
                var key = Guid.NewGuid();

                var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var createTask = Task.Run(async () => { await start.Task; return await CreateFromWorkoutAsync(userId, p, w, key); });
                var cancelTask = Task.Run(async () => { await start.Task; await CancelAsync(userId, key); });

                start.SetResult();
                await Task.WhenAll(createTask, cancelTask);

                Assert.Equal(0, await SessionCountAsync(userId));
                Assert.Equal(1, await ScalarAsync(
                    "SELECT count(*) FROM \"SessionCreateOperations\" WHERE \"UserId\" = @u AND \"CanceledAt\" IS NOT NULL",
                    ("u", userId)));

                var afterwards = await CreateFromWorkoutAsync(userId, p, w, key);
                Assert.Equal(SessionCreateResult.Canceled, afterwards.Result);
                Assert.Equal(0, await ExerciseCountForUserAsync(userId)); // no orphan children this iteration
            }
        }
    }
}
