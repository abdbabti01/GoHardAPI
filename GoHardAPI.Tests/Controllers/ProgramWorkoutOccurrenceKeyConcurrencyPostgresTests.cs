using System;
using System.Linq;
using System.Text.Json;
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
    /// Real-PostgreSQL coverage of <see cref="ProgramWorkoutExerciseOccurrences"/>'s persisted
    /// self-heal: the compare-and-swap write actually persists through EF's relational
    /// <c>ExecuteUpdateAsync</c>, never clobbers a concurrent edit, and is not re-run once a
    /// row is already normalized. Reuses <see cref="SessionCancellationPostgresFixture"/>
    /// (schema built from the live model via <c>EnsureCreated</c>) rather than a dedicated
    /// fixture — this suite needs the full current schema, not a legacy pre-migration one
    /// (that is <see cref="ProgramWorkoutOccurrenceKeyMigrationPostgresTests"/>'s job).
    ///
    /// Concurrency is coordinated deterministically by explicit sequencing (capture original
    /// text, apply the "concurrent" write, then run the CAS with the stale original) — never
    /// wall-clock sleeps or real thread races.
    /// </summary>
    [Collection(SessionCancellationPostgresCollection.Name)]
    [Trait("Category", "PostgresIntegration")]
    public sealed class ProgramWorkoutOccurrenceKeyConcurrencyPostgresTests
    {
        private readonly SessionCancellationPostgresFixture _pg;

        public ProgramWorkoutOccurrenceKeyConcurrencyPostgresTests(SessionCancellationPostgresFixture pg)
        {
            _pg = pg;
        }

        private async Task<(int userId, int programId, int workoutId)> SeedLegacyWorkoutAsync(string exercisesJson)
        {
            var userId = await _pg.SeedUserAsync();
            await using var ctx = _pg.NewContext();
            var program = new GoHardAPI.Models.Program
            {
                UserId = userId,
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
                WorkoutName = "Legacy",
                ExercisesJson = exercisesJson,
            };
            ctx.ProgramWorkouts.Add(workout);
            await ctx.SaveChangesAsync();
            return (userId, program.Id, workout.Id);
        }

        private static string? KeyAt(string exercisesJson, int index)
        {
            var entries = JsonSerializer.Deserialize<JsonElement[]>(exercisesJson)!;
            return entries[index].TryGetProperty("occurrenceKey", out var k) ? k.GetString() : null;
        }

        [DockerRequiredFact]
        public async Task EnsurePersistedAsync_LostRace_AdoptsTheConcurrentWritersAlreadyNormalizedContent()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var (_, _, workoutId) = await SeedLegacyWorkoutAsync("""[ { "name": "Squat" } ]""");

            // "Reader" loads the stale, still-unkeyed row.
            await using var readerCtx = _pg.NewContext();
            var stale = await readerCtx.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);

            // A concurrent editor (e.g. UpdateWorkout) changes the row to different, already
            // fully-keyed content before the reader's self-heal write lands.
            await using (var writerCtx = _pg.NewContext())
            {
                var tracked = await writerCtx.ProgramWorkouts.FirstAsync(w => w.Id == workoutId);
                tracked.ExercisesJson = """[ { "name": "Deadlift", "occurrenceKey": "concurrent-key" } ]""";
                await writerCtx.SaveChangesAsync();
            }

            // The reader's CAS is scoped to the ORIGINAL (now-stale) text, so it must lose the
            // race — and, per EnsurePersistedAsync's contract, adopt the winner's content
            // rather than handing back its own now-divergent locally-generated key.
            var result = await ProgramWorkoutExerciseOccurrences.EnsurePersistedAsync(
                readerCtx, stale, CancellationToken.None);

            Assert.Equal("concurrent-key", KeyAt(result, 0));

            await using var verifyCtx = _pg.NewContext();
            var current = await verifyCtx.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.Equal("concurrent-key", KeyAt(current.ExercisesJson, 0));
            Assert.Contains("Deadlift", current.ExercisesJson);
        }

        [DockerRequiredFact]
        public async Task EnsurePersistedAsync_PersistsOnce_SecondCallIsANoOp_KeyNeverRegenerated()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var (_, _, workoutId) = await SeedLegacyWorkoutAsync("""[ { "name": "Row" } ]""");

            await using var ctx1 = _pg.NewContext();
            var workout1 = await ctx1.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            var firstResult = await ProgramWorkoutExerciseOccurrences.EnsurePersistedAsync(ctx1, workout1, CancellationToken.None);
            var firstKey = KeyAt(firstResult, 0);
            Assert.False(string.IsNullOrWhiteSpace(firstKey));

            await using var verify1 = _pg.NewContext();
            var stored1 = await verify1.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.Equal(firstKey, KeyAt(stored1.ExercisesJson, 0));

            // A second self-heal pass over the now-normalized row must be a pure no-op.
            await using var ctx2 = _pg.NewContext();
            var workout2 = await ctx2.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            var secondResult = await ProgramWorkoutExerciseOccurrences.EnsurePersistedAsync(ctx2, workout2, CancellationToken.None);

            Assert.Equal(stored1.ExercisesJson, secondResult);
            Assert.Equal(firstKey, KeyAt(secondResult, 0));
        }

        [DockerRequiredFact]
        public async Task TwoIndependentKeyedSessionCreates_FromTheSameProgramWorkout_BothSucceed_RealTransactionsAndLocks()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var (userId, programId, workoutId) = await SeedLegacyWorkoutAsync(
                """[ { "name": "Squat", "occurrenceKey": "occ-1" } ]""");

            var dto = new CreateSessionFromProgramWorkoutDto
            {
                ProgramId = programId,
                ProgramWorkoutId = workoutId,
            };

            await using var ctxA = _pg.NewContext();
            var serviceA = new SessionCreateService(ctxA, NullLogger<SessionCreateService>.Instance);
            dto.ClientOperationId = Guid.NewGuid();
            var outcomeA = await serviceA.CreateFromProgramWorkoutAsync(userId, dto, CancellationToken.None);

            await using var ctxB = _pg.NewContext();
            var serviceB = new SessionCreateService(ctxB, NullLogger<SessionCreateService>.Instance);
            dto.ClientOperationId = Guid.NewGuid();
            var outcomeB = await serviceB.CreateFromProgramWorkoutAsync(userId, dto, CancellationToken.None);

            Assert.Equal(SessionCreateResult.Created, outcomeA.Result);
            Assert.Equal(SessionCreateResult.Created, outcomeB.Result);
            Assert.NotEqual(outcomeA.Session!.Id, outcomeB.Session!.Id);

            await using var verifyCtx = _pg.NewContext();
            var exercisesA = await verifyCtx.Exercises.AsNoTracking()
                .Where(e => e.SessionId == outcomeA.Session!.Id).ToListAsync();
            var exercisesB = await verifyCtx.Exercises.AsNoTracking()
                .Where(e => e.SessionId == outcomeB.Session!.Id).ToListAsync();

            Assert.Equal("occ-1", exercisesA.Single().OccurrenceKey);
            Assert.Equal("occ-1", exercisesB.Single().OccurrenceKey);
        }
    }
}
