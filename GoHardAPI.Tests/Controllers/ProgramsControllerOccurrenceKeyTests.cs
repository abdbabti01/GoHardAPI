using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Occurrence-key coverage for <see cref="ProgramsController"/>'s writers
    /// (<c>AddWorkout</c>/<c>UpdateWorkout</c>/<c>CreateProgram</c>) and readers
    /// (<c>GetProgram</c>/<c>GetWeekWorkouts</c>) — validation contract, verbatim preservation
    /// through reorder/edit, and the GET-time self-heal actually persisting through EF's
    /// relational <c>ExecuteUpdateAsync</c>. Uses a real SQLite database (not the EF InMemory
    /// provider) specifically so the persisted compare-and-swap write in
    /// <c>ProgramWorkoutExerciseOccurrences.EnsurePersistedAsync</c> actually runs — InMemory
    /// doesn't support <c>ExecuteUpdateAsync</c> and this normalizer deliberately degrades to an
    /// in-memory-only computation there (see
    /// <see cref="SessionCreateFromProgramWorkoutOccurrenceKeyTests"/> for that path). Real
    /// PostgreSQL concurrency coverage lives in <c>ProgramWorkoutOccurrenceKeyPostgresTests</c>.
    /// </summary>
    public class ProgramsControllerOccurrenceKeyTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly TrainingContext _context;

        public ProgramsControllerOccurrenceKeyTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _context = new TrainingContext(
                new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_connection).Options);
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        private static ProgramsController Controller(TrainingContext ctx, int userId) =>
            new(ctx)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth")),
                    },
                },
            };

        private async Task<int> SeedUserAndProgram(int userId)
        {
            _context.Users.Add(new User { Id = userId, Name = $"u{userId}", Username = $"u{userId}", Email = $"u{userId}@x.com", PasswordHash = "h" });
            var program = new GoHardAPI.Models.Program
            {
                UserId = userId,
                Title = "P",
                StartDate = new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc),
            };
            _context.Programs.Add(program);
            await _context.SaveChangesAsync();
            return program.Id;
        }

        private static string[] KeysOf(string exercisesJson) =>
            JsonSerializer.Deserialize<JsonElement[]>(exercisesJson)!
                .Select(e => e.TryGetProperty("occurrenceKey", out var k) ? k.GetString() : null)
                .ToArray()!;

        // ---- AddWorkout: validation contract -------------------------------------------------

        [Fact]
        public async Task AddWorkout_MissingKeys_AreFilledIn_AndPersisted()
        {
            var programId = await SeedUserAndProgram(1);
            var workout = new ProgramWorkout
            {
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = """[ { "name": "Squat" }, { "name": "Bench Press" } ]""",
            };

            var result = await Controller(_context, 1).AddWorkout(programId, workout);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var body = Assert.IsType<ProgramWorkout>(created.Value);
            var keys = KeysOf(body.ExercisesJson);
            Assert.All(keys, k => Assert.False(string.IsNullOrWhiteSpace(k)));
            Assert.Equal(2, keys.Distinct().Count());

            var stored = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == body.Id);
            Assert.Equal(keys, KeysOf(stored.ExercisesJson));
        }

        [Fact]
        public async Task AddWorkout_SuppliedValidDistinctKeys_ArePreservedVerbatim()
        {
            var programId = await SeedUserAndProgram(1);
            var workout = new ProgramWorkout
            {
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = """[ { "name": "Squat", "occurrenceKey": "occ-1" } ]""",
            };

            var result = await Controller(_context, 1).AddWorkout(programId, workout);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var body = Assert.IsType<ProgramWorkout>(created.Value);
            Assert.Equal(new[] { "occ-1" }, KeysOf(body.ExercisesJson));
        }

        [Theory]
        [InlineData("""[ { "name": "X", "occurrenceKey": "" } ]""")]
        [InlineData("""[ { "name": "X", "occurrenceKey": "   " } ]""")]
        public async Task AddWorkout_MalformedKey_Returns400(string exercisesJson)
        {
            var programId = await SeedUserAndProgram(1);
            var workout = new ProgramWorkout
            {
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = exercisesJson,
            };

            var result = await Controller(_context, 1).AddWorkout(programId, workout);

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(_context.ProgramWorkouts);
        }

        [Fact]
        public async Task AddWorkout_DuplicateSuppliedKeys_Returns400()
        {
            var programId = await SeedUserAndProgram(1);
            var workout = new ProgramWorkout
            {
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = """
                [
                  { "name": "A", "occurrenceKey": "same" },
                  { "name": "B", "occurrenceKey": "same" }
                ]
                """,
            };

            var result = await Controller(_context, 1).AddWorkout(programId, workout);

            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(_context.ProgramWorkouts);
        }

        [Fact]
        public async Task AddWorkout_ForeignUsersProgram_Returns404_BeforeAnyNormalization()
        {
            var ownerProgramId = await SeedUserAndProgram(1);
            _context.Users.Add(new User { Id = 2, Name = "u2", Username = "u2", Email = "u2@x.com", PasswordHash = "h" });
            await _context.SaveChangesAsync();

            var workout = new ProgramWorkout
            {
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = """[ { "name": "Squat" } ]""",
            };

            var result = await Controller(_context, 2).AddWorkout(ownerProgramId, workout);

            Assert.IsType<NotFoundResult>(result.Result);
            Assert.Empty(_context.ProgramWorkouts);
        }

        // ---- UpdateWorkout: reorder / edit preserve identity; malformed/duplicate rejected --

        [Fact]
        public async Task UpdateWorkout_ReorderedArray_PreservesSuppliedKeys()
        {
            var programId = await SeedUserAndProgram(1);
            var addResult = await Controller(_context, 1).AddWorkout(programId, new ProgramWorkout
            {
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = """
                [
                  { "name": "A", "occurrenceKey": "k-a" },
                  { "name": "B", "occurrenceKey": "k-b" }
                ]
                """,
            });
            var workoutId = Assert.IsType<ProgramWorkout>(
                Assert.IsType<CreatedAtActionResult>(addResult.Result).Value).Id;

            var reordered = new ProgramWorkout
            {
                Id = workoutId,
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = """
                [
                  { "name": "B", "occurrenceKey": "k-b" },
                  { "name": "A", "occurrenceKey": "k-a" }
                ]
                """,
            };

            var updateResult = await Controller(_context, 1).UpdateWorkout(workoutId, reordered);

            Assert.IsType<NoContentResult>(updateResult);
            var stored = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.Equal(new[] { "k-b", "k-a" }, KeysOf(stored.ExercisesJson));
        }

        [Fact]
        public async Task UpdateWorkout_DuplicateSuppliedKeys_Returns400_AndLeavesExistingDataUnchanged()
        {
            var programId = await SeedUserAndProgram(1);
            const string original = """[ { "name": "A", "occurrenceKey": "k-a" } ]""";
            var addResult = await Controller(_context, 1).AddWorkout(programId, new ProgramWorkout
            {
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = original,
            });
            var workoutId = Assert.IsType<ProgramWorkout>(
                Assert.IsType<CreatedAtActionResult>(addResult.Result).Value).Id;

            var badUpdate = new ProgramWorkout
            {
                Id = workoutId,
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = """
                [
                  { "name": "A", "occurrenceKey": "dup" },
                  { "name": "B", "occurrenceKey": "dup" }
                ]
                """,
            };

            var result = await Controller(_context, 1).UpdateWorkout(workoutId, badUpdate);

            Assert.IsType<BadRequestObjectResult>(result);
            var stored = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.Equal(original, stored.ExercisesJson);
        }

        [Fact]
        public async Task UpdateWorkout_ForeignUsersWorkout_Returns404_AndLeavesItUnchanged()
        {
            var ownerProgramId = await SeedUserAndProgram(1);
            _context.Users.Add(new User { Id = 2, Name = "u2", Username = "u2", Email = "u2@x.com", PasswordHash = "h" });
            await _context.SaveChangesAsync();

            const string original = """[ { "name": "A", "occurrenceKey": "k-a" } ]""";
            var addResult = await Controller(_context, 1).AddWorkout(ownerProgramId, new ProgramWorkout
            {
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = original,
            });
            var workoutId = Assert.IsType<ProgramWorkout>(
                Assert.IsType<CreatedAtActionResult>(addResult.Result).Value).Id;

            var attempt = new ProgramWorkout
            {
                Id = workoutId,
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Hijacked",
                ExercisesJson = """[ { "name": "Z", "occurrenceKey": "z" } ]""",
            };

            var result = await Controller(_context, 2).UpdateWorkout(workoutId, attempt);

            Assert.IsType<NotFoundResult>(result);
            var stored = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.Equal(original, stored.ExercisesJson);
            Assert.Equal("Day 1", stored.WorkoutName);
        }

        // ---- GET self-heal: fills in and persists once, never regenerates on a later read ----

        [Fact]
        public async Task GetProgram_LegacyWorkoutMissingKeys_SelfHeals_AndPersistsExactlyOnce()
        {
            var programId = await SeedUserAndProgram(1);
            // Bypass the controller (which would already normalize) to simulate a legacy row
            // written before this feature existed.
            _context.ProgramWorkouts.Add(new ProgramWorkout
            {
                ProgramId = programId,
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Legacy",
                ExercisesJson = """[ { "name": "Squat" }, { "name": "Squat" } ]""",
            });
            await _context.SaveChangesAsync();

            var first = await Controller(_context, 1).GetProgram(programId);
            var firstProgram = Assert.IsType<GoHardAPI.Models.Program>(
                Assert.IsType<OkObjectResult>(first.Result).Value);
            var firstKeys = KeysOf(firstProgram.Workouts.Single().ExercisesJson);
            Assert.All(firstKeys, k => Assert.False(string.IsNullOrWhiteSpace(k)));
            Assert.Equal(2, firstKeys.Distinct().Count());

            var storedAfterFirst = await _context.ProgramWorkouts.AsNoTracking()
                .FirstAsync(w => w.ProgramId == programId);
            Assert.Equal(firstKeys, KeysOf(storedAfterFirst.ExercisesJson));

            // A second read — through a genuinely fresh DbContext/connection handle sharing the
            // same underlying SQLite database, not the identity-map instance the first call
            // populated — must reuse the persisted keys, never regenerate them.
            await using var freshContext = new TrainingContext(
                new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_connection).Options);
            var second = await Controller(freshContext, 1).GetProgram(programId);
            var secondProgram = Assert.IsType<GoHardAPI.Models.Program>(
                Assert.IsType<OkObjectResult>(second.Result).Value);
            Assert.Equal(firstKeys, KeysOf(secondProgram.Workouts.Single().ExercisesJson));
        }

        [Fact]
        public async Task GetProgram_ForeignUser_Returns404_NeverNormalizesAnotherUsersData()
        {
            var ownerProgramId = await SeedUserAndProgram(1);
            _context.Users.Add(new User { Id = 2, Name = "u2", Username = "u2", Email = "u2@x.com", PasswordHash = "h" });
            _context.ProgramWorkouts.Add(new ProgramWorkout
            {
                ProgramId = ownerProgramId,
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Legacy",
                ExercisesJson = """[ { "name": "Squat" } ]""",
            });
            await _context.SaveChangesAsync();

            var result = await Controller(_context, 2).GetProgram(ownerProgramId);

            Assert.IsType<NotFoundResult>(result.Result);
            var stored = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.ProgramId == ownerProgramId);
            Assert.DoesNotContain("occurrenceKey", stored.ExercisesJson);
        }

        // ---- ActivateDraftProgram: self-heal followed by an unrelated SaveChangesAsync -------

        /// <summary>
        /// Regression coverage for the exact scenario flagged in review: self-heal
        /// (<see cref="ProgramsController.SelfHealWorkoutOccurrenceKeysAsync"/>, invoked at the
        /// top of <c>ActivateDraftProgram</c>) neutralizes its own tracked change via
        /// <c>PropertyEntry.OriginalValue</c>, and the method THEN calls
        /// <c>SaveChangesAsync</c> for unrelated <c>Program</c> fields (<c>Status</c>,
        /// <c>IsActive</c>) on the <b>same</b> context/entity graph — with no
        /// <c>request.StartDate</c> supplied, so the only other per-workout mutation
        /// (<c>ScheduledDate</c>) never runs and the healed <c>ProgramWorkout</c> entity is not
        /// touched again before that save. This must not throw, must not re-issue a write for
        /// <c>ExercisesJson</c>, and the self-healed keys must survive exactly as persisted by
        /// the compare-and-swap.
        /// </summary>
        [Fact]
        public async Task ActivateDraftProgram_SelfHealsLegacyWorkout_ThenSavesUnrelatedProgramFields_WithoutError()
        {
            _context.Users.Add(new User { Id = 1, Name = "u1", Username = "u1", Email = "u1@x.com", PasswordHash = "h" });
            var program = new GoHardAPI.Models.Program
            {
                UserId = 1,
                Title = "Draft P",
                Status = "draft",
                StartDate = new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc),
            };
            _context.Programs.Add(program);
            await _context.SaveChangesAsync();

            _context.ProgramWorkouts.Add(new ProgramWorkout
            {
                ProgramId = program.Id,
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Legacy",
                ExercisesJson = """[ { "name": "Squat" }, { "name": "Bench Press" } ]""",
            });
            await _context.SaveChangesAsync();

            // No StartDate in the request: the ScheduledDate-recalculation branch (the only
            // other per-workout mutation in this action) does not run, so the healed workout
            // entity reaches SaveChangesAsync having been touched ONLY by the self-heal.
            var result = await Controller(_context, 1).ActivateDraftProgram(program.Id, request: null);

            Assert.IsType<OkObjectResult>(result);

            var stored = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.ProgramId == program.Id);
            var keys = KeysOf(stored.ExercisesJson);
            Assert.All(keys, k => Assert.False(string.IsNullOrWhiteSpace(k)));
            Assert.Equal(2, keys.Distinct().Count());

            var storedProgram = await _context.Programs.AsNoTracking().FirstAsync(p => p.Id == program.Id);
            Assert.Equal("active", storedProgram.Status);
            Assert.True(storedProgram.IsActive);
        }
    }
}
