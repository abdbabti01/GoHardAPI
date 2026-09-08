using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Occurrence-key-focused coverage of <c>POST /api/v1/sessions/from-program-workout</c>,
    /// alongside the general keyed-CREATE coverage in
    /// <see cref="SessionCreateFromProgramWorkoutHttpTests"/>. Same InMemory-provider /
    /// directly-constructed-controller approach; real persisted-normalization/CAS behavior is
    /// proven against a real database in <c>ProgramWorkoutOccurrenceKeyPostgresTests</c>.
    /// </summary>
    public class SessionCreateFromProgramWorkoutOccurrenceKeyTests
    {
        private static TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static SessionsController Controller(TrainingContext ctx, int userId = 1) =>
            new(ctx, new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance))
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

        private static async Task SeedUser(TrainingContext ctx, int id)
        {
            ctx.Users.Add(new User { Id = id, Name = $"u{id}", Email = $"u{id}@x.com", PasswordHash = "h" });
            await ctx.SaveChangesAsync();
        }

        private static async Task<(int programId, int workoutId)> SeedProgramWorkout(
            TrainingContext ctx, int ownerUserId, string exercisesJson)
        {
            var program = new GoHardAPI.Models.Program
            {
                UserId = ownerUserId,
                Title = "P",
                StartDate = new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc),
            };
            ctx.Programs.Add(program);
            await ctx.SaveChangesAsync();

            var workout = new ProgramWorkout
            {
                ProgramId = program.Id,
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
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

        private static Session SessionOf(ActionResult<Session> result) => result.Result switch
        {
            CreatedAtActionResult c => Assert.IsType<Session>(c.Value),
            OkObjectResult o => Assert.IsType<Session>(o.Value),
            _ => throw new Xunit.Sdk.XunitException($"not a session-bearing result: {result.Result?.GetType().Name}"),
        };

        private const string PreKeyedJson = """
        [
          { "name": "Squat", "occurrenceKey": "occ-squat" },
          { "name": "Bench Press", "occurrenceKey": "occ-bench" }
        ]
        """;

        private const string UnkeyedJson = """
        [ { "name": "Squat" }, { "name": "Bench Press" } ]
        """;

        [Fact]
        public async Task KeyedCreate_CopiesExactOccurrenceKeys_FromSourceWorkout()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1, PreKeyedJson);

            var result = await Controller(ctx).CreateSessionFromProgramWorkout(
                Dto(p, w, Guid.NewGuid()), CancellationToken.None);

            var session = SessionOf(result);
            var keys = session.Exercises.Select(e => e.OccurrenceKey).OrderBy(k => k).ToList();
            Assert.Equal(new[] { "occ-bench", "occ-squat" }, keys);
        }

        [Fact]
        public async Task KeyedCreate_SourceWorkoutMissingKeys_StillGetsStableKeysOnEachExercise()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1, UnkeyedJson);

            var result = await Controller(ctx).CreateSessionFromProgramWorkout(
                Dto(p, w, Guid.NewGuid()), CancellationToken.None);

            var session = SessionOf(result);
            Assert.All(session.Exercises, e => Assert.False(string.IsNullOrWhiteSpace(e.OccurrenceKey)));
            // Distinct even though both entries omitted a key.
            Assert.Equal(2, session.Exercises.Select(e => e.OccurrenceKey).Distinct().Count());
        }

        [Fact]
        public async Task Replay_AfterSourceWorkoutEdited_StillReturnsTheOriginalSessionsOriginalKeys()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1, PreKeyedJson);
            var key = Guid.NewGuid();

            var first = SessionOf(await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None));
            var firstKeys = first.Exercises.Select(e => e.OccurrenceKey).OrderBy(k => k).ToList();

            // Source workout changes after the fact — replay must never re-read it.
            var workout = await ctx.ProgramWorkouts.FirstAsync(x => x.Id == w);
            workout.ExercisesJson = """[ { "name": "Deadlift", "occurrenceKey": "occ-new" } ]""";
            await ctx.SaveChangesAsync();

            var replay = SessionOf(await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None));

            Assert.Equal(first.Id, replay.Id);
            var replayKeys = replay.Exercises.Select(e => e.OccurrenceKey).OrderBy(k => k).ToList();
            Assert.Equal(firstKeys, replayKeys);
            Assert.DoesNotContain("occ-new", replayKeys);
        }

        [Fact]
        public async Task TwoIndependentKeyedCreates_FromTheSameProgramWorkout_BothSucceed()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1, PreKeyedJson);

            var sessionA = SessionOf(await Controller(ctx).CreateSessionFromProgramWorkout(
                Dto(p, w, Guid.NewGuid()), CancellationToken.None));
            var sessionB = SessionOf(await Controller(ctx).CreateSessionFromProgramWorkout(
                Dto(p, w, Guid.NewGuid()), CancellationToken.None));

            Assert.NotEqual(sessionA.Id, sessionB.Id);
            // Both sessions' exercises carry the same occurrence keys (same source workout) —
            // occurrenceKey is scoped per workout, not globally unique across Exercises.
            var keysA = sessionA.Exercises.Select(e => e.OccurrenceKey).OrderBy(k => k).ToList();
            var keysB = sessionB.Exercises.Select(e => e.OccurrenceKey).OrderBy(k => k).ToList();
            Assert.Equal(keysA, keysB);
            Assert.Equal(2, ctx.Sessions.Count(s => s.ProgramWorkoutId == w));
        }

        [Fact]
        public async Task UnkeyedLegacyCreate_AlsoCopiesOccurrenceKeys()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1, PreKeyedJson);

            var result = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w), CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var session = Assert.IsType<Session>(created.Value);
            var keys = session.Exercises.Select(e => e.OccurrenceKey).OrderBy(k => k).ToList();
            Assert.Equal(new[] { "occ-bench", "occ-squat" }, keys);
        }
    }
}
