using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Converters;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Proves the real JSON body of the current Flutter client binds onto
    /// <see cref="SessionCreateRequestDto"/> through <c>System.Text.Json</c> configured the
    /// same way as <c>Program.cs</c> <c>AddJsonOptions</c> (camelCase policy,
    /// case-insensitive, UTC + date-only converters, default unmapped-member = Skip), and
    /// that each state-machine outcome maps to the right HTTP result (201 / 200 / 404 / 409
    /// / 410).
    ///
    /// A full <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{T}"/> boot
    /// is impractical here: <c>Program.cs</c> runs a database migration/bootstrap block at
    /// startup that rethrows on failure, so the host cannot start hermetically without also
    /// neutering that block. This test isolates the two things that matter for the contract
    /// - binding fidelity and outcome-to-status mapping.
    /// </summary>
    public class SessionCreateHttpBindingTests
    {
        // Mirrors GoHardAPI/Program.cs AddControllers().AddJsonOptions(...).
        private static readonly JsonSerializerOptions MvcLikeJson = BuildMvcLikeJson();

        private static JsonSerializerOptions BuildMvcLikeJson()
        {
            var o = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true, // ASP.NET Core MVC default
            };
            o.Converters.Add(new UtcDateTimeConverter());
            o.Converters.Add(new NullableUtcDateTimeConverter());
            return o;
        }

        private static TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static SessionsController Controller(TrainingContext ctx, int userId = 1)
        {
            var controller = new SessionsController(
                ctx, new SessionCreateService(ctx, NullLogger<SessionCreateService>.Instance));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
            return controller;
        }

        private static async Task SeedUser(TrainingContext ctx, int id = 1)
        {
            ctx.Users.Add(new User { Id = id, Name = "u", Email = $"u{id}@x.com", PasswordHash = "h" });
            await ctx.SaveChangesAsync();
        }

        // ---- binding fidelity -------------------------------------------------------------

        [Fact]
        public void Bind_CurrentFlutterForegroundBody_MapsScalarsAndKey_IgnoresServerControlledMembers()
        {
            // Exactly the shape lib/data/repositories/session_repository.dart posts:
            // full session.toJson() incl. id/userId/version/exercises + a clientOperationId.
            const string body = """
            {
              "id": 123, "userId": 9, "version": 4,
              "date": "2026-09-03",
              "duration": 3600, "notes": "n", "type": "strength", "name": "Pull Day",
              "status": "in_progress",
              "startedAt": "2026-09-03T07:15:00.000Z",
              "completedAt": null, "pausedAt": null,
              "programId": 77, "programWorkoutId": 88,
              "exercises": [ { "name": "Row", "sets": [ { "reps": 8 } ] } ],
              "clientOperationId": "9f8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d"
            }
            """;

            var dto = JsonSerializer.Deserialize<SessionCreateRequestDto>(body, MvcLikeJson)!;

            Assert.Equal("Pull Day", dto.Name);
            Assert.Equal("strength", dto.Type);
            Assert.Equal("in_progress", dto.Status);
            Assert.Equal(3600, dto.Duration);
            Assert.Equal(new DateTime(2026, 9, 3), dto.Date.Date);
            // The converter keeps the wall clock and marks it UTC (no local-time shift).
            Assert.Equal(DateTimeKind.Utc, dto.StartedAt!.Value.Kind);
            Assert.Equal(new DateTime(2026, 9, 3, 7, 15, 0), dto.StartedAt!.Value); // ticks compare, Kind-insensitive
            Assert.Equal(77, dto.ProgramId);
            Assert.Equal(88, dto.ProgramWorkoutId);
            Assert.Equal(Guid.Parse("9f8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d"), dto.ClientOperationId);

            // id / userId / version / exercises have no target on the DTO -> dropped.
            var session = dto.ToNewSession(userId: 42);
            Assert.Equal(0, session.Id);
            Assert.Equal(42, session.UserId);
            Assert.Equal(1, session.Version);
            Assert.Empty(session.Exercises);
        }

        [Fact]
        public void Bind_CurrentFlutterSyncStrippedBody_HasNoKey_AndBindsScalars()
        {
            // lib/core/services/sync_service.dart strips id/exercises/version/programId/
            // programWorkoutId and never adds clientOperationId.
            const string body = """
            { "userId": 9, "date": "2026-09-03", "duration": null, "notes": null,
              "type": null, "name": "Draft", "status": "draft",
              "startedAt": null, "completedAt": null, "pausedAt": null }
            """;

            var dto = JsonSerializer.Deserialize<SessionCreateRequestDto>(body, MvcLikeJson)!;

            Assert.Null(dto.ClientOperationId);
            Assert.Equal("Draft", dto.Name);
            Assert.Equal("draft", dto.Status);
            Assert.Null(dto.Duration);
        }

        // ---- outcome -> status mapping --------------------------------------------------

        [Fact]
        public async Task Map_LegacyUnkeyed_Returns201_WithCanonicalDto()
        {
            var ctx = NewContext();
            await SeedUser(ctx);
            var dto = JsonSerializer.Deserialize<SessionCreateRequestDto>(
                """{ "date": "2026-09-03", "name": "S", "status": "draft" }""", MvcLikeJson)!;

            var result = await Controller(ctx).CreateSession(dto, CancellationToken.None);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.IsType<SessionResponseDto>(created.Value);
        }

        [Fact]
        public async Task Map_KeyedFirstThenReplay_Returns201Then200_SameSession()
        {
            var ctx = NewContext();
            await SeedUser(ctx);
            var key = Guid.NewGuid();
            var dto = JsonSerializer.Deserialize<SessionCreateRequestDto>(
                $$"""{ "date": "2026-09-03", "name": "S", "status": "draft", "clientOperationId": "{{key}}" }""",
                MvcLikeJson)!;

            var first = await Controller(ctx).CreateSession(dto, CancellationToken.None);
            var firstDto = Assert.IsType<SessionResponseDto>(
                Assert.IsType<CreatedAtActionResult>(first.Result).Value);

            var replay = await Controller(ctx).CreateSession(dto, CancellationToken.None);
            var replayDto = Assert.IsType<SessionResponseDto>(
                Assert.IsType<OkObjectResult>(replay.Result).Value);

            Assert.Equal(firstDto.Id, replayDto.Id);
        }

        [Fact]
        public async Task Map_CanceledOperation_Returns409()
        {
            var ctx = NewContext();
            await SeedUser(ctx);
            var key = Guid.NewGuid();
            ctx.SessionCreateOperations.Add(new SessionCreateOperation
            {
                UserId = 1,
                ClientOperationId = key,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                CanceledAt = DateTime.UtcNow.AddMinutes(-1),
            });
            await ctx.SaveChangesAsync();
            var dto = new SessionCreateRequestDto { Name = "S", Date = DateTime.UtcNow, ClientOperationId = key };

            var result = await Controller(ctx).CreateSession(dto, CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task Map_CompletedOperationWhoseSessionIsGone_Returns410()
        {
            var ctx = NewContext();
            await SeedUser(ctx);
            var key = Guid.NewGuid();
            ctx.SessionCreateOperations.Add(new SessionCreateOperation
            {
                UserId = 1,
                ClientOperationId = key,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = DateTime.UtcNow.AddMinutes(-4),
                SessionId = null,
            });
            await ctx.SaveChangesAsync();
            var dto = new SessionCreateRequestDto { Name = "S", Date = DateTime.UtcNow, ClientOperationId = key };

            var result = await Controller(ctx).CreateSession(dto, CancellationToken.None);

            var obj = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status410Gone, obj.StatusCode);
        }

        [Fact]
        public async Task Map_ForeignProgramId_Returns404_program_not_found()
        {
            var ctx = NewContext();
            await SeedUser(ctx, 1);
            await SeedUser(ctx, 2);
            ctx.Programs.Add(new GoHardAPI.Models.Program { Id = 5, UserId = 2, Title = "P" });
            await ctx.SaveChangesAsync();
            var dto = new SessionCreateRequestDto { Name = "S", Date = DateTime.UtcNow, ProgramId = 5 };

            var result = await Controller(ctx, userId: 1).CreateSession(dto, CancellationToken.None);

            var nf = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(SessionCreateErrorCodes.ProgramNotFound,
                nf.Value!.GetType().GetProperty("code")!.GetValue(nf.Value));
        }
    }
}
