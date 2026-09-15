using System;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Real-PostgreSQL, deterministically-interleaved proof that Start (session creation
    /// from a ProgramWorkout), Complete, Skip and Unskip can never race into an
    /// inconsistent committed result for the SAME occurrence - closing the gap where a
    /// plain "check no in-progress session, then write" (with no shared lock) leaves a
    /// window for a session to be created between the check and the write.
    ///
    /// <para>Every scenario here holds one writer's transaction open (after it has
    /// acquired <see cref="ProgramWorkoutOccurrenceLock"/> and made its in-memory
    /// decision, but before <c>SaveChanges</c>/commit) while a second writer's real
    /// call - <see cref="ProgramsController.SkipWorkout"/>,
    /// <see cref="ProgramsController.CompleteWorkout"/>,
    /// <see cref="ProgramsController.UnskipWorkout"/>, or
    /// <see cref="SessionCreateService.CreateFromProgramWorkoutAsync"/> - is fired
    /// concurrently on a background <see cref="Task"/> against the SAME workout id. That
    /// second call blocks on <c>pg_advisory_xact_lock</c> until the first commits, so the
    /// ordering is deterministic (never a wall-clock race) while still exercising a REAL
    /// lock wait across two independent connections/transactions - not a simulated
    /// sequence.</para>
    /// </summary>
    [Collection(SessionCancellationPostgresCollection.Name)]
    [Trait("Category", "PostgresIntegration")]
    public sealed class ProgramWorkoutOccurrenceConcurrencyPostgresTests
    {
        private readonly SessionCancellationPostgresFixture _pg;

        public ProgramWorkoutOccurrenceConcurrencyPostgresTests(SessionCancellationPostgresFixture pg)
        {
            _pg = pg;
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

        private async Task<(int programId, int workoutId)> SeedProgramWithWorkoutAsync(int userId)
        {
            await using var ctx = _pg.NewContext();
            var program = new GoHardAPI.Models.Program
            {
                UserId = userId,
                Title = "P",
                StartDate = new DateTime(2020, 1, 6, 0, 0, 0, DateTimeKind.Utc),
                Status = ProgramStatus.Active.ToApiString(),
                CreatedAt = DateTime.UtcNow,
            };
            ctx.Programs.Add(program);
            await ctx.SaveChangesAsync();

            var workout = new ProgramWorkout
            {
                ProgramId = program.Id,
                WeekNumber = 1,
                DayNumber = 1,
                WorkoutName = "Day 1",
                ExercisesJson = "[]",
            };
            ctx.ProgramWorkouts.Add(workout);
            await ctx.SaveChangesAsync();
            return (program.Id, workout.Id);
        }

        /// <summary>
        /// Manually drives exactly the same steps as
        /// <see cref="ProgramsController.SkipWorkout"/> - open transaction, acquire the
        /// occurrence lock, mark skipped - but stops short of commit so the test can hold
        /// it open while a concurrent writer races it.
        /// </summary>
        private async Task<(TrainingContext ctx, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx)>
            BeginHeldSkipAsync(int workoutId)
        {
            var ctx = _pg.NewContext();
            var tx = await ctx.Database.BeginTransactionAsync();
            await ProgramWorkoutOccurrenceLock.AcquireAsync(ctx, workoutId, default);
            var workout = await ctx.ProgramWorkouts.FirstAsync(w => w.Id == workoutId);
            workout.IsSkipped = true;
            workout.SkippedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
            return (ctx, tx);
        }

        [DockerRequiredFact]
        public async Task StartRacesSkip_SkipCommitsFirst_SessionCreateSeesSkippedAndRefuses()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");
            var userId = await _pg.SeedUserAsync();
            var (programId, workoutId) = await SeedProgramWithWorkoutAsync(userId);

            // Writer A: Skip, held open after acquiring the lock and deciding to skip.
            var (heldCtx, heldTx) = await BeginHeldSkipAsync(workoutId);

            // Writer B: a real "start" (session creation from this ProgramWorkout),
            // fired concurrently. It must block on the same advisory lock until A commits.
            var startTask = Task.Run(async () =>
            {
                await using var ctx = _pg.NewContext();
                var service = new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance);
                return await service.CreateFromProgramWorkoutAsync(
                    userId,
                    new CreateSessionFromProgramWorkoutDto { ProgramWorkoutId = workoutId, ProgramId = programId },
                    default);
            });

            // Give B time to reach and block on the lock before A commits.
            await Task.Delay(300);
            await heldTx.CommitAsync();
            await heldCtx.DisposeAsync();

            var outcome = await startTask;

            Assert.Equal(SessionCreateResult.ProgramWorkoutSkipped, outcome.Result);
            Assert.Null(outcome.Session);

            await using var verifyCtx = _pg.NewContext();
            Assert.True((await verifyCtx.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId)).IsSkipped);
            Assert.False(await verifyCtx.Sessions.AnyAsync(s => s.ProgramWorkoutId == workoutId));
        }

        [DockerRequiredFact]
        public async Task StartRacesSkip_StartCommitsFirst_SkipThenSeesAndCascadesToTheNewSession()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");
            var userId = await _pg.SeedUserAsync();
            var (_, workoutId) = await SeedProgramWithWorkoutAsync(userId);

            // Writer A: a "start", manually driven through the SAME steps
            // ProgramWorkoutFirstWriteAsync takes (under the lock, re-check IsSkipped,
            // materialize) - held open before commit so the test controls the
            // interleaving. Calling the real CreateFromProgramWorkoutAsync here isn't
            // possible: it manages its own transaction end-to-end internally (that's the
            // fix), so it cannot be "held open" from outside.
            var startCtx = _pg.NewContext();
            var startTx = await startCtx.Database.BeginTransactionAsync();
            await ProgramWorkoutOccurrenceLock.AcquireAsync(startCtx, workoutId, default);
            var lockedWorkout = await startCtx.ProgramWorkouts.FirstAsync(w => w.Id == workoutId);
            Assert.False(lockedWorkout.IsSkipped);
            var newSession = new Session
            {
                UserId = userId,
                ProgramWorkoutId = workoutId,
                Date = DateTime.UtcNow,
                Status = SessionStatus.Planned,
            };
            startCtx.Sessions.Add(newSession);
            await startCtx.SaveChangesAsync();

            // Writer B: Skip, fired concurrently - must block until A's create commits.
            var skipTask = Task.Run(async () =>
            {
                await using var ctx = _pg.NewContext();
                return await Controller(ctx, userId).SkipWorkout(workoutId);
            });

            await Task.Delay(300);
            await startTx.CommitAsync();
            await startCtx.DisposeAsync();

            var skipResult = await skipTask;

            // B, unblocked, re-reads under the lock and sees A's just-committed Planned
            // session (not the stale "no session" snapshot from before A committed) -
            // Skip is still allowed against a Planned session, and cascades onto it too.
            Assert.IsType<NoContentResult>(skipResult);

            await using var verifyCtx = _pg.NewContext();
            var workout = await verifyCtx.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.True(workout.IsSkipped);
            // Exactly ONE session exists - Skip never created a second one, and Start's
            // session was not deleted, only cascaded from Planned to Skipped.
            var session = await verifyCtx.Sessions.AsNoTracking().SingleAsync(s => s.ProgramWorkoutId == workoutId);
            Assert.Equal(SessionStatus.Skipped, session.Status);
        }

        [DockerRequiredFact]
        public async Task CompleteRacesSkip_SkipCommitsFirst_CompleteIsRefused()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");
            var userId = await _pg.SeedUserAsync();
            var (_, workoutId) = await SeedProgramWithWorkoutAsync(userId);

            var (heldCtx, heldTx) = await BeginHeldSkipAsync(workoutId);

            var completeTask = Task.Run(async () =>
            {
                await using var ctx = _pg.NewContext();
                return await Controller(ctx, userId).CompleteWorkout(workoutId, null);
            });

            await Task.Delay(300);
            await heldTx.CommitAsync();
            await heldCtx.DisposeAsync();

            var completeResult = await completeTask;

            Assert.IsType<ConflictObjectResult>(completeResult);

            await using var verifyCtx = _pg.NewContext();
            var workout = await verifyCtx.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.True(workout.IsSkipped);
            Assert.False(workout.IsCompleted);
            Assert.Null(workout.CompletedAt);
        }

        [DockerRequiredFact]
        public async Task RepeatedSkip_TwoConcurrentSkipRequests_ConvergeToOneConsistentOutcome()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");
            var userId = await _pg.SeedUserAsync();
            var (_, workoutId) = await SeedProgramWithWorkoutAsync(userId);

            var (heldCtx, heldTx) = await BeginHeldSkipAsync(workoutId);
            var firstSkippedAt = (await heldCtx.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId)).SkippedAt;

            // A second, fully independent Skip request for the SAME occurrence, fired
            // concurrently - must block until the first commits.
            var secondSkipTask = Task.Run(async () =>
            {
                await using var ctx = _pg.NewContext();
                return await Controller(ctx, userId).SkipWorkout(workoutId);
            });

            await Task.Delay(300);
            await heldTx.CommitAsync();
            await heldCtx.DisposeAsync();

            var secondResult = await secondSkipTask;

            // Idempotent convergence: the second call sees IsSkipped already true (under
            // the lock, not a stale snapshot) and returns success without re-stamping
            // SkippedAt or touching anything else.
            Assert.IsType<NoContentResult>(secondResult);

            await using var verifyCtx = _pg.NewContext();
            var workout = await verifyCtx.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.True(workout.IsSkipped);
            Assert.Equal(firstSkippedAt, workout.SkippedAt);
        }

        [DockerRequiredFact]
        public async Task Undo_UnskipCommitsFirst_ThenANewSkipDeterministicallyWins()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");
            var userId = await _pg.SeedUserAsync();
            var (_, workoutId) = await SeedProgramWithWorkoutAsync(userId);

            // Start from an already-skipped occurrence.
            await using (var setupCtx = _pg.NewContext())
            {
                var workout = await setupCtx.ProgramWorkouts.FirstAsync(w => w.Id == workoutId);
                workout.IsSkipped = true;
                workout.SkippedAt = DateTime.UtcNow.AddHours(-1);
                await setupCtx.SaveChangesAsync();
            }

            // Writer A: Unskip, held open after acquiring the lock and clearing the flag.
            var unskipCtx = _pg.NewContext();
            var unskipTx = await unskipCtx.Database.BeginTransactionAsync();
            await ProgramWorkoutOccurrenceLock.AcquireAsync(unskipCtx, workoutId, default);
            var heldWorkout = await unskipCtx.ProgramWorkouts.FirstAsync(w => w.Id == workoutId);
            heldWorkout.IsSkipped = false;
            heldWorkout.SkippedAt = null;
            await unskipCtx.SaveChangesAsync();

            // Writer B: a NEWER skip decision for the same occurrence, racing the undo -
            // must block until A's unskip commits.
            var newSkipTask = Task.Run(async () =>
            {
                await using var ctx = _pg.NewContext();
                return await Controller(ctx, userId).SkipWorkout(workoutId);
            });

            await Task.Delay(300);
            await unskipTx.CommitAsync();
            await unskipCtx.DisposeAsync();

            var newSkipResult = await newSkipTask;
            Assert.IsType<NoContentResult>(newSkipResult);

            // One consistent committed outcome: since the undo committed BEFORE the new
            // skip, the new skip deterministically wins - the occurrence ends up skipped
            // again, never left in some averaged/ambiguous state.
            await using var verifyCtx = _pg.NewContext();
            var final = await verifyCtx.ProgramWorkouts.AsNoTracking().FirstAsync(w => w.Id == workoutId);
            Assert.True(final.IsSkipped);
            Assert.NotNull(final.SkippedAt);
        }
    }
}
