using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
    /// Controller-level coverage of <c>POST /api/v1/sessions/from-program-workout</c> with
    /// the new optional <c>clientOperationId</c>: outcome-to-HTTP mapping, replay without
    /// duplicated children, cross-endpoint key reuse against the shared
    /// <see cref="SessionCreateOperation"/> registry, ownership/non-disclosure, and the
    /// backward-compatible unkeyed path.
    ///
    /// Uses the isolated EF InMemory provider and a directly-constructed
    /// <see cref="SessionsController"/> (same approach as
    /// <see cref="SessionCreateHttpBindingTests"/>); real transaction / advisory-lock /
    /// concurrency behavior is proven in
    /// <see cref="SessionCreateFromProgramWorkoutPostgresTests"/>.
    /// </summary>
    public class SessionCreateFromProgramWorkoutHttpTests
    {
        private static TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static SessionsController Controller(TrainingContext ctx, int userId = 1, SessionCreateService? service = null)
        {
            var controller = new SessionsController(
                ctx, service ?? new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            return controller;
        }

        /// <summary>Returns Created pointing at a Session id that does not exist, to drive the
        /// controller's "canonical reload came back null" branch.</summary>
        private sealed class GhostService : SessionCreateService
        {
            private readonly int _userId;
            public GhostService(TrainingContext ctx, int userId)
                : base(ctx, NullLogger<SessionCreateService>.Instance) => _userId = userId;

            public override Task<SessionCreateOutcome> CreateFromProgramWorkoutAsync(
                int userId, CreateSessionFromProgramWorkoutDto request, CancellationToken cancellationToken) =>
                Task.FromResult(SessionCreateOutcome.Created(new Session { Id = 987654321, UserId = _userId }));
        }

        private const string ThreeExercisesJson = """
        [
          { "name": "Squat", "rest": 120, "exerciseTemplateId": null },
          { "name": "Bench Press", "rest": 90 },
          { "name": "Row", "notes": "tempo" }
        ]
        """;

        private static async Task SeedUser(TrainingContext ctx, int id)
        {
            ctx.Users.Add(new User { Id = id, Name = $"u{id}", Email = $"u{id}@x.com", PasswordHash = "h" });
            await ctx.SaveChangesAsync();
        }

        /// <summary>Seeds a program + one workout owned by <paramref name="ownerUserId"/>.</summary>
        private static async Task<(int programId, int workoutId)> SeedProgramWorkout(
            TrainingContext ctx, int ownerUserId, string exercisesJson = ThreeExercisesJson)
        {
            var program = new GoHardAPI.Models.Program
            {
                UserId = ownerUserId,
                Title = "P",
                StartDate = new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc), // a Monday, well in the past
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

        // ===== 1. first keyed creation =================================================

        [Fact]
        public async Task KeyedFirstCreate_Returns201_WithSessionAndMaterializedExercises_AndOneOperationRow()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var key = Guid.NewGuid();

            var result = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var session = Assert.IsType<Session>(created.Value);
            Assert.Equal(key, session.ClientOperationId);
            Assert.Equal(3, session.Exercises.Count);
            Assert.Equal(p, session.ProgramId);
            Assert.Equal(w, session.ProgramWorkoutId);

            Assert.Equal(1, ctx.Sessions.Count(s => s.UserId == 1));
            Assert.Equal(3, ctx.Exercises.Count(e => e.SessionId == session.Id));
            var op = Assert.Single(ctx.SessionCreateOperations.Where(o => o.UserId == 1));
            Assert.Equal(key, op.ClientOperationId);
            Assert.Equal(session.Id, op.SessionId);
            Assert.NotNull(op.CompletedAt);
            Assert.Null(op.CanceledAt);
        }

        // ===== 2. sequential retry: same Session, exact child count, no duplicates =======

        [Fact]
        public async Task KeyedSequentialRetry_Returns200_SameSessionId_SameExactChildCount()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var key = Guid.NewGuid();

            var first = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);
            var firstSession = Assert.IsType<Session>(Assert.IsType<CreatedAtActionResult>(first.Result).Value);

            var replay = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);
            var replaySession = Assert.IsType<Session>(Assert.IsType<OkObjectResult>(replay.Result).Value);

            Assert.Equal(firstSession.Id, replaySession.Id);
            Assert.Equal(3, replaySession.Exercises.Count);
            Assert.Equal(1, ctx.Sessions.Count(s => s.UserId == 1));
            Assert.Equal(3, ctx.Exercises.Count(e => e.SessionId == firstSession.Id)); // NOT 6
            Assert.Single(ctx.SessionCreateOperations.Where(o => o.UserId == 1));
        }

        // ===== 3. completed retry never re-materializes from a changed template =========

        [Fact]
        public async Task KeyedReplay_AfterProgramWorkoutExercisesChanged_ReturnsOriginal_NoRematerialize()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var key = Guid.NewGuid();

            var first = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);
            var firstSession = SessionOf(first);

            // Source workout template changes (more exercises, different name).
            var workout = await ctx.ProgramWorkouts.FirstAsync(x => x.Id == w);
            workout.ExercisesJson = """[ {"name":"A"}, {"name":"B"}, {"name":"C"}, {"name":"D"}, {"name":"E"} ]""";
            workout.WorkoutName = "RENAMED";
            await ctx.SaveChangesAsync();

            var replay = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);
            var replaySession = Assert.IsType<Session>(Assert.IsType<OkObjectResult>(replay.Result).Value);

            Assert.Equal(firstSession.Id, replaySession.Id);
            Assert.Equal("Day 1", replaySession.Name);          // original, not "RENAMED"
            Assert.Equal(3, ctx.Exercises.Count(e => e.SessionId == firstSession.Id)); // still 3
        }

        // ===== 4. omitted key -> unchanged legacy behavior =============================

        [Fact]
        public async Task OmittedKey_PreservesLegacyBehavior_201_NewSessionEachCall_NoOperationRow()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);

            var a = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w), CancellationToken.None);
            var b = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w), CancellationToken.None);

            var sa = Assert.IsType<Session>(Assert.IsType<CreatedAtActionResult>(a.Result).Value);
            var sb = Assert.IsType<Session>(Assert.IsType<CreatedAtActionResult>(b.Result).Value);
            Assert.NotEqual(sa.Id, sb.Id);
            Assert.Null(sa.ClientOperationId);
            Assert.Equal(2, ctx.Sessions.Count(s => s.UserId == 1));
            Assert.Empty(ctx.SessionCreateOperations);
        }

        // ===== malformed / empty key ==================================================

        [Fact]
        public void MalformedKey_FailsDtoBinding()
        {
            var o = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<CreateSessionFromProgramWorkoutDto>(
                """{ "programWorkoutId": 1, "programId": 1, "clientOperationId": "not-a-guid" }""", o));
        }

        [Fact]
        public async Task EmptyGuidKey_Returns400_invalid_operation_key_AndPersistsNothing()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);

            var result = await Controller(ctx).CreateSessionFromProgramWorkout(
                Dto(p, w, Guid.Empty), CancellationToken.None);

            var br = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(SessionCancelErrorCodes.InvalidOperationKey,
                br.Value!.GetType().GetProperty("code")!.GetValue(br.Value));

            // Nothing was written: no Session, no Exercise, no ExerciseSet, no operation row.
            Assert.Empty(ctx.Sessions);
            Assert.Empty(ctx.Exercises);
            Assert.Empty(ctx.ExerciseSets);
            Assert.Empty(ctx.SessionCreateOperations);
        }

        [Fact]
        public async Task EmptyGuidKey_AtServiceLevel_ReturnsInvalidOperationKey_BeforeAnyPersistence()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var svc = new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance);

            var outcome = await svc.CreateFromProgramWorkoutAsync(
                1, Dto(p, w, Guid.Empty), CancellationToken.None);

            Assert.Equal(SessionCreateResult.InvalidOperationKey, outcome.Result);
            Assert.Equal(SessionCancelErrorCodes.InvalidOperationKey, outcome.ErrorCode);
            Assert.Null(outcome.Session);
            Assert.Empty(ctx.Sessions);
            Assert.Empty(ctx.Exercises);
            Assert.Empty(ctx.SessionCreateOperations);
        }

        [Fact]
        public async Task NullKey_IsNotTreatedAsEmpty_TakesUnkeyedPath_AndCreates201()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);

            var result = await Controller(ctx).CreateSessionFromProgramWorkout(
                new CreateSessionFromProgramWorkoutDto { ProgramId = p, ProgramWorkoutId = w, ClientOperationId = null },
                CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var session = Assert.IsType<Session>(created.Value);
            Assert.Null(session.ClientOperationId);
            Assert.Equal(3, session.Exercises.Count);
            Assert.Empty(ctx.SessionCreateOperations); // unkeyed: no operation row
        }

        // ===== materially different input, same key -> replay original =================

        [Fact]
        public async Task MateriallyDifferentInput_SameKey_ReplaysOriginal_IgnoresNewBody()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p1, w1) = await SeedProgramWorkout(ctx, 1);
            var (p2, w2) = await SeedProgramWorkout(ctx, 1, """[ {"name":"Only one"} ]""");
            var key = Guid.NewGuid();

            var first = SessionOf(await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p1, w1, key), CancellationToken.None));
            var replay = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p2, w2, key), CancellationToken.None);
            var replaySession = Assert.IsType<Session>(Assert.IsType<OkObjectResult>(replay.Result).Value);

            Assert.Equal(first.Id, replaySession.Id);
            Assert.Equal(w1, replaySession.ProgramWorkoutId); // first writer wins
            Assert.Equal(1, ctx.Sessions.Count(s => s.UserId == 1));
            Assert.Equal(3, ctx.Exercises.Count(e => e.SessionId == first.Id));
        }

        // ===== cross-endpoint key reuse (shared registry) =============================

        [Fact]
        public async Task CrossEndpointKeyReuse_GenericThenProgramWorkout_ReplaysGenericSession_NoExercises()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var key = Guid.NewGuid();

            var generic = await Controller(ctx).CreateSession(
                new SessionCreateRequestDto { Name = "generic", Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);
            var genericSession = Assert.IsType<SessionResponseDto>(
                Assert.IsType<CreatedAtActionResult>(generic.Result).Value);

            var pw = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);
            var pwSession = Assert.IsType<Session>(Assert.IsType<OkObjectResult>(pw.Result).Value);

            Assert.Equal(genericSession.Id, pwSession.Id);
            Assert.Empty(ctx.Exercises.Where(e => e.SessionId == pwSession.Id)); // never materialized
            Assert.Equal(1, ctx.Sessions.Count(s => s.UserId == 1));
            Assert.Single(ctx.SessionCreateOperations.Where(o => o.UserId == 1));
        }

        [Fact]
        public async Task CrossEndpointKeyReuse_ProgramWorkoutThenGeneric_ReplaysProgramWorkoutSession()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var key = Guid.NewGuid();

            var pw = SessionOf(await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None));

            var generic = await Controller(ctx).CreateSession(
                new SessionCreateRequestDto { Name = "generic", Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);
            var genericSession = Assert.IsType<SessionResponseDto>(
                Assert.IsType<OkObjectResult>(generic.Result).Value);

            Assert.Equal(pw.Id, genericSession.Id);
            Assert.Equal(3, ctx.Exercises.Count(e => e.SessionId == pw.Id));
            Assert.Equal(1, ctx.Sessions.Count(s => s.UserId == 1));
        }

        // ===== different users, same key ==============================================

        [Fact]
        public async Task DifferentUsers_SameKey_CreateIndependentSessions_NoCrossUserReplay()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            await SeedUser(ctx, 2);
            var (p1, w1) = await SeedProgramWorkout(ctx, 1);
            var (p2, w2) = await SeedProgramWorkout(ctx, 2);
            var shared = Guid.NewGuid();

            var a = SessionOf(await Controller(ctx, 1).CreateSessionFromProgramWorkout(Dto(p1, w1, shared), CancellationToken.None));
            var b = SessionOf(await Controller(ctx, 2).CreateSessionFromProgramWorkout(Dto(p2, w2, shared), CancellationToken.None));

            Assert.NotEqual(a.Id, b.Id);
            Assert.Equal(1, a.UserId);
            Assert.Equal(2, b.UserId);

            var bReplay = await Controller(ctx, 2).CreateSessionFromProgramWorkout(Dto(p2, w2, shared), CancellationToken.None);
            Assert.Equal(b.Id, Assert.IsType<Session>(Assert.IsType<OkObjectResult>(bReplay.Result).Value).Id);
        }

        // ===== ownership / non-disclosure =============================================

        [Fact]
        public async Task ForeignProgramWorkout_Keyed_Returns404_program_not_found_NothingWritten()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            await SeedUser(ctx, 2);
            var (p2, w2) = await SeedProgramWorkout(ctx, 2); // owned by user 2

            var result = await Controller(ctx, 1).CreateSessionFromProgramWorkout(
                Dto(p2, w2, Guid.NewGuid()), CancellationToken.None);

            var nf = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(SessionCreateErrorCodes.ProgramNotFound,
                nf.Value!.GetType().GetProperty("code")!.GetValue(nf.Value));
            Assert.Empty(ctx.Sessions.Where(s => s.UserId == 1));
            Assert.Empty(ctx.SessionCreateOperations.Where(o => o.UserId == 1));
        }

        [Fact]
        public async Task MissingProgramWorkout_Keyed_Returns404_program_not_found()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);

            var result = await Controller(ctx, 1).CreateSessionFromProgramWorkout(
                Dto(999999, 999999, Guid.NewGuid()), CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Empty(ctx.SessionCreateOperations);
        }

        [Fact]
        public async Task ForeignRequestProgramId_Keyed_Returns404_EvenWhenWorkoutIsOwned()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            await SeedUser(ctx, 2);
            var (_, w1) = await SeedProgramWorkout(ctx, 1);        // workout owned by user 1
            var (p2, _) = await SeedProgramWorkout(ctx, 2);        // program owned by user 2

            var result = await Controller(ctx, 1).CreateSessionFromProgramWorkout(
                Dto(p2, w1, Guid.NewGuid()), CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Empty(ctx.Sessions.Where(s => s.UserId == 1));
        }

        [Fact]
        public async Task OwnedRequestProgramId_ButForeignWorkout_Keyed_Returns404_NeverMaterializesForeignPrivateExercises()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            await SeedUser(ctx, 2);
            var (p1, _) = await SeedProgramWorkout(ctx, 1); // program owned by the caller
            var (_, victimWorkout) = await SeedProgramWorkout(
                ctx, 2, exercisesJson: """[ {"name":"VICTIM-SECRET-A"}, {"name":"VICTIM-SECRET-B"} ]""");

            // Caller pairs their OWN programId with the victim's workout id.
            var result = await Controller(ctx, 1).CreateSessionFromProgramWorkout(
                Dto(p1, victimWorkout, Guid.NewGuid()), CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Empty(ctx.Sessions.Where(s => s.UserId == 1));
            Assert.Empty(ctx.SessionCreateOperations.Where(o => o.UserId == 1));
            // The victim's private template contents never leaked into any exercise row.
            Assert.Empty(ctx.Exercises.Where(e => e.Name!.StartsWith("VICTIM-SECRET")));
        }

        [Fact]
        public async Task CompletedOperation_PointingAtAnotherUsersSession_Returns410_NotThatSession()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            await SeedUser(ctx, 2);
            var (p2, w2) = await SeedProgramWorkout(ctx, 2);
            var bSession = SessionOf(await Controller(ctx, 2).CreateSessionFromProgramWorkout(
                Dto(p2, w2, Guid.NewGuid()), CancellationToken.None));

            var key = Guid.NewGuid();
            ctx.SessionCreateOperations.Add(new SessionCreateOperation
            {
                UserId = 1,
                ClientOperationId = key,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = DateTime.UtcNow.AddMinutes(-4),
                SessionId = bSession.Id, // corrupted cross-user pointer
            });
            await ctx.SaveChangesAsync();
            var (p1, w1) = await SeedProgramWorkout(ctx, 1);

            var result = await Controller(ctx, 1).CreateSessionFromProgramWorkout(Dto(p1, w1, key), CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status410Gone, obj.StatusCode);
        }

        // ===== canceled operation ====================================================

        [Fact]
        public async Task CanceledOperation_Keyed_Returns409_operation_canceled_CreatesNothing()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var key = Guid.NewGuid();
            ctx.SessionCreateOperations.Add(new SessionCreateOperation
            {
                UserId = 1,
                ClientOperationId = key,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                CanceledAt = DateTime.UtcNow.AddMinutes(-1),
            });
            await ctx.SaveChangesAsync();

            var result = await Controller(ctx, 1).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(SessionCreateErrorCodes.OperationCanceled,
                conflict.Value!.GetType().GetProperty("code")!.GetValue(conflict.Value));
            Assert.Empty(ctx.Sessions.Where(s => s.UserId == 1));
        }

        // ===== cancellation interop: cancel via the shared by-operation endpoint =======

        [Fact]
        public async Task CancelByOperation_ThenKeyedProgramWorkoutRetry_Returns409_operation_canceled()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var key = Guid.NewGuid();

            var created = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);
            var sessionId = SessionOf(created).Id;

            var cancel = await Controller(ctx).CancelSessionCreateByOperation(key, CancellationToken.None);
            Assert.IsType<NoContentResult>(cancel);

            Assert.Empty(ctx.Sessions.Where(s => s.Id == sessionId));
            Assert.Empty(ctx.Exercises.Where(e => e.SessionId == sessionId));

            var retry = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);
            Assert.IsType<ConflictObjectResult>(retry.Result);
        }

        // ===== retry after ordinary Session deletion ==================================

        [Fact]
        public async Task KeyedRetry_AfterOrdinarySessionDeletion_Returns410_NeverRecreated()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var key = Guid.NewGuid();

            var created = SessionOf(await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None));

            var del = await Controller(ctx).DeleteSession(created.Id);
            Assert.IsType<NoContentResult>(del);

            var retry = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);
            var obj = Assert.IsType<ObjectResult>(retry.Result);
            Assert.Equal(StatusCodes.Status410Gone, obj.StatusCode);
            Assert.Empty(ctx.Sessions.Where(s => s.UserId == 1));
        }

        // ===== invalid ExercisesJson =================================================

        [Fact]
        public async Task InvalidExercisesJson_Keyed_Returns400_program_workout_data_invalid_NoTombstone_ThenRetrySucceedsAfterFix()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1, exercisesJson: "{ this is not valid json");
            var key = Guid.NewGuid();

            var bad = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);
            var br = Assert.IsType<BadRequestObjectResult>(bad.Result);
            Assert.Equal(SessionCreateErrorCodes.ProgramWorkoutDataInvalid,
                br.Value!.GetType().GetProperty("code")!.GetValue(br.Value));
            Assert.Empty(ctx.Sessions.Where(s => s.UserId == 1));
            Assert.Empty(ctx.SessionCreateOperations); // no tombstone -> retryable

            // Fix the template, retry the same key -> now succeeds as a fresh first write.
            var workout = await ctx.ProgramWorkouts.FirstAsync(x => x.Id == w);
            workout.ExercisesJson = ThreeExercisesJson;
            await ctx.SaveChangesAsync();

            var ok = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None);
            var session = Assert.IsType<Session>(Assert.IsType<CreatedAtActionResult>(ok.Result).Value);
            Assert.Equal(3, session.Exercises.Count);
        }

        [Fact]
        public async Task InvalidExercisesJson_Unkeyed_Returns400_WithLegacyBodyShape_NoSessionPersisted()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1, exercisesJson: "not json at all");

            var result = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w), CancellationToken.None);

            var br = Assert.IsType<BadRequestObjectResult>(result.Result);
            var t = br.Value!.GetType();
            Assert.Equal("Failed to parse exercises from program workout", t.GetProperty("message")!.GetValue(br.Value));
            Assert.NotNull(t.GetProperty("error")); // legacy shape has message + error
            Assert.Empty(ctx.Sessions.Where(s => s.UserId == 1));
        }

        // ===== legacy unkeyed error responses preserved ===============================

        [Fact]
        public async Task Unkeyed_MissingWorkout_Returns404_WithLegacyStringBody()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);

            var result = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(1, 999999), CancellationToken.None);

            var nf = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Program workout not found", nf.Value);
        }

        [Fact]
        public async Task Unkeyed_ForeignProgram_Returns401_WithLegacyStringBody()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            await SeedUser(ctx, 2);
            var (p2, w2) = await SeedProgramWorkout(ctx, 2);

            var result = await Controller(ctx, 1).CreateSessionFromProgramWorkout(Dto(p2, w2), CancellationToken.None);

            var un = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("You don't have access to this program", un.Value);
        }

        // ===== GET clientOperationId serialization ====================================

        [Fact]
        public async Task GetSession_AfterKeyedProgramWorkoutCreate_ExposesClientOperationId()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var key = Guid.NewGuid();

            var created = SessionOf(await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w, key), CancellationToken.None));

            var get = await Controller(ctx).GetSession(created.Id);
            var session = Assert.IsType<Session>(get.Value);
            Assert.Equal(key, session.ClientOperationId);

            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            });
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(key, doc.RootElement.GetProperty("clientOperationId").GetGuid());
        }

        // ===== unkeyed 201 response body shape (the exact contract the Flutter client parses) =

        [Fact]
        public async Task Unkeyed_201_ResponseBody_HasCamelCaseExercisesArrayAndNullClientOperationId()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);

            var result = await Controller(ctx).CreateSessionFromProgramWorkout(Dto(p, w), CancellationToken.None);
            var body = Assert.IsType<CreatedAtActionResult>(result.Result).Value;

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            });
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            Assert.Equal(JsonValueKind.Null, root.GetProperty("clientOperationId").ValueKind);
            var exercises = root.GetProperty("exercises");
            Assert.Equal(JsonValueKind.Array, exercises.ValueKind);
            Assert.Equal(3, exercises.GetArrayLength());
            Assert.Equal("Squat", exercises[0].GetProperty("name").GetString());
            Assert.True(exercises[0].TryGetProperty("exerciseSets", out var sets) && sets.ValueKind == JsonValueKind.Array);
            Assert.True(root.TryGetProperty("programWorkoutId", out var pwId) && pwId.GetInt32() == w);
        }

        // ===== keyed reload race: canonical Session vanished between commit and reload =====

        [Fact]
        public async Task Keyed_CanonicalReloadReturnsNull_MapsTo410_NotA200Or201WithNullBody()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);

            var controller = Controller(ctx, userId: 1, service: new GhostService(ctx, 1));
            var result = await controller.CreateSessionFromProgramWorkout(
                Dto(1, 1, Guid.NewGuid()), CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status410Gone, obj.StatusCode);
            Assert.Equal(SessionCreateErrorCodes.OperationTargetDeleted,
                obj.Value!.GetType().GetProperty("code")!.GetValue(obj.Value));
        }

        // ===== service-level unkeyed path (CreateFromProgramWorkoutAsync with no key) ======

        [Fact]
        public async Task Service_Unkeyed_Create_MaterializesSessionAndExercises_NoOperationRow()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1);
            var svc = new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance);

            var outcome = await svc.CreateFromProgramWorkoutAsync(
                1, new CreateSessionFromProgramWorkoutDto { ProgramId = p, ProgramWorkoutId = w }, CancellationToken.None);

            Assert.Equal(SessionCreateResult.Created, outcome.Result);
            Assert.Equal(3, ctx.Exercises.Count(e => e.SessionId == outcome.Session!.Id));
            Assert.Empty(ctx.SessionCreateOperations);
        }

        [Fact]
        public async Task Service_Unkeyed_ForeignWorkout_ReturnsProgramNotFound()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            await SeedUser(ctx, 2);
            var (p2, w2) = await SeedProgramWorkout(ctx, 2);
            var svc = new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance);

            var outcome = await svc.CreateFromProgramWorkoutAsync(
                1, new CreateSessionFromProgramWorkoutDto { ProgramId = p2, ProgramWorkoutId = w2 }, CancellationToken.None);

            Assert.Equal(SessionCreateResult.ProgramNotFound, outcome.Result);
            Assert.Empty(ctx.Sessions.Where(s => s.UserId == 1));
        }

        [Fact]
        public async Task Service_Unkeyed_InvalidExercisesJson_ReturnsProgramWorkoutDataInvalid_NoSession()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            var (p, w) = await SeedProgramWorkout(ctx, 1, exercisesJson: "}{ not json");
            var svc = new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance);

            var outcome = await svc.CreateFromProgramWorkoutAsync(
                1, new CreateSessionFromProgramWorkoutDto { ProgramId = p, ProgramWorkoutId = w }, CancellationToken.None);

            Assert.Equal(SessionCreateResult.ProgramWorkoutDataInvalid, outcome.Result);
            Assert.Empty(ctx.Sessions.Where(s => s.UserId == 1));
        }
    }
}
