using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GoHardAPI.Configuration;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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

        [Theory]
        [InlineData("Metric", "kilograms (kg)")]
        [InlineData("Imperial", "pounds (lb)")]
        public async Task Analyze_progress_prompt_describes_canonical_kg_once_history_reset_is_verified(
            string unitPreference, string expectedPreferredUnit)
        {
            (await _context.Users.FirstAsync(u => u.Id == UserId)).UnitPreference = unitPreference;
            await _context.SaveChangesAsync();

            var provider = FakeProvider.Returning("Groq", "analysis");

            var result = await Chat(provider, LiftedWeight(canonicalHistory: true)).AnalyzeProgress(new AnalyzeProgressRequest());

            var body = Assert.IsType<ConversationDetailResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
            var prompt = body.Messages.First(m => m.Role == "user").Content;

            // The ambiguous-history note no longer applies once history is verified canonical kg.
            Assert.DoesNotContain(ChatController.ProgressAnalysisLoadUnitNote, prompt);
            Assert.Contains(
                $"Load values are in kilograms (kg). The user prefers {expectedPreferredUnit}; " +
                "express load recommendations in that unit.",
                prompt);

            var benchLine = Assert.Single(prompt.Split('\n'), l => l.StartsWith("- Bench Press:"));
            Assert.Contains($"Max: {102.5:F1} kg, Avg: {101.25:F1} kg", benchLine);
        }

        [Fact]
        public async Task AI_created_planned_sets_are_stored_unitless_even_when_the_extracted_plan_has_a_weight()
        {
            var conversation = new ChatConversation
            {
                UserId = UserId,
                Title = "Workout Plan",
                Type = "workout_plan",
                CreatedAt = DateTime.UtcNow,
            };
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();
            _context.ChatMessages.Add(new ChatMessage
            {
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = "Here is your plan.",
                CreatedAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();

            const string extractedPlanJson =
                "{\"sessions\":[{\"name\":\"Day 1\",\"exercises\":" +
                "[{\"name\":\"Bench Press\",\"sets\":1,\"reps\":1,\"weight\":225}]}]}";
            var provider = FakeProvider.Returning("Groq", extractedPlanJson);

            var result = await Chat(provider).CreateSessionsFromPlan(conversation.Id);

            Assert.IsType<OkObjectResult>(result.Result);
            var createdSet = await _context.ExerciseSets.OrderByDescending(s => s.Id).FirstAsync();
            Assert.Equal(0, createdSet.Weight);
        }

        [Fact]
        public async Task AI_generated_program_workout_json_carries_no_weight_even_when_the_plan_has_one()
        {
            const string planReply =
                "A simple plan.\n\n```json\n" +
                "{\"programName\":\"Test Plan\",\"splitType\":\"Full Body\",\"totalWeeks\":1," +
                "\"sessions\":[{\"name\":\"Day 1\",\"type\":\"strength\",\"notes\":\"n\"," +
                "\"exercises\":[{\"name\":\"Squat\",\"sets\":3,\"reps\":8,\"weight\":225,\"restTime\":90,\"notes\":\"n\"}]}]}\n```\n";
            var provider = FakeProvider.Returning("Groq", planReply);

            var request = new GenerateWorkoutPlanRequest
            {
                Goal = "Build muscle",
                ExperienceLevel = "beginner",
                DaysPerWeek = 1,
            };

            var result = await Chat(provider).GenerateWorkoutPlan(request);

            var body = Assert.IsType<ConversationDetailResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.NotNull(body.DraftProgramId);

            var workout = await _context.ProgramWorkouts
                .Where(w => w.ProgramId == body.DraftProgramId && !w.IsRestDay)
                .FirstAsync();
            using var doc = JsonDocument.Parse(workout.ExercisesJson);
            var exercise = Assert.Single(doc.RootElement.EnumerateArray());
            Assert.Equal(JsonValueKind.Null, exercise.GetProperty("weight").ValueKind);
        }

        [Fact]
        public async Task ChatController_optional_liftedWeight_parameter_is_resolved_by_real_aspnetcore_di_when_registered()
        {
            // ASP.NET Core's controller activator builds controllers with
            // ActivatorUtilities.CreateInstance, resolving every constructor parameter from the
            // DI container by type - including an optional-with-default one - whenever that type
            // is registered. This proves the real app (which registers
            // IOptionsMonitor<LiftedWeightOptions> in Program.cs) injects the real monitor rather
            // than silently falling back to the null default, by observing that the injected
            // options flip AnalyzeProgress's output to the canonical-kg wording.
            var services = new ServiceCollection();
            services.Configure<LiftedWeightOptions>(o => o.CanonicalHistory = true);
            var provider = services.BuildServiceProvider();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AISettings:DefaultProvider"] = "Groq" })
                .Build();
            var fakeProvider = FakeProvider.Returning("Groq", "analysis");
            var aiService = new AIService(new FakeProviderFactory(config, new[] { fakeProvider }), config, NullLogger<AIService>.Instance);

            var controller = ActivatorUtilities.CreateInstance<ChatController>(
                provider, _context, aiService, new CurrentMeasurementsService(_context), NullLogger<ChatController>.Instance);
            controller.ControllerContext = AuthenticatedContext();

            var result = await controller.AnalyzeProgress(new AnalyzeProgressRequest());

            var body = Assert.IsType<ConversationDetailResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
            var prompt = body.Messages.First(m => m.Role == "user").Content;
            Assert.DoesNotContain(ChatController.ProgressAnalysisLoadUnitNote, prompt);
            Assert.Contains("Load values are in kilograms (kg).", prompt);
        }

        private static IOptionsMonitor<LiftedWeightOptions> LiftedWeight(bool canonicalHistory) =>
            new ServiceCollection()
                .Configure<LiftedWeightOptions>(o => o.CanonicalHistory = canonicalHistory)
                .BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<LiftedWeightOptions>>();

        private AnalyticsController Analytics() =>
            new(_context) { ControllerContext = AuthenticatedContext() };

        private ChatController Chat(FakeProvider provider, IOptionsMonitor<LiftedWeightOptions>? liftedWeight = null)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AISettings:DefaultProvider"] = "Groq" })
                .Build();
            var aiService = new AIService(new FakeProviderFactory(config, new[] { provider }), config, NullLogger<AIService>.Instance);
            return new ChatController(_context, aiService, new CurrentMeasurementsService(_context), NullLogger<ChatController>.Instance, liftedWeight)
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
