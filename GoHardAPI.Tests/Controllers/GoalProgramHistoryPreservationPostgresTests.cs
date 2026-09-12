using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Real-PostgreSQL evidence that removing a Goal or Program from active use no
    /// longer destroys completed workout history, and that Archive is a genuinely
    /// distinct, non-destructive lifecycle action from Delete/Complete.
    ///
    /// Deleting a Goal used to cascade-delete its linked Program, which cascade-deleted
    /// every Session (and Exercises/Sets) ever created from it. These tests assert the
    /// new application-level detachment behavior: the DELETE endpoints keep their exact
    /// URL/verb/204 response, but linked children survive with their FK columns nulled
    /// instead of being removed.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    [Collection(HistoryPreservationPostgresCollection.Name)]
    public sealed class GoalProgramHistoryPreservationPostgresTests
    {
        private readonly HistoryPreservationPostgresFixture _pg;

        public GoalProgramHistoryPreservationPostgresTests(HistoryPreservationPostgresFixture pg) => _pg = pg;

        private static T WithUser<T>(T controller, int userId) where T : ControllerBase
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"))
                }
            };
            return controller;
        }

        private static GoalsController Goals(TrainingContext ctx, int userId) =>
            WithUser(new GoalsController(ctx), userId);

        private static ProgramsController Programs(TrainingContext ctx, int userId) =>
            WithUser(new ProgramsController(ctx), userId);

        private async Task<int> SeedUser()
        {
            await using var ctx = _pg.NewContext();
            var u = new User
            {
                Name = "U",
                Username = $"u{Guid.NewGuid():N}"[..20],
                Email = $"u{Guid.NewGuid():N}@x.com",
                PasswordHash = "h",
                DateCreated = DateTime.UtcNow,
                UnitPreference = "Metric",
            };
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();
            return u.Id;
        }

        /// <summary>Seeds Goal -> Program -> a completed Session with one Exercise and one ExerciseSet.</summary>
        private async Task<(int goalId, int programId, int sessionId, int exerciseId, int setId)> SeedGoalProgramWithCompletedSession(int userId)
        {
            await using var ctx = _pg.NewContext();

            var goal = new Goal
            {
                UserId = userId,
                GoalType = "Weight Loss",
                TargetValue = 150,
                CurrentValue = 180,
                StartDate = DateTime.UtcNow.Date,
                IsActive = true,
            };
            ctx.Goals.Add(goal);
            await ctx.SaveChangesAsync();

            var program = new GoHardAPI.Models.Program
            {
                UserId = userId,
                Title = "12-Week Plan",
                GoalId = goal.Id,
                TotalWeeks = 12,
                StartDate = DateTime.UtcNow.Date,
                IsActive = true,
                Status = "active",
            };
            ctx.Programs.Add(program);
            await ctx.SaveChangesAsync();

            var session = new Session
            {
                UserId = userId,
                Date = DateTime.UtcNow.Date,
                Status = SessionStatus.Completed,
                ProgramId = program.Id,
                StartedAt = DateTime.UtcNow.AddHours(-1),
                CompletedAt = DateTime.UtcNow,
            };
            ctx.Sessions.Add(session);
            await ctx.SaveChangesAsync();

            var exercise = new Exercise
            {
                SessionId = session.Id,
                Name = "Bench Press",
            };
            ctx.Exercises.Add(exercise);
            await ctx.SaveChangesAsync();

            var set = new ExerciseSet
            {
                ExerciseId = exercise.Id,
                SetNumber = 1,
                Reps = 8,
                Weight = 100,
            };
            ctx.ExerciseSets.Add(set);
            await ctx.SaveChangesAsync();

            return (goal.Id, program.Id, session.Id, exercise.Id, set.Id);
        }

        [DockerRequiredFact]
        public async Task deleting_a_goal_detaches_its_program_and_preserves_completed_session_history()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var (goalId, programId, sessionId, exerciseId, setId) = await SeedGoalProgramWithCompletedSession(userId);

            await using (var ctx = _pg.NewContext())
            {
                var result = await Goals(ctx, userId).DeleteGoal(goalId);
                Assert.IsType<NoContentResult>(result);
            }

            await using (var verify = _pg.NewContext())
            {
                // The Goal row is never physically removed — only soft-deleted — which is
                // what closes the concurrent-creation race by construction (see
                // concurrent_program_creation_during_goal_deletion_never_loses_the_new_program).
                var deletedGoal = await verify.Goals.FirstOrDefaultAsync(g => g.Id == goalId);
                Assert.NotNull(deletedGoal);
                Assert.True(deletedGoal!.IsDeleted);
                Assert.NotNull(deletedGoal.DeletedAt);
                Assert.False(deletedGoal.IsActive);

                // ...but it is unconditionally excluded from every read endpoint, so it is
                // gone from the user's perspective exactly as a hard delete would appear.
                Assert.False(await verify.Goals.AnyAsync(g => g.Id == goalId && !g.IsDeleted));
                var getResult = await Goals(verify, userId).GetGoal(goalId);
                Assert.IsType<NotFoundResult>(getResult.Result);

                var listResult = await Goals(verify, userId).GetGoals(isActive: null);
                var listedOk = Assert.IsType<OkObjectResult>(listResult.Result);
                var listedGoals = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Goal>>(listedOk.Value);
                Assert.DoesNotContain(listedGoals, g => g.Id == goalId);

                var program = await verify.Programs.FirstOrDefaultAsync(p => p.Id == programId);
                Assert.NotNull(program);
                Assert.Null(program!.GoalId);
                Assert.True(program.IsActive); // untouched by goal deletion

                var session = await verify.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
                Assert.NotNull(session);
                Assert.Equal(SessionStatus.Completed, session!.Status);
                Assert.Equal(programId, session.ProgramId); // program row still exists, still linked

                Assert.True(await verify.Exercises.AnyAsync(e => e.Id == exerciseId));
                Assert.True(await verify.ExerciseSets.AnyAsync(s => s.Id == setId));
            }
        }

        [DockerRequiredFact]
        public async Task deleting_a_program_detaches_its_sessions_and_preserves_completed_history()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var (_, programId, sessionId, exerciseId, setId) = await SeedGoalProgramWithCompletedSession(userId);

            await using (var ctx = _pg.NewContext())
            {
                var result = await Programs(ctx, userId).DeleteProgram(programId);
                Assert.IsType<NoContentResult>(result);
            }

            await using (var verify = _pg.NewContext())
            {
                // The Program row is never physically removed — only soft-deleted — which
                // is what closes the concurrent-creation race by construction (see
                // concurrent_session_creation_during_program_deletion_never_loses_the_new_session).
                var deletedProgram = await verify.Programs.FirstOrDefaultAsync(p => p.Id == programId);
                Assert.NotNull(deletedProgram);
                Assert.Equal("deleted", deletedProgram!.Status);
                Assert.False(deletedProgram.IsActive);

                // ...but it is unconditionally excluded from every read endpoint, so it is
                // gone from the user's perspective exactly as a hard delete would appear.
                Assert.False(await verify.Programs.AnyAsync(p => p.Id == programId && p.Status != "deleted"));
                var getResult = await Programs(verify, userId).GetProgram(programId);
                Assert.IsType<NotFoundResult>(getResult.Result);

                var listResult = await Programs(verify, userId).GetPrograms(isActive: null, status: null, includeDrafts: false);
                var listedOk = Assert.IsType<OkObjectResult>(listResult.Result);
                var listedPrograms = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<GoHardAPI.Models.Program>>(listedOk.Value);
                Assert.DoesNotContain(listedPrograms, p => p.Id == programId);

                var session = await verify.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
                Assert.NotNull(session);
                Assert.Equal(SessionStatus.Completed, session!.Status);
                Assert.Null(session.ProgramId);
                Assert.Null(session.ProgramWorkoutId);

                Assert.True(await verify.Exercises.AnyAsync(e => e.Id == exerciseId));
                Assert.True(await verify.ExerciseSets.AnyAsync(s => s.Id == setId));
            }
        }

        [DockerRequiredFact]
        public async Task deleting_a_program_preserves_an_in_progress_session_and_its_logged_data()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();

            int programId, sessionId;
            await using (var ctx = _pg.NewContext())
            {
                var program = new GoHardAPI.Models.Program
                {
                    UserId = userId,
                    Title = "Plan",
                    TotalWeeks = 4,
                    StartDate = DateTime.UtcNow.Date,
                    IsActive = true,
                    Status = "active",
                };
                ctx.Programs.Add(program);
                await ctx.SaveChangesAsync();

                var session = new Session
                {
                    UserId = userId,
                    Date = DateTime.UtcNow.Date,
                    Status = SessionStatus.InProgress,
                    ProgramId = program.Id,
                    StartedAt = DateTime.UtcNow.AddMinutes(-10),
                };
                ctx.Sessions.Add(session);
                await ctx.SaveChangesAsync();

                programId = program.Id;
                sessionId = session.Id;
            }

            await using (var ctx = _pg.NewContext())
            {
                await Programs(ctx, userId).DeleteProgram(programId);
            }

            await using (var verify = _pg.NewContext())
            {
                var session = await verify.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
                Assert.NotNull(session);
                Assert.Equal(SessionStatus.InProgress, session!.Status);
                Assert.NotNull(session.StartedAt);
                Assert.Null(session.ProgramId);
            }
        }

        [DockerRequiredFact]
        public async Task archiving_a_goal_never_completes_it_and_never_touches_its_program()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var (goalId, programId, _, _, _) = await SeedGoalProgramWithCompletedSession(userId);

            await using (var ctx = _pg.NewContext())
            {
                var result = await Goals(ctx, userId).ArchiveGoal(goalId);
                Assert.IsType<NoContentResult>(result);
            }

            await using (var verify = _pg.NewContext())
            {
                var goal = await verify.Goals.FirstAsync(g => g.Id == goalId);
                Assert.True(goal.IsArchived);
                Assert.NotNull(goal.ArchivedAt);
                Assert.False(goal.IsActive);
                Assert.False(goal.IsCompleted); // archiving is not completing
                Assert.Null(goal.CompletedAt);

                // Still discoverable via the plain GET /goals list.
                var listed = await Goals(verify, userId).GetGoals(isActive: null);
                var listedOk = Assert.IsType<OkObjectResult>(listed.Result);
                var goals = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Goal>>(listedOk.Value);
                Assert.Contains(goals, g => g.Id == goalId);

                var program = await verify.Programs.FirstAsync(p => p.Id == programId);
                Assert.True(program.IsActive); // archiving the goal must not cascade to the program
                Assert.Equal(goalId, program.GoalId);
            }
        }

        [DockerRequiredFact]
        public async Task archiving_a_program_never_completes_it_and_never_touches_workouts_or_sessions()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();

            int programId, workoutId;
            await using (var ctx = _pg.NewContext())
            {
                var program = new GoHardAPI.Models.Program
                {
                    UserId = userId,
                    Title = "Plan",
                    TotalWeeks = 4,
                    StartDate = DateTime.UtcNow.Date,
                    IsActive = true,
                    Status = "active",
                };
                ctx.Programs.Add(program);
                await ctx.SaveChangesAsync();

                var workout = new ProgramWorkout
                {
                    ProgramId = program.Id,
                    WeekNumber = 1,
                    DayNumber = 1,
                    WorkoutName = "Push Day",
                    ExercisesJson = "[]",
                    IsCompleted = false,
                };
                ctx.ProgramWorkouts.Add(workout);
                await ctx.SaveChangesAsync();

                programId = program.Id;
                workoutId = workout.Id;
            }

            int sessionCountBefore;
            await using (var countCtx = _pg.NewContext())
            {
                sessionCountBefore = await countCtx.Sessions.CountAsync(s => s.UserId == userId);
            }

            await using (var ctx = _pg.NewContext())
            {
                var result = await Programs(ctx, userId).ArchiveProgram(programId);
                Assert.IsType<NoContentResult>(result);
            }

            await using (var verify = _pg.NewContext())
            {
                var program = await verify.Programs.FirstAsync(p => p.Id == programId);
                Assert.Equal("archived", program.Status);
                Assert.False(program.IsActive);
                Assert.False(program.IsCompleted); // archiving is not completing
                Assert.Null(program.CompletedAt);

                var workout = await verify.ProgramWorkouts.FirstAsync(w => w.Id == workoutId);
                Assert.False(workout.IsCompleted); // no fabricated completion

                var sessionCountAfter = await verify.Sessions.CountAsync(s => s.UserId == userId);
                Assert.Equal(sessionCountBefore, sessionCountAfter); // no Session created

                // Still discoverable via the plain GET /Programs list (default filter only excludes drafts).
                var listed = await Programs(verify, userId).GetPrograms(isActive: null, status: null, includeDrafts: false);
                var listedOk = Assert.IsType<OkObjectResult>(listed.Result);
                var programs = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<GoHardAPI.Models.Program>>(listedOk.Value);
                Assert.Contains(programs, p => p.Id == programId);
            }
        }

        [DockerRequiredFact]
        public async Task get_sessions_does_not_500_when_its_program_has_been_deleted()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();
            var (_, programId, sessionId, _, _) = await SeedGoalProgramWithCompletedSession(userId);

            await using (var ctx = _pg.NewContext())
            {
                await Programs(ctx, userId).DeleteProgram(programId);
            }

            await using (var ctx = _pg.NewContext())
            {
                var controller = new SessionsController(ctx, new GoHardAPI.Services.SessionCreateService(ctx, NullLogger<GoHardAPI.Services.SessionCreateService>.Instance));
                WithUser(controller, userId);
                var listResult = await controller.GetSessions();
                var sessions = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Session>>(listResult.Value);
                Assert.Contains(sessions, s => s.Id == sessionId);

                var detailResult = await controller.GetSession(sessionId);
                Assert.NotNull(detailResult.Value);
                Assert.Null(detailResult.Value!.ProgramId);
            }
        }

        [DockerRequiredFact]
        public async Task deleting_and_archiving_are_scoped_to_the_owning_user()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var owner = await SeedUser();
            var stranger = await SeedUser();
            var (goalId, programId, _, _, _) = await SeedGoalProgramWithCompletedSession(owner);

            await using (var ctx = _pg.NewContext())
            {
                Assert.IsType<NotFoundResult>(await Goals(ctx, stranger).DeleteGoal(goalId));
                Assert.IsType<NotFoundResult>(await Goals(ctx, stranger).ArchiveGoal(goalId));
                Assert.IsType<NotFoundResult>(await Programs(ctx, stranger).DeleteProgram(programId));
                Assert.IsType<NotFoundResult>(await Programs(ctx, stranger).ArchiveProgram(programId));
            }

            await using (var verify = _pg.NewContext())
            {
                Assert.True(await verify.Goals.AnyAsync(g => g.Id == goalId));
                Assert.True(await verify.Programs.AnyAsync(p => p.Id == programId));
            }
        }

        /// <summary>
        /// Closes the concurrent-creation-vs-delete race for Goal deletion: a Program
        /// created and linked to a Goal concurrently with that Goal being deleted must
        /// never be destroyed, regardless of which request's write actually lands first
        /// at the database. This is real concurrency against the real PostgreSQL
        /// container — two separate <see cref="TrainingContext"/> instances (separate
        /// connections/transactions) racing via <c>Task.WhenAll</c>, not a hypothetical
        /// or mocked interleaving.
        ///
        /// Under the OLD (hard-delete) design this was a genuine data-loss window: if
        /// the Program insert committed after the Goal row's DELETE, the DB-level
        /// ON DELETE CASCADE from Programs.GoalId would remove it. Under the current
        /// soft-delete design there is no physical DELETE statement to race against —
        /// the FK the new Program's insert depends on (Goals.Id) is never removed, so
        /// the insert always succeeds and the row always survives.
        /// </summary>
        [DockerRequiredFact]
        public async Task concurrent_program_creation_during_goal_deletion_never_loses_the_new_program()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();

            int goalId;
            await using (var ctx = _pg.NewContext())
            {
                var goal = new Goal
                {
                    UserId = userId,
                    GoalType = "Weight Loss",
                    TargetValue = 150,
                    CurrentValue = 180,
                    StartDate = DateTime.UtcNow.Date,
                    IsActive = true,
                };
                ctx.Goals.Add(goal);
                await ctx.SaveChangesAsync();
                goalId = goal.Id;
            }

            await using var deleteCtx = _pg.NewContext();
            await using var createCtx = _pg.NewContext();

            // Started together, not awaited individually, so both requests' I/O is
            // genuinely in flight against the real container at the same time.
            var deleteTask = Goals(deleteCtx, userId).DeleteGoal(goalId);
            var createTask = Programs(createCtx, userId).CreateProgram(new GoHardAPI.Models.Program
            {
                UserId = userId,
                Title = "Concurrently Created Program",
                GoalId = goalId,
                TotalWeeks = 8,
                StartDate = DateTime.UtcNow.Date,
            });

            await Task.WhenAll(deleteTask, createTask);

            Assert.IsType<NoContentResult>(await deleteTask);
            var createdOk = Assert.IsType<CreatedAtActionResult>((await createTask).Result);
            var createdProgram = Assert.IsType<GoHardAPI.Models.Program>(createdOk.Value);

            await using var verify = _pg.NewContext();

            var goalRow = await verify.Goals.FirstAsync(g => g.Id == goalId);
            Assert.True(goalRow.IsDeleted);

            // The acceptance bar: the concurrently-created Program was never destroyed,
            // no matter which write landed first at the database.
            var programRow = await verify.Programs.FirstOrDefaultAsync(p => p.Id == createdProgram.Id);
            Assert.NotNull(programRow);
            Assert.Equal("Concurrently Created Program", programRow!.Title);
        }

        /// <summary>
        /// Closes the concurrent-creation-vs-delete race for Program deletion: a Session
        /// created and linked to a Program (through the real <c>POST /sessions</c>
        /// endpoint, via <see cref="Services.SessionCreateService"/>) concurrently with
        /// that Program being deleted must never be destroyed, regardless of which
        /// request's write actually lands first at the database. Same real-concurrency
        /// shape as the Goal/Program test above: two separate
        /// <see cref="TrainingContext"/> instances raced via <c>Task.WhenAll</c> against
        /// the real PostgreSQL container.
        ///
        /// Under the OLD (hard-delete) design this was the exact window the prior
        /// report disclosed as an accepted limitation: a Session insert committing after
        /// the Program row's DELETE would be cascade-removed via
        /// ON DELETE CASCADE from Sessions.ProgramId. Under the current soft-delete
        /// design there is no physical DELETE to race against, so the Session's FK
        /// dependency is never pulled out from under it.
        /// </summary>
        [DockerRequiredFact]
        public async Task concurrent_session_creation_during_program_deletion_never_loses_the_new_session()
        {
            Assert.True(_pg.Available, "PostgreSQL container must be available in CI");
            var userId = await SeedUser();

            int programId;
            await using (var ctx = _pg.NewContext())
            {
                var program = new GoHardAPI.Models.Program
                {
                    UserId = userId,
                    Title = "Plan",
                    TotalWeeks = 4,
                    StartDate = DateTime.UtcNow.Date,
                    IsActive = true,
                    Status = "active",
                };
                ctx.Programs.Add(program);
                await ctx.SaveChangesAsync();
                programId = program.Id;
            }

            await using var deleteCtx = _pg.NewContext();
            await using var createCtx = _pg.NewContext();

            var deleteController = Programs(deleteCtx, userId);
            var sessionsController = new SessionsController(
                createCtx,
                new GoHardAPI.Services.SessionCreateService(
                    createCtx, NullLogger<GoHardAPI.Services.SessionCreateService>.Instance));
            WithUser(sessionsController, userId);

            var deleteTask = deleteController.DeleteProgram(programId);
            var createTask = sessionsController.CreateSession(
                new GoHardAPI.DTOs.SessionCreateRequestDto
                {
                    Status = SessionStatus.InProgress,
                    Date = DateTime.UtcNow.Date,
                    StartedAt = DateTime.UtcNow,
                    ProgramId = programId,
                },
                CancellationToken.None);

            await Task.WhenAll(deleteTask, createTask);

            Assert.IsType<NoContentResult>(await deleteTask);
            var createResult = await createTask;
            var createdAt = Assert.IsType<CreatedAtActionResult>(createResult.Result);
            var createdSession = Assert.IsType<GoHardAPI.DTOs.SessionResponseDto>(createdAt.Value);

            await using var verify = _pg.NewContext();

            var programRow = await verify.Programs.FirstAsync(p => p.Id == programId);
            Assert.Equal("deleted", programRow.Status);

            // The acceptance bar: the concurrently-created Session was never destroyed,
            // no matter which write landed first at the database.
            var sessionRow = await verify.Sessions.FirstOrDefaultAsync(s => s.Id == createdSession.Id);
            Assert.NotNull(sessionRow);
        }
    }
}
