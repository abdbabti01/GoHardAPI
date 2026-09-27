using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
    /// Stored set weights have no reliably known unit (the app has always asked for
    /// lbs while analytics called the same numbers kg). Presentation must not claim a
    /// unit, and must not change the numbers.
    /// </summary>
    public class LiftedWeightUnitPresentationTests : IDisposable
    {
        private const int UserId = 1;
        private const int TemplateId = 7;

        // Matches a weight unit written as a word/suffix: "100kg", "100 kg", "lbs", "lb".
        private static readonly Regex UnitWord = new(@"(?i)(\d\s*(kg|lbs?)\b)|\b(kg|lbs?)\b");

        private readonly SqliteConnection _connection;
        private readonly TrainingContext _context;

        public LiftedWeightUnitPresentationTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _context = new TrainingContext(
                new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_connection).Options);
            _context.Database.EnsureCreated();
            _context.Users.Add(new User { Id = UserId, Name = "u", Username = "u", Email = "u@x.com", PasswordHash = "h" });
            _context.ExerciseTemplates.Add(new ExerciseTemplate { Id = TemplateId, Name = "Bench Press" });

            // 10 × 100 + 5 × 102.5 = 1512.5 ; max weight 102.5
            var session = new Session
            {
                UserId = UserId,
                Name = "Push",
                Status = SessionStatus.Completed,
                Date = DateTime.UtcNow.AddDays(-1),
            };
            session.Exercises.Add(new Exercise
            {
                Name = "Bench Press",
                ExerciseTemplateId = TemplateId,
                ExerciseSets = new List<ExerciseSet>
                {
                    new() { SetNumber = 1, Reps = 10, Weight = 100, IsCompleted = true },
                    new() { SetNumber = 2, Reps = 5, Weight = 102.5, IsCompleted = true },
                },
            });
            _context.Sessions.Add(session);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task Volume_over_time_keeps_the_value_and_labels_it_without_a_unit()
        {
            var result = await Analytics().GetVolumeOverTime(days: 30);

            var point = Assert.Single(Assert.IsType<OkObjectResult>(result.Result).Value as List<ProgressDataPoint> ?? new());
            Assert.Equal(1512.5, point.Value);
            Assert.DoesNotMatch(UnitWord, point.Label ?? "");
            Assert.Equal(point.Value.ToString("F0", CultureInfo.CurrentCulture), point.Label);
        }

        [Fact]
        public async Task Exercise_progress_keeps_the_max_weight_and_labels_it_without_a_unit()
        {
            var result = await Analytics().GetExerciseProgressOverTime(TemplateId, days: 30);

            var point = Assert.Single(Assert.IsType<OkObjectResult>(result.Result).Value as List<ProgressDataPoint> ?? new());
            Assert.Equal(102.5, point.Value);
            Assert.DoesNotMatch(UnitWord, point.Label ?? "");
            Assert.Equal(point.Value.ToString("F1", CultureInfo.CurrentCulture), point.Label);
        }

        [Fact]
        public async Task Analyze_progress_prompt_does_not_call_stored_weights_kg()
        {
            var provider = FakeProvider.Returning("Groq", "analysis");

            var result = await Chat(provider).AnalyzeProgress(new AnalyzeProgressRequest());

            var body = Assert.IsType<ConversationDetailResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
            var prompt = body.Messages.First(m => m.Role == "user").Content;

            // The data-limitation note is present...
            Assert.Contains(ChatController.ProgressAnalysisLoadUnitNote, prompt);
            // ...and the per-exercise data lines carry the raw numbers with no unit.
            var benchLine = Assert.Single(prompt.Split('\n'), l => l.StartsWith("- Bench Press:"));
            Assert.DoesNotMatch(UnitWord, benchLine);
            Assert.Contains($"Max load: {102.5}", benchLine);
            Assert.Contains($"Avg load: {101.25:F1}", benchLine);
            // Outside the explanatory note, the prompt never names a weight unit.
            Assert.DoesNotMatch(UnitWord, prompt.Replace(ChatController.ProgressAnalysisLoadUnitNote, ""));
            Assert.Equal(1, provider.Calls);
        }

        private AnalyticsController Analytics() =>
            new(_context) { ControllerContext = AuthenticatedContext() };

        private ChatController Chat(FakeProvider provider)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AISettings:DefaultProvider"] = "Groq" })
                .Build();
            var aiService = new AIService(new FakeProviderFactory(config, new[] { provider }), config, NullLogger<AIService>.Instance);
            return new ChatController(_context, aiService, new CurrentMeasurementsService(_context), NullLogger<ChatController>.Instance)
            {
                ControllerContext = AuthenticatedContext(),
            };
        }

        private static ControllerContext AuthenticatedContext() => new()
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) }, "TestAuth")),
            },
        };
    }
}
