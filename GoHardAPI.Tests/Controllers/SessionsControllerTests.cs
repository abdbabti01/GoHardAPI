using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    public class SessionsControllerTests
    {
        private TrainingContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<TrainingContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new TrainingContext(options);
        }

        private SessionsController CreateControllerWithUser(TrainingContext context, int userId)
        {
            var controller = new SessionsController(context);

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

        private async Task<User> CreateTestUser(TrainingContext context, int userId = 1)
        {
            var user = new User
            {
                Id = userId,
                Name = "Test User",
                Email = $"test{userId}@example.com",
                PasswordHash = "hash",
                PasswordSalt = ""
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task GetSessions_ReturnsEmptyList_WhenNoSessions()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);

            // Act
            var result = await controller.GetSessions();

            // Assert
            var sessions = Assert.IsType<List<Session>>(result.Value);
            Assert.Empty(sessions);
        }

        [Fact]
        public async Task GetSessions_ReturnsUserSessionsOnly()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);

            context.Sessions.AddRange(
                new Session { UserId = 1, Name = "User 1 Session", Date = DateTime.UtcNow },
                new Session { UserId = 2, Name = "User 2 Session", Date = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);

            // Act
            var result = await controller.GetSessions();

            // Assert
            var sessions = Assert.IsType<List<Session>>(result.Value);
            Assert.Single(sessions);
            Assert.Equal("User 1 Session", sessions[0].Name);
        }

        [Fact]
        public async Task GetSession_ReturnsSession_WhenExists()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Test Session", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);

            // Act
            var result = await controller.GetSession(session.Id);

            // Assert
            var returnedSession = Assert.IsType<Session>(result.Value);
            Assert.Equal("Test Session", returnedSession.Name);
        }

        [Fact]
        public async Task GetSession_ReturnsNotFound_WhenSessionDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);

            // Act
            var result = await controller.GetSession(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetSession_ReturnsNotFound_WhenSessionBelongsToOtherUser()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);
            var session = new Session { UserId = 2, Name = "Other User Session", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);

            // Act
            var result = await controller.GetSession(session.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateSession_ReturnsCreatedSession()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);
            var newSession = new Session { Name = "New Session", Date = DateTime.UtcNow };

            // Act
            var result = await controller.CreateSession(newSession);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var session = Assert.IsType<Session>(createdResult.Value);
            Assert.Equal("New Session", session.Name);
            Assert.Equal(1, session.UserId);
        }

        [Fact]
        public async Task CreateSession_AssignsCurrentUserId()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);
            var newSession = new Session
            {
                Name = "New Session",
                Date = DateTime.UtcNow,
                UserId = 999 // Try to set different user
            };

            // Act
            var result = await controller.CreateSession(newSession);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var session = Assert.IsType<Session>(createdResult.Value);
            Assert.Equal(1, session.UserId); // Should be current user, not 999
        }

        [Fact]
        public async Task UpdateSession_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Original", Date = DateTime.UtcNow, Version = 1 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var updatedSession = new Session
            {
                Id = session.Id,
                UserId = 1,
                Name = "Updated",
                Date = DateTime.UtcNow,
                Version = 1
            };

            // Act
            var result = await controller.UpdateSession(session.Id, updatedSession);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var dbSession = await context.Sessions.FindAsync(session.Id);
            Assert.Equal("Updated", dbSession!.Name);
        }

        [Fact]
        public async Task UpdateSession_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);
            var session = new Session { Id = 1, UserId = 1, Name = "Test", Date = DateTime.UtcNow };

            // Act
            var result = await controller.UpdateSession(999, session);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateSession_ReturnsNotFound_WhenSessionDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);
            var session = new Session { Id = 999, UserId = 1, Name = "Test", Date = DateTime.UtcNow };

            // Act
            var result = await controller.UpdateSession(999, session);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateSession_ReturnsConflict_WhenVersionMismatch()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Original", Date = DateTime.UtcNow, Version = 2 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var updatedSession = new Session
            {
                Id = session.Id,
                UserId = 1,
                Name = "Updated",
                Date = DateTime.UtcNow,
                Version = 1 // Outdated version
            };

            // Act
            var result = await controller.UpdateSession(session.Id, updatedSession);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task DeleteSession_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "To Delete", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);

            // Act
            var result = await controller.DeleteSession(session.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Sessions.FindAsync(session.Id));
        }

        [Fact]
        public async Task DeleteSession_ReturnsNotFound_WhenSessionDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);

            // Act
            var result = await controller.DeleteSession(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteSession_ReturnsNotFound_WhenSessionBelongsToOtherUser()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);
            var session = new Session { UserId = 2, Name = "Other User Session", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);

            // Act
            var result = await controller.DeleteSession(session.Id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateSessionStatus_ReturnsNoContent_WhenValid()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Test", Status = "draft", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new UpdateStatusRequest { Status = "in_progress" };

            // Act
            var result = await controller.UpdateSessionStatus(session.Id, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var updatedSession = await context.Sessions.FindAsync(session.Id);
            Assert.Equal("in_progress", updatedSession!.Status);
        }

        [Fact]
        public async Task UpdateSessionStatus_ReturnsBadRequest_WhenStatusEmpty()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Test", Status = "draft", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new UpdateStatusRequest { Status = "" };

            // Act
            var result = await controller.UpdateSessionStatus(session.Id, request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSessionStatus_ReturnsBadRequest_WhenStatusInvalid()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Test", Status = "draft", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new UpdateStatusRequest { Status = "invalid_status" };

            // Act
            var result = await controller.UpdateSessionStatus(session.Id, request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSessionStatus_SetsStartedAt_WhenMovingToInProgress()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Test", Status = "draft", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new UpdateStatusRequest { Status = "in_progress" };

            // Act
            await controller.UpdateSessionStatus(session.Id, request);

            // Assert
            var updatedSession = await context.Sessions.FindAsync(session.Id);
            Assert.NotNull(updatedSession!.StartedAt);
        }

        [Fact]
        public async Task UpdateSessionStatus_SetsCompletedAt_WhenMovingToCompleted()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session
            {
                UserId = 1,
                Name = "Test",
                Status = "in_progress",
                Date = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow.AddMinutes(-30)
            };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new UpdateStatusRequest { Status = "completed" };

            // Act
            await controller.UpdateSessionStatus(session.Id, request);

            // Assert
            var updatedSession = await context.Sessions.FindAsync(session.Id);
            Assert.NotNull(updatedSession!.CompletedAt);
        }

        [Fact]
        public async Task GetSessions_IncludesExercisesAndSets()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Test", Date = DateTime.UtcNow };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var exercise = new Exercise { SessionId = session.Id, Name = "Bench Press" };
            context.Exercises.Add(exercise);
            await context.SaveChangesAsync();

            var set = new ExerciseSet { ExerciseId = exercise.Id, Weight = 100, Reps = 10 };
            context.ExerciseSets.Add(set);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);

            // Act
            var result = await controller.GetSessions();

            // Assert
            var sessions = Assert.IsType<List<Session>>(result.Value);
            Assert.Single(sessions);
            Assert.Single(sessions[0].Exercises);
            Assert.Equal("Bench Press", sessions[0].Exercises.First().Name);
        }
    }
}
