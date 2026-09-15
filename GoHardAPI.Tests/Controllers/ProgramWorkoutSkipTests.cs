using System;
using System.Linq;
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
    /// Skip must not mean complete: covers <see cref="ProgramsController.SkipWorkout"/> and
    /// <see cref="ProgramsController.UnskipWorkout"/> - no fake completion credit, idempotent
    /// retries, in-progress/completed sessions are protected rather than clobbered, and skip is
    /// scoped to exactly one occurrence.
    /// </summary>
    public class ProgramWorkoutSkipTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly TrainingContext _context;

        public ProgramWorkoutSkipTests()
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

        private async Task<(int programId, int workoutId)> SeedProgramWithWorkout(int userId, bool secondWorkout = false)
        {
            _context.Users.Add(new User { Id = userId, Name = $"u{userId}", Username = $"u{userId}", Email = $"u{userId}@x.com", PasswordHash = "h" });
            var program = new GoHardAPI.Models.Program
            {
                UserId = userId,
                Title = "P",
                StartDate = new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc),
                Status = ProgramStatus.Active.ToApiString(),
            };
            _context.Programs.Add(program);
            await _context.SaveChangesAsync();

            var workout = new ProgramWorkout
            {
                ProgramId = program.Id,
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = "[]",
            };
            _context.ProgramWorkouts.Add(workout);

            if (secondWorkout)
            {
                _context.ProgramWorkouts.Add(new ProgramWorkout
                {
                    ProgramId = program.Id,
                    WeekNumber = 1,
                    DayNumber = 2,
                    WorkoutName = "Day 2",
                    ExercisesJson = "[]",
                });
            }

            await _context.SaveChangesAsync();
            return (program.Id, workout.Id);
        }

        private async Task<int> SeedSession(int userId, int workoutId, string status)
        {
            var session = new Session
            {
                UserId = userId,
                ProgramWorkoutId = workoutId,
                Date = DateTime.UtcNow,
                Status = status,
            };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return session.Id;
        }

        [Fact]
        public async Task SkipWorkout_MarksSkipped_NeverSetsCompletion()
        {
            var (_, workoutId) = await SeedProgramWithWorkout(1);

            var result = await Controller(_context, 1).SkipWorkout(workoutId);

            Assert.IsType<NoContentResult>(result);
            var stored = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.True(stored.IsSkipped);
            Assert.NotNull(stored.SkippedAt);
            Assert.False(stored.IsCompleted);
            Assert.Null(stored.CompletedAt);
        }

        [Fact]
        public async Task CompleteWorkout_AlreadySkipped_IsRejected_AndNotOverwritten()
        {
            var (_, workoutId) = await SeedProgramWithWorkout(1);
            await Controller(_context, 1).SkipWorkout(workoutId);

            var result = await Controller(_context, 1).CompleteWorkout(workoutId, null);

            Assert.IsType<ConflictObjectResult>(result);
            var stored = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.True(stored.IsSkipped);
            Assert.False(stored.IsCompleted);
            Assert.Null(stored.CompletedAt);
        }

        [Fact]
        public async Task SkipWorkout_RepeatedCalls_Converge()
        {
            var (_, workoutId) = await SeedProgramWithWorkout(1);

            await Controller(_context, 1).SkipWorkout(workoutId);
            var firstSkippedAt = (await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId)).SkippedAt;

            var second = await Controller(_context, 1).SkipWorkout(workoutId);

            Assert.IsType<NoContentResult>(second);
            var stored = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.True(stored.IsSkipped);
            Assert.Equal(firstSkippedAt, stored.SkippedAt); // unchanged by the second, idempotent call
        }

        [Fact]
        public async Task SkipWorkout_AlreadyCompleted_IsRejected_AndNotOverwritten()
        {
            var (_, workoutId) = await SeedProgramWithWorkout(1);
            var workout = await _context.ProgramWorkouts.FindAsync(workoutId);
            workout!.IsCompleted = true;
            workout.CompletedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await _context.SaveChangesAsync();

            var result = await Controller(_context, 1).SkipWorkout(workoutId);

            Assert.IsType<ConflictObjectResult>(result);
            var stored = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.True(stored.IsCompleted);
            Assert.False(stored.IsSkipped);
        }

        [Fact]
        public async Task SkipWorkout_InProgressSession_IsRejected_SessionUntouched()
        {
            var (_, workoutId) = await SeedProgramWithWorkout(1);
            var sessionId = await SeedSession(1, workoutId, SessionStatus.InProgress);

            var result = await Controller(_context, 1).SkipWorkout(workoutId);

            Assert.IsType<ConflictObjectResult>(result);
            var workout = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.False(workout.IsSkipped);
            var session = await _context.Sessions.AsNoTracking().FirstAsync(s => s.Id == sessionId);
            Assert.Equal(SessionStatus.InProgress, session.Status);
        }

        [Fact]
        public async Task SkipWorkout_CompletedSession_IsRejected_SessionUntouched()
        {
            var (_, workoutId) = await SeedProgramWithWorkout(1);
            var sessionId = await SeedSession(1, workoutId, SessionStatus.Completed);

            var result = await Controller(_context, 1).SkipWorkout(workoutId);

            Assert.IsType<ConflictObjectResult>(result);
            var session = await _context.Sessions.AsNoTracking().FirstAsync(s => s.Id == sessionId);
            Assert.Equal(SessionStatus.Completed, session.Status);
        }

        [Fact]
        public async Task SkipWorkout_PlannedSession_AlsoTransitionsToSkipped_NoDuplicateSession()
        {
            var (_, workoutId) = await SeedProgramWithWorkout(1);
            var sessionId = await SeedSession(1, workoutId, SessionStatus.Planned);

            var result = await Controller(_context, 1).SkipWorkout(workoutId);

            Assert.IsType<NoContentResult>(result);
            var session = await _context.Sessions.AsNoTracking().SingleAsync(s => s.ProgramWorkoutId == workoutId);
            Assert.Equal(sessionId, session.Id);
            Assert.Equal(SessionStatus.Skipped, session.Status);
        }

        [Fact]
        public async Task SkipWorkout_DoesNotAffectOtherWorkoutsInSameProgram()
        {
            var (programId, workoutId) = await SeedProgramWithWorkout(1, secondWorkout: true);
            var otherWorkoutId = await _context.ProgramWorkouts
                .Where(w => w.ProgramId == programId && w.Id != workoutId)
                .Select(w => w.Id)
                .SingleAsync();

            await Controller(_context, 1).SkipWorkout(workoutId);

            var other = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == otherWorkoutId);
            Assert.False(other.IsSkipped);
        }

        [Fact]
        public async Task UnskipWorkout_RestoresScheduledState_NoSessionDuplication()
        {
            var (_, workoutId) = await SeedProgramWithWorkout(1);
            var sessionId = await SeedSession(1, workoutId, SessionStatus.Planned);
            await Controller(_context, 1).SkipWorkout(workoutId);

            var result = await Controller(_context, 1).UnskipWorkout(workoutId);

            Assert.IsType<NoContentResult>(result);
            var workout = await _context.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.False(workout.IsSkipped);
            Assert.Null(workout.SkippedAt);
            var session = await _context.Sessions.AsNoTracking().SingleAsync(s => s.ProgramWorkoutId == workoutId);
            Assert.Equal(sessionId, session.Id);
            Assert.Equal(SessionStatus.Planned, session.Status);
        }

        [Fact]
        public async Task UnskipWorkout_NotSkipped_IsNoOp()
        {
            var (_, workoutId) = await SeedProgramWithWorkout(1);

            var result = await Controller(_context, 1).UnskipWorkout(workoutId);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task SkipWorkout_OtherUsersWorkout_IsNotFound()
        {
            var (_, workoutId) = await SeedProgramWithWorkout(1);

            _context.Users.Add(new User { Id = 2, Name = "u2", Username = "u2", Email = "u2@x.com", PasswordHash = "h" });
            await _context.SaveChangesAsync();

            var result = await Controller(_context, 2).SkipWorkout(workoutId);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
