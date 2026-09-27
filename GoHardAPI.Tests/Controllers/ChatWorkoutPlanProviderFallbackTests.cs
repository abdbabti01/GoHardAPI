using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// POST /api/v1/chat/workout-plan end to end through <see cref="AIService"/>: a primary
    /// provider whose model is retired (Groq 404 model_not_found - the production failure)
    /// must no longer fail the request when a configured fallback provider works.
    /// </summary>
    public class ChatWorkoutPlanProviderFallbackTests : IDisposable
    {
        private const int UserId = 1;

        private const string PlanReply =
            "A simple full-body plan.\n\n```json\n" +
            "{\"programName\":\"Test Plan\",\"splitType\":\"Full Body\",\"totalWeeks\":4," +
            "\"sessions\":[{\"name\":\"Day 1: Full Body\",\"type\":\"strength\",\"notes\":\"n\"," +
            "\"exercises\":[{\"name\":\"Squat\",\"sets\":3,\"reps\":8,\"restTime\":90,\"notes\":\"n\"}]}]}\n```\n";

        private readonly SqliteConnection _connection;
        private readonly TrainingContext _context;

        public ChatWorkoutPlanProviderFallbackTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _context = new TrainingContext(
                new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_connection).Options);
            _context.Database.EnsureCreated();
            _context.Users.Add(new User { Id = UserId, Name = "u", Username = "u", Email = "u@x.com", PasswordHash = "h" });
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task Primary_model_not_found_falls_back_and_the_plan_is_generated()
        {
            var groq = FakeProvider.Throwing("Groq", ModelNotFound());
            var anthropic = FakeProvider.Returning("Anthropic", PlanReply);
            var controller = Chat(groq, anthropic);

            var result = await controller.GenerateWorkoutPlan(Request());

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<ConversationDetailResponse>(ok.Value);
            Assert.NotNull(body.DraftProgramId);
            Assert.Equal(1, groq.Calls);
            Assert.Equal(1, anthropic.Calls);
            Assert.Contains("fitness coach", groq.LastSystemPrompt);
        }

        [Fact]
        public async Task Every_provider_failing_returns_the_controlled_500()
        {
            var groq = FakeProvider.Throwing("Groq", ModelNotFound());
            var anthropic = FakeProvider.Throwing("Anthropic", ModelNotFound());
            var controller = Chat(groq, anthropic);

            var result = await controller.GenerateWorkoutPlan(Request());

            var error = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, error.StatusCode);
            Assert.Equal(1, groq.Calls);
            Assert.Equal(1, anthropic.Calls);
        }

        private static GenerateWorkoutPlanRequest Request() => new()
        {
            Goal = "Build muscle",
            ExperienceLevel = "beginner",
            DaysPerWeek = 1,
            Equipment = "full gym",
        };

        private static HttpRequestException ModelNotFound() => new(
            "Response status code does not indicate success: 404 (Not Found).", null, HttpStatusCode.NotFound);

        private ChatController Chat(params FakeProvider[] providers)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AISettings:DefaultProvider"] = "Groq" })
                .Build();
            var aiService = new AIService(new FakeProviderFactory(config, providers), config, NullLogger<AIService>.Instance);
            return new ChatController(_context, aiService, new CurrentMeasurementsService(_context), NullLogger<ChatController>.Instance)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) }, "TestAuth")),
                    },
                },
            };
        }
    }
}
