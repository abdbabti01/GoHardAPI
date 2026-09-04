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
            var controller = new SessionsController(
                context,
                new SessionCreateService(context, NullLogger<SessionCreateService>.Instance));

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
                PasswordHash = "hash"
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
        public async Task CreateSession_ReturnsCreatedCanonicalDto_ForLegacyUnkeyedRequest()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionCreateRequestDto { Name = "New Session", Date = DateTime.UtcNow };

            // Act
            var result = await controller.CreateSession(request, CancellationToken.None);

            // Assert: legacy (no clientOperationId) still returns 201, now with the canonical DTO
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<SessionResponseDto>(createdResult.Value);
            Assert.Equal("New Session", dto.Name);
            Assert.Equal(1, dto.UserId);
            Assert.IsNotType<Session>(createdResult.Value);
            Assert.Single(context.Sessions);
            // Unkeyed create writes no operation record.
            Assert.Empty(context.SessionCreateOperations);
        }

        [Fact]
        public async Task CreateSession_AssignsCurrentUserId_FromJwtOnly()
        {
            // Arrange: SessionCreateRequestDto has no UserId field, so ownership can only
            // come from the JWT.
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionCreateRequestDto { Name = "New Session", Date = DateTime.UtcNow };

            // Act
            var result = await controller.CreateSession(request, CancellationToken.None);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<SessionResponseDto>(createdResult.Value);
            Assert.Equal(1, dto.UserId);
        }

        [Fact]
        public void SessionCreateRequestDto_DoesNotExposeServerControlledFields()
        {
            // No server Id, no UserId, no User navigation, no Version, no Exercises / child graph.
            var properties = typeof(SessionCreateRequestDto).GetProperties().Select(p => p.Name).ToList();

            Assert.DoesNotContain("Id", properties);
            Assert.DoesNotContain("UserId", properties);
            Assert.DoesNotContain("User", properties);
            Assert.DoesNotContain("Version", properties);
            Assert.DoesNotContain("Exercises", properties);
        }

        [Fact]
        public void SessionCreateRequestDto_ToNewSession_NeverSetsIdUserIdVersionOrChildren()
        {
            var request = new SessionCreateRequestDto { Name = "X", Status = "draft", Date = DateTime.UtcNow };

            var session = request.ToNewSession(userId: 7);

            Assert.Equal(0, session.Id);
            Assert.Equal(7, session.UserId);
            Assert.Equal(1, session.Version);
            Assert.Empty(session.Exercises);
        }

        [Fact]
        public void SessionCreateRequestDto_IgnoresOverpostedIdUserIdVersionAndExercises_OnBind()
        {
            // System.Text.Json drops unknown members; the DTO has no place to put these,
            // so a client that batches a session + child exercises in one POST body gets
            // the session created and the children silently ignored (they must go through
            // POST /sessions/{id}/exercises).
            const string body = """
            {
              "name": "Leg Day", "status": "draft", "date": "2026-09-03",
              "id": 999, "userId": 4242, "version": 77,
              "user": { "id": 4242 },
              "exercises": [ { "name": "Squat", "sets": [ { "reps": 5 } ] } ]
            }
            """;

            var dto = System.Text.Json.JsonSerializer.Deserialize<SessionCreateRequestDto>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            Assert.Equal("Leg Day", dto.Name);
            var session = dto.ToNewSession(userId: 4);
            Assert.Equal(0, session.Id);
            Assert.Equal(4, session.UserId);
            Assert.Equal(1, session.Version);
            Assert.Empty(session.Exercises);
        }

        [Fact]
        public void SessionResponseDto_PostBody_HasExactlyTheCanonicalScalarKeys_NoChildGraph()
        {
            // Locks the 201 body shape: POST now returns SessionResponseDto, not the raw
            // Session entity, so it no longer carries "exercises"/"user"/"program"/
            // "programWorkout"/"clientOperationId". Any future drift breaks this test.
            var session = new Session
            {
                Id = 5,
                UserId = 1,
                Date = DateTime.UtcNow,
                Name = "n",
                Status = "draft",
                Version = 1,
                ClientOperationId = Guid.NewGuid(),
                Exercises = { new Exercise { Name = "x" } },
            };

            var json = System.Text.Json.JsonSerializer.Serialize(
                SessionResponseDto.FromEntity(session),
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                });

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var keys = doc.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(k => k).ToArray();

            Assert.Equal(
                new[]
                {
                    "completedAt", "date", "duration", "id", "name", "notes", "pausedAt",
                    "programId", "programWorkoutId", "startedAt", "status", "type", "userId", "version",
                },
                keys);
        }

        // ===== Keyed CREATE state machine (InMemory logic-level coverage) ======================
        // These prove the branch logic. They are NOT concurrency evidence — that lives in the
        // real-PostgreSQL SessionCreateIdempotencyPostgresTests.

        [Fact]
        public async Task CreateSession_KeyedReplay_Returns200_WithOriginalCanonicalSession_AndNoMutation()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);
            var key = Guid.NewGuid();

            var first = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "Original", Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);
            var created = Assert.IsType<CreatedAtActionResult>(first.Result);
            var createdDto = Assert.IsType<SessionResponseDto>(created.Value);

            // Conflicting replay body — must be ignored (first writer wins).
            var replay = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "DIFFERENT", Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(replay.Result);
            var replayDto = Assert.IsType<SessionResponseDto>(ok.Value);
            Assert.Equal(createdDto.Id, replayDto.Id);
            Assert.Equal("Original", replayDto.Name);
            Assert.Single(context.Sessions);
            Assert.Equal("Original", (await context.Sessions.FindAsync(createdDto.Id))!.Name);
        }

        [Fact]
        public async Task CreateSession_DifferentKeys_CreateDifferentSessions()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);

            var a = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "A", Date = DateTime.UtcNow, ClientOperationId = Guid.NewGuid() },
                CancellationToken.None);
            var b = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "B", Date = DateTime.UtcNow, ClientOperationId = Guid.NewGuid() },
                CancellationToken.None);

            var aDto = Assert.IsType<SessionResponseDto>(Assert.IsType<CreatedAtActionResult>(a.Result).Value);
            var bDto = Assert.IsType<SessionResponseDto>(Assert.IsType<CreatedAtActionResult>(b.Result).Value);
            Assert.NotEqual(aDto.Id, bDto.Id);
            Assert.Equal(2, context.Sessions.Count());
        }

        [Fact]
        public async Task CreateSession_SameKeyDifferentUsers_CreateIndependentSessions_AndBCannotSeeA()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);
            var key = Guid.NewGuid();

            var aController = CreateControllerWithUser(context, 1);
            var bController = CreateControllerWithUser(context, 2);

            var a = await aController.CreateSession(
                new SessionCreateRequestDto { Name = "A-owned", Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);
            var b = await bController.CreateSession(
                new SessionCreateRequestDto { Name = "B-owned", Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);

            var aDto = Assert.IsType<SessionResponseDto>(Assert.IsType<CreatedAtActionResult>(a.Result).Value);
            var bDto = Assert.IsType<SessionResponseDto>(Assert.IsType<CreatedAtActionResult>(b.Result).Value);

            Assert.NotEqual(aDto.Id, bDto.Id);
            Assert.Equal(1, aDto.UserId);
            Assert.Equal(2, bDto.UserId);
            Assert.Equal("A-owned", aDto.Name);
            Assert.Equal("B-owned", bDto.Name);
        }

        [Fact]
        public async Task CreateSession_CanceledOperation_Returns409_operation_canceled_AndCreatesNothing()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var key = Guid.NewGuid();
            context.SessionCreateOperations.Add(new SessionCreateOperation
            {
                UserId = 1,
                ClientOperationId = key,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                CanceledAt = DateTime.UtcNow.AddMinutes(-1),
            });
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var result = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "X", Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(SessionCreateErrorCodes.OperationCanceled,
                conflict.Value!.GetType().GetProperty("code")!.GetValue(conflict.Value));
            Assert.Empty(context.Sessions);
        }

        [Fact]
        public async Task CreateSession_CompletedOperationWhoseSessionWasDeleted_Returns410_AndNeverRecreates()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var key = Guid.NewGuid();
            context.SessionCreateOperations.Add(new SessionCreateOperation
            {
                UserId = 1,
                ClientOperationId = key,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = DateTime.UtcNow.AddMinutes(-4),
                SessionId = null, // ON DELETE SET NULL fired when the Session was deleted
            });
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var result = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "X", Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);

            var gone = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status410Gone, gone.StatusCode);
            Assert.Equal(SessionCreateErrorCodes.OperationTargetDeleted,
                gone.Value!.GetType().GetProperty("code")!.GetValue(gone.Value));
            Assert.Empty(context.Sessions);
        }

        [Fact]
        public async Task CreateSession_IndeterminateOperation_FailsClosed_409_AndCreatesNoSecondSession()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var key = Guid.NewGuid();
            context.SessionCreateOperations.Add(new SessionCreateOperation
            {
                UserId = 1,
                ClientOperationId = key,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = null,
                CanceledAt = null,
                SessionId = null,
            });
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var result = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "X", Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal(SessionCreateErrorCodes.OperationIncomplete,
                conflict.Value!.GetType().GetProperty("code")!.GetValue(conflict.Value));
            Assert.Empty(context.Sessions);
        }

        [Fact]
        public async Task CreateSession_KeyedFirstCreate_WritesExactlyOneSessionAndOneOperationRow()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);
            var key = Guid.NewGuid();

            var result = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "Once", Date = DateTime.UtcNow, ClientOperationId = key },
                CancellationToken.None);

            var dto = Assert.IsType<SessionResponseDto>(Assert.IsType<CreatedAtActionResult>(result.Result).Value);
            Assert.Single(context.Sessions);
            var op = Assert.Single(context.SessionCreateOperations);
            Assert.Equal(1, op.UserId);
            Assert.Equal(key, op.ClientOperationId);
            Assert.Equal(dto.Id, op.SessionId);
            Assert.NotNull(op.CompletedAt);
            Assert.Null(op.CanceledAt);
        }

        // ===== UpdateSession (PUT) - SessionUpdateRequestDto / SessionResponseDto contract =====

        [Fact]
        public async Task UpdateSession_ReturnsOkWithIncrementedVersion_WhenExplicitVersionMatches()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Original", Date = DateTime.UtcNow, Version = 1 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Updated",
                Date = DateTime.UtcNow,
                Version = 1
            };

            // Act
            var result = await controller.UpdateSession(session.Id, request);

            // Assert: 200 OK with the incremented (N+1) authoritative version
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<SessionResponseDto>(okResult.Value);
            Assert.Equal(2, dto.Version);
            Assert.Equal("Updated", dto.Name);

            var dbSession = await context.Sessions.FindAsync(session.Id);
            Assert.Equal("Updated", dbSession!.Name);
            Assert.Equal(2, dbSession.Version);
        }

        [Fact]
        public async Task UpdateSession_ReturnsConflict_WhenExplicitVersionIsStale()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Original", Date = DateTime.UtcNow, Version = 2 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Updated",
                Date = DateTime.UtcNow,
                Version = 1 // Outdated version
            };

            // Act
            var result = await controller.UpdateSession(session.Id, request);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);

            var dbSession = await context.Sessions.FindAsync(session.Id);
            Assert.Equal("Original", dbSession!.Name); // stale write must not apply
            Assert.Equal(2, dbSession.Version); // version must not change on conflict
        }

        [Fact]
        public async Task UpdateSession_ConflictServerDataIsSessionResponseDto_WithCurrentVersion()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Original", Date = DateTime.UtcNow, Version = 2 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Updated",
                Date = DateTime.UtcNow,
                Version = 1
            };

            // Act
            var result = await controller.UpdateSession(session.Id, request);

            // Assert: conflict body carries a typed SessionResponseDto, not the EF entity
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            var body = conflictResult.Value!;
            var bodyType = body.GetType();

            var messageProp = bodyType.GetProperty("message");
            var currentVersionProp = bodyType.GetProperty("currentVersion");
            var serverDataProp = bodyType.GetProperty("serverData");

            Assert.NotNull(messageProp);
            Assert.NotNull(currentVersionProp);
            Assert.NotNull(serverDataProp);

            Assert.Equal(2, currentVersionProp!.GetValue(body));

            var serverData = serverDataProp!.GetValue(body);
            var serverDataDto = Assert.IsType<SessionResponseDto>(serverData);
            Assert.Equal(2, serverDataDto.Version);
            Assert.Equal("Original", serverDataDto.Name);
        }

        [Fact]
        public async Task UpdateSession_ReturnsOkWithVersion2_WhenVersionMissingAndStoredVersionIsOne()
        {
            // Arrange: a session at its default version (1) - as if created before version
            // tracking existed on the client, or simply never updated yet.
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Original", Date = DateTime.UtcNow, Version = 1 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Updated",
                Date = DateTime.UtcNow,
                Version = null // legacy client - never sends version
            };

            // Act
            var result = await controller.UpdateSession(session.Id, request);

            // Assert: resolves to 1, matches stored version 1, succeeds and increments to 2
            var okResult = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<SessionResponseDto>(okResult.Value);
            Assert.Equal(2, dto.Version);

            var dbSession = await context.Sessions.FindAsync(session.Id);
            Assert.Equal(2, dbSession!.Version);
        }

        [Fact]
        public async Task UpdateSession_ReturnsConflict_WhenVersionMissingAndStoredVersionIsTwoOrGreater()
        {
            // Arrange: session has already been updated once (version 2+), but the
            // legacy client still has no way to present a version.
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Original", Date = DateTime.UtcNow, Version = 2 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Updated",
                Date = DateTime.UtcNow,
                Version = null // legacy client - never sends version
            };

            // Act
            var result = await controller.UpdateSession(session.Id, request);

            // Assert: the legacy-null fallback (1) does not match stored (2) - the
            // fallback must not bypass conflict detection.
            Assert.IsType<ConflictObjectResult>(result);

            var dbSession = await context.Sessions.FindAsync(session.Id);
            Assert.Equal("Original", dbSession!.Name);
            Assert.Equal(2, dbSession.Version);
        }

        [Fact]
        public void SessionUpdateRequestDto_DoesNotExposeIdOrUserId()
        {
            // The route id identifies the session and the JWT identifies its owner -
            // neither must be a client-assignable field on the update contract.
            var properties = typeof(SessionUpdateRequestDto).GetProperties().Select(p => p.Name);

            Assert.DoesNotContain("Id", properties);
            Assert.DoesNotContain("UserId", properties);
        }

        [Fact]
        public async Task UpdateSession_DoesNotChangeSessionOwnership()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            var session = new Session { UserId = 1, Name = "Original", Date = DateTime.UtcNow, Version = 1 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Updated",
                Date = DateTime.UtcNow,
                Version = 1
            };

            // Act
            var result = await controller.UpdateSession(session.Id, request);

            // Assert: ownership is untouched by the update (there is no field on the
            // request DTO that could carry a different owner in the first place)
            Assert.IsType<OkObjectResult>(result);
            var dbSession = await context.Sessions.FindAsync(session.Id);
            Assert.Equal(1, dbSession!.UserId);
        }

        [Fact]
        public async Task UpdateSession_ReturnsNotFound_WhenSessionBelongsToOtherUser()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);
            var session = new Session { UserId = 2, Name = "Other User Session", Date = DateTime.UtcNow, Version = 1 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Hijacked",
                Date = DateTime.UtcNow,
                Version = 1
            };

            // Act
            var result = await controller.UpdateSession(session.Id, request);

            // Assert: cross-user update is rejected, and the other user's data is untouched
            Assert.IsType<NotFoundResult>(result);
            var dbSession = await context.Sessions.FindAsync(session.Id);
            Assert.Equal("Other User Session", dbSession!.Name);
        }

        [Fact]
        public async Task UpdateSession_ReturnsNotFound_WhenSessionDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Test",
                Date = DateTime.UtcNow,
                Version = 1
            };

            // Act
            var result = await controller.UpdateSession(999, request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateSession_OnlyUpdatesPermittedFields()
        {
            // Arrange: program linkage and id are not part of SessionUpdateRequestDto,
            // so they must survive an update completely unchanged.
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session
            {
                UserId = 1,
                Name = "Original",
                Date = DateTime.UtcNow,
                Version = 1,
                ProgramId = 42,
                ProgramWorkoutId = 99
            };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();
            var originalId = session.Id;

            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Updated",
                Status = "in_progress",
                Date = DateTime.UtcNow,
                Duration = 45,
                Notes = "some notes",
                Version = 1
            };

            // Act
            await controller.UpdateSession(originalId, request);

            // Assert: permitted fields changed, everything not on the DTO did not
            var dbSession = await context.Sessions.FindAsync(originalId);
            Assert.Equal("Updated", dbSession!.Name);
            Assert.Equal("in_progress", dbSession.Status);
            Assert.Equal(45, dbSession.Duration);
            Assert.Equal("some notes", dbSession.Notes);
            Assert.Equal(originalId, dbSession.Id);
            Assert.Equal(1, dbSession.UserId);
            Assert.Equal(42, dbSession.ProgramId);
            Assert.Equal(99, dbSession.ProgramWorkoutId);
        }

        [Fact]
        public async Task UpdateSession_DoesNotAffectExistingExerciseRelationships()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Original", Date = DateTime.UtcNow, Version = 1 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var exercise = new Exercise { SessionId = session.Id, Name = "Bench Press" };
            context.Exercises.Add(exercise);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Updated",
                Date = DateTime.UtcNow,
                Version = 1
            };

            // Act
            await controller.UpdateSession(session.Id, request);

            // Assert: the exercise relationship is untouched by the session update
            var exercisesForSession = context.Exercises.Where(e => e.SessionId == session.Id).ToList();
            Assert.Single(exercisesForSession);
            Assert.Equal("Bench Press", exercisesForSession[0].Name);
        }

        [Fact]
        public async Task UpdateSession_ResponseContainsOnlySessionResponseDtoData()
        {
            // Arrange
            var context = GetInMemoryContext();
            await CreateTestUser(context);
            var session = new Session { UserId = 1, Name = "Original", Date = DateTime.UtcNow, Version = 1 };
            context.Sessions.Add(session);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, 1);
            var request = new SessionUpdateRequestDto
            {
                Name = "Updated",
                Date = DateTime.UtcNow,
                Version = 1
            };

            // Act
            var result = await controller.UpdateSession(session.Id, request);

            // Assert: the response is exactly the DTO shape, never the EF entity
            // (which would carry navigation properties, tracking state, etc.)
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<SessionResponseDto>(okResult.Value);
            Assert.IsNotType<Session>(okResult.Value);
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

        // ===== Program / ProgramWorkout ownership on CREATE ==================================
        // A supplied programId / programWorkoutId must resolve to a resource owned by the
        // JWT user. Missing and foreign both return the SAME non-disclosing 404
        // { code: "program_not_found" }. No FK exception may escape as a 500. A keyed replay
        // never revalidates. Each removed ownership/relationship predicate must break a test.

        private async Task<GoHardAPI.Models.Program> SeedProgramAsync(TrainingContext ctx, int id, int ownerUserId)
        {
            var program = new GoHardAPI.Models.Program { Id = id, UserId = ownerUserId, Title = "P" };
            ctx.Programs.Add(program);
            await ctx.SaveChangesAsync();
            return program;
        }

        private async Task<ProgramWorkout> SeedProgramWorkoutAsync(TrainingContext ctx, int id, int programId)
        {
            var workout = new ProgramWorkout { Id = id, ProgramId = programId, WeekNumber = 1, DayNumber = 1, WorkoutName = "W" };
            ctx.ProgramWorkouts.Add(workout);
            await ctx.SaveChangesAsync();
            return workout;
        }

        private static string? CodeOf(object? body) =>
            body?.GetType().GetProperty("code")?.GetValue(body) as string;

        [Theory]
        [InlineData(null)]                                   // legacy / unkeyed
        [InlineData("11111111-1111-1111-1111-111111111111")] // keyed
        public async Task CreateSession_WithOwnedProgramId_Succeeds(string? key)
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await SeedProgramAsync(context, id: 10, ownerUserId: 1);
            var controller = CreateControllerWithUser(context, 1);

            var result = await controller.CreateSession(
                new SessionCreateRequestDto
                {
                    Name = "S",
                    Date = DateTime.UtcNow,
                    ProgramId = 10,
                    ClientOperationId = key is null ? null : Guid.Parse(key),
                },
                CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<SessionResponseDto>(created.Value);
            Assert.Equal(10, dto.ProgramId);
            Assert.Equal(10, (await context.Sessions.SingleAsync()).ProgramId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("22222222-2222-2222-2222-222222222222")]
        public async Task CreateSession_WithMissingProgramId_Returns404_program_not_found_AndCreatesNothing(string? key)
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            var controller = CreateControllerWithUser(context, 1);

            var result = await controller.CreateSession(
                new SessionCreateRequestDto
                {
                    Name = "S",
                    Date = DateTime.UtcNow,
                    ProgramId = 999,
                    ClientOperationId = key is null ? null : Guid.Parse(key),
                },
                CancellationToken.None);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(SessionCreateErrorCodes.ProgramNotFound, CodeOf(notFound.Value));
            Assert.Empty(context.Sessions);
            Assert.Empty(context.SessionCreateOperations);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("33333333-3333-3333-3333-333333333333")]
        public async Task CreateSession_WithForeignProgramId_Returns404_AndForeignProgramUntouched(string? key)
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);
            await SeedProgramAsync(context, id: 10, ownerUserId: 2); // belongs to user 2
            var controller = CreateControllerWithUser(context, 1);   // acting as user 1

            var result = await controller.CreateSession(
                new SessionCreateRequestDto
                {
                    Name = "S",
                    Date = DateTime.UtcNow,
                    ProgramId = 10,
                    ClientOperationId = key is null ? null : Guid.Parse(key),
                },
                CancellationToken.None);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(SessionCreateErrorCodes.ProgramNotFound, CodeOf(notFound.Value));
            Assert.Empty(context.Sessions);
            Assert.Empty(context.SessionCreateOperations);
            Assert.Equal(2, (await context.Programs.SingleAsync()).UserId); // untouched
        }

        [Fact]
        public async Task CreateSession_WithForeignProgramWorkoutId_Returns404()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);
            await SeedProgramAsync(context, id: 10, ownerUserId: 2);
            await SeedProgramWorkoutAsync(context, id: 100, programId: 10);
            var controller = CreateControllerWithUser(context, 1);

            var result = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "S", Date = DateTime.UtcNow, ProgramWorkoutId = 100 },
                CancellationToken.None);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(SessionCreateErrorCodes.ProgramNotFound, CodeOf(notFound.Value));
            Assert.Empty(context.Sessions);
        }

        [Fact]
        public async Task CreateSession_WithOwnedProgramWorkoutId_Succeeds()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await SeedProgramAsync(context, id: 10, ownerUserId: 1);
            await SeedProgramWorkoutAsync(context, id: 100, programId: 10);
            var controller = CreateControllerWithUser(context, 1);

            var result = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "S", Date = DateTime.UtcNow, ProgramWorkoutId = 100 },
                CancellationToken.None);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(100, (await context.Sessions.SingleAsync()).ProgramWorkoutId);
        }

        [Fact]
        public async Task CreateSession_WithMissingProgramWorkoutId_Returns404_AndCreatesNothing()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            var controller = CreateControllerWithUser(context, 1);

            var result = await controller.CreateSession(
                new SessionCreateRequestDto { Name = "S", Date = DateTime.UtcNow, ProgramWorkoutId = 424242 },
                CancellationToken.None);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(SessionCreateErrorCodes.ProgramNotFound, CodeOf(notFound.Value));
            Assert.Empty(context.Sessions);
        }

        [Fact]
        public async Task CreateSession_ProgramIdAndMismatchedProgramWorkoutId_Returns404()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await SeedProgramAsync(context, id: 10, ownerUserId: 1);
            await SeedProgramAsync(context, id: 11, ownerUserId: 1);
            await SeedProgramWorkoutAsync(context, id: 100, programId: 11); // workout is in program 11
            var controller = CreateControllerWithUser(context, 1);

            var result = await controller.CreateSession(
                new SessionCreateRequestDto
                {
                    Name = "S",
                    Date = DateTime.UtcNow,
                    ProgramId = 10,
                    ProgramWorkoutId = 100, // ...but caller claims program 10
                },
                CancellationToken.None);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(SessionCreateErrorCodes.ProgramNotFound, CodeOf(notFound.Value));
            Assert.Empty(context.Sessions);
        }

        [Fact]
        public async Task CreateSession_ProgramIdAndMatchingProgramWorkoutId_Succeeds()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await SeedProgramAsync(context, id: 10, ownerUserId: 1);
            await SeedProgramWorkoutAsync(context, id: 100, programId: 10);
            var controller = CreateControllerWithUser(context, 1);

            var result = await controller.CreateSession(
                new SessionCreateRequestDto
                {
                    Name = "S",
                    Date = DateTime.UtcNow,
                    ProgramId = 10,
                    ProgramWorkoutId = 100,
                },
                CancellationToken.None);

            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        [Fact]
        public async Task CreateSession_ProgramEnumeration_MissingAndForeignAreIndistinguishable()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);
            await SeedProgramAsync(context, id: 10, ownerUserId: 2); // exists, foreign
            // ids 11..15 do not exist at all
            var controller = CreateControllerWithUser(context, 1);

            var responses = new List<(int statusCode, string? code)>();
            foreach (var probe in new[] { 10, 11, 12, 13, 14, 15 })
            {
                var r = await controller.CreateSession(
                    new SessionCreateRequestDto { Name = "S", Date = DateTime.UtcNow, ProgramId = probe },
                    CancellationToken.None);
                var nf = Assert.IsType<NotFoundObjectResult>(r.Result);
                responses.Add((nf.StatusCode ?? 0, CodeOf(nf.Value)));
            }

            // Every probe - the foreign-but-existing id 10 and the non-existent 11..15 -
            // returns the identical 404 / program_not_found. No existence oracle.
            Assert.All(responses, x =>
            {
                Assert.Equal(StatusCodes.Status404NotFound, x.statusCode);
                Assert.Equal(SessionCreateErrorCodes.ProgramNotFound, x.code);
            });
            Assert.Empty(context.Sessions);
        }

        [Fact]
        public async Task CreateSession_KeyedReplay_DoesNotRevalidateProgram_EvenAfterItBecomesForeign()
        {
            var context = GetInMemoryContext();
            await CreateTestUser(context, 1);
            await CreateTestUser(context, 2);
            var program = await SeedProgramAsync(context, id: 10, ownerUserId: 1);
            var controller = CreateControllerWithUser(context, 1);
            var key = Guid.NewGuid();

            var first = await controller.CreateSession(
                new SessionCreateRequestDto
                {
                    Name = "Original",
                    Date = DateTime.UtcNow,
                    ProgramId = 10,
                    ClientOperationId = key,
                },
                CancellationToken.None);
            var createdDto = Assert.IsType<SessionResponseDto>(
                Assert.IsType<CreatedAtActionResult>(first.Result).Value);

            // After the canonical first write, program 10 is reassigned to another user.
            // The session row is untouched; only the program's ownership changed.
            program.UserId = 2;
            context.Programs.Update(program);
            await context.SaveChangesAsync();

            // Replay with the same key must NOT re-check program ownership (which would now
            // fail), must NOT return 404, and must return the original canonical session
            // unchanged - first writer wins.
            var replay = await controller.CreateSession(
                new SessionCreateRequestDto
                {
                    Name = "DIFFERENT",
                    Date = DateTime.UtcNow,
                    ProgramId = 10,
                    ClientOperationId = key,
                },
                CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(replay.Result);
            var replayDto = Assert.IsType<SessionResponseDto>(ok.Value);
            Assert.Equal(createdDto.Id, replayDto.Id);
            Assert.Equal("Original", replayDto.Name);
            Assert.Equal(10, replayDto.ProgramId);
            Assert.Single(context.Sessions);
        }
    }
}
