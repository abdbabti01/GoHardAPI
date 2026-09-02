using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Contract tests for <see cref="ExerciseSetsController.UpdateExerciseSet"/>.
    /// Direct controller instantiation (the pattern used across this project) is
    /// enough here: the endpoint's behavior is route/body identity, the full
    /// set -> exercise -> session -> user ownership chain, no cross-user
    /// reparenting, and a 204 No Content success contract. Middleware 401s are
    /// covered by the class-level [Authorize] shared with every other controller.
    /// </summary>
    public class ExerciseSetsControllerTests
    {
        private TrainingContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<TrainingContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new TrainingContext(options);
        }

        private ExerciseSetsController CreateControllerWithUser(TrainingContext context, int userId)
        {
            var controller = new ExerciseSetsController(context);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            return controller;
        }

        private async Task<User> CreateTestUser(TrainingContext context, int userId)
        {
            var user = new User
            {
                Id = userId,
                Name = "Test User",
                Email = $"test{userId}@example.com",
                PasswordHash = "hash"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Creates a session -> exercise -> set chain for <paramref name="userId"/>
        /// and returns the persisted set (with a real, assigned Id and ExerciseId).
        /// </summary>
        private async Task<ExerciseSet> SeedSet(
            TrainingContext context,
            int userId,
            int setNumber = 1,
            int reps = 10,
            double weight = 100,
            bool isCompleted = false)
        {
            var session = new Session { UserId = userId, Name = "S", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var exercise = new Exercise { SessionId = session.Id, Name = "Bench Press" };
            context.Exercises.Add(exercise);
            await context.SaveChangesAsync();

            var set = new ExerciseSet
            {
                ExerciseId = exercise.Id,
                SetNumber = setNumber,
                Reps = reps,
                Weight = weight,
                IsCompleted = isCompleted,
                Version = 1
            };
            context.ExerciseSets.Add(set);
            await context.SaveChangesAsync();
            return set;
        }

        private async Task<Exercise> SeedExercise(TrainingContext context, int userId)
        {
            var session = new Session { UserId = userId, Name = "S2", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var exercise = new Exercise { SessionId = session.Id, Name = "Squat" };
            context.Exercises.Add(exercise);
            await context.SaveChangesAsync();
            return exercise;
        }

        private static ExerciseSetUpdateRequestDto CanonicalBody(ExerciseSet set) => new()
        {
            Id = set.Id,
            ExerciseId = set.ExerciseId,
            SetNumber = set.SetNumber,
            Reps = set.Reps,
            Weight = set.Weight,
            Duration = set.Duration,
            IsCompleted = set.IsCompleted,
            CompletedAt = set.CompletedAt,
            Notes = set.Notes
        };

        // ===== Success contract =====

        [Fact]
        public async Task UpdateExerciseSet_ReturnsNoContent_WhenOwnerSendsCanonicalBody()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            var set = await SeedSet(context, userId: 1, reps: 10, weight: 100);

            var controller = CreateControllerWithUser(context, 1);
            var body = CanonicalBody(set);
            body.Reps = 12;
            body.Weight = 105;
            body.Notes = "top set";

            var result = await controller.UpdateExerciseSet(set.Id, body);

            Assert.IsType<NoContentResult>(result);
            var stored = await context.ExerciseSets.FindAsync(set.Id);
            Assert.Equal(12, stored!.Reps);
            Assert.Equal(105, stored.Weight);
            Assert.Equal("top set", stored.Notes);
        }

        [Fact]
        public async Task UpdateExerciseSet_PersistsCompletionFields_ForOfflineCompleteSync()
        {
            // Mirrors SyncService._syncUpdateSet after an offline "complete set":
            // a full body with isCompleted/completedAt must round-trip to 204.
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            var set = await SeedSet(context, userId: 1, isCompleted: false);

            var controller = CreateControllerWithUser(context, 1);
            var completedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var body = CanonicalBody(set);
            body.IsCompleted = true;
            body.CompletedAt = completedAt;

            var result = await controller.UpdateExerciseSet(set.Id, body);

            Assert.IsType<NoContentResult>(result);
            var stored = await context.ExerciseSets.FindAsync(set.Id);
            Assert.True(stored!.IsCompleted);
            Assert.Equal(completedAt, stored.CompletedAt);
        }

        [Fact]
        public async Task UpdateExerciseSet_LeavesParentAndVersionUntouched_OnSuccess()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            var set = await SeedSet(context, userId: 1);
            var originalExerciseId = set.ExerciseId;

            var controller = CreateControllerWithUser(context, 1);
            var body = CanonicalBody(set);
            body.Reps = 99;

            await controller.UpdateExerciseSet(set.Id, body);

            var stored = await context.ExerciseSets.FindAsync(set.Id);
            Assert.Equal(originalExerciseId, stored!.ExerciseId);
            Assert.Equal(1, stored.Version); // not client-assignable, not bumped here
        }

        // ===== Route / body identity =====

        [Fact]
        public async Task UpdateExerciseSet_ReturnsBadRequest_WhenRouteIdAndBodyIdDiffer()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            var set = await SeedSet(context, userId: 1, reps: 10);

            var controller = CreateControllerWithUser(context, 1);
            var body = CanonicalBody(set);
            body.Id = set.Id + 1; // mismatch
            body.Reps = 50;

            var result = await controller.UpdateExerciseSet(set.Id, body);

            Assert.IsType<BadRequestObjectResult>(result);
            var stored = await context.ExerciseSets.FindAsync(set.Id);
            Assert.Equal(10, stored!.Reps); // nothing written
        }

        // ===== Ownership chain =====

        [Fact]
        public async Task UpdateExerciseSet_ReturnsNotFound_WhenSetBelongsToAnotherUser()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);
            var foreignSet = await SeedSet(context, userId: 2, reps: 7);

            var controller = CreateControllerWithUser(context, 1);
            var body = CanonicalBody(foreignSet);
            body.Reps = 999;

            var result = await controller.UpdateExerciseSet(foreignSet.Id, body);

            Assert.IsType<NotFoundResult>(result);
            var stored = await context.ExerciseSets.FindAsync(foreignSet.Id);
            Assert.Equal(7, stored!.Reps); // foreign data untouched
        }

        [Fact]
        public async Task UpdateExerciseSet_ReturnsNotFound_WhenSetDoesNotExist()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            var controller = CreateControllerWithUser(context, 1);

            var body = new ExerciseSetUpdateRequestDto { Id = 999, ExerciseId = 1, SetNumber = 1 };
            var result = await controller.UpdateExerciseSet(999, body);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateExerciseSet_FailsClosed_WhenParentExerciseIsMissing()
        {
            // An orphaned set (parent exercise row gone) has no resolvable owner
            // chain - it must fail closed as NotFound, never fall through to a write.
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            var set = new ExerciseSet { ExerciseId = 424242, SetNumber = 1, Reps = 5, Version = 1 };
            context.ExerciseSets.Add(set);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var body = new ExerciseSetUpdateRequestDto
            {
                Id = set.Id,
                ExerciseId = 424242,
                SetNumber = 1,
                Reps = 50
            };

            var result = await controller.UpdateExerciseSet(set.Id, body);

            Assert.IsType<NotFoundResult>(result);
            var stored = await context.ExerciseSets.FindAsync(set.Id);
            Assert.Equal(5, stored!.Reps);
        }

        // ===== No reparenting =====

        [Fact]
        public async Task UpdateExerciseSet_ReturnsBadRequest_WhenBodyNamesADifferentOwnedExercise()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            var set = await SeedSet(context, userId: 1);
            var otherOwnExercise = await SeedExercise(context, userId: 1);

            var controller = CreateControllerWithUser(context, 1);
            var body = CanonicalBody(set);
            body.ExerciseId = otherOwnExercise.Id; // reparent within own data

            var result = await controller.UpdateExerciseSet(set.Id, body);

            Assert.IsType<BadRequestObjectResult>(result);
            var stored = await context.ExerciseSets.FindAsync(set.Id);
            Assert.NotEqual(otherOwnExercise.Id, stored!.ExerciseId);
        }

        [Fact]
        public async Task UpdateExerciseSet_CannotReparentSetOntoAnotherUsersExercise()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);
            var set = await SeedSet(context, userId: 1);
            var foreignExercise = await SeedExercise(context, userId: 2);
            var originalParent = set.ExerciseId;

            var controller = CreateControllerWithUser(context, 1);
            var body = CanonicalBody(set);
            body.ExerciseId = foreignExercise.Id;
            body.Reps = 123;

            var result = await controller.UpdateExerciseSet(set.Id, body);

            Assert.IsType<BadRequestObjectResult>(result);
            var stored = await context.ExerciseSets.FindAsync(set.Id);
            Assert.Equal(originalParent, stored!.ExerciseId); // never moved
            Assert.NotEqual(123, stored.Reps);               // nothing written
        }

        // ===== Contract shape =====

        [Fact]
        public void ExerciseSetUpdateRequestDto_DoesNotExposeVersion()
        {
            // Version is optimistic-concurrency metadata resolved server-side; it
            // must not be a client-assignable field on the update contract.
            var properties = typeof(ExerciseSetUpdateRequestDto).GetProperties().Select(p => p.Name);
            Assert.DoesNotContain("Version", properties);
        }
    }
}
