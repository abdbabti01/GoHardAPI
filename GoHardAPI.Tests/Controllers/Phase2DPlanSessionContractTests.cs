using System.Security.Claims;
using System.Text.Json;
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
    /// Phase 2D API half of the end-to-end identity/target contract: AI exercise name →
    /// ExerciseTemplateResolver → ProgramWorkout.exercisesJson.exerciseTemplateId (+ targets) →
    /// materialized session exercise, asserted against the fixture the APP half consumes.
    /// </summary>
    public sealed class Phase2DPlanSessionContractTests : IDisposable
    {
        private const int UserId = 1;
        private readonly SqliteConnection _connection;
        private readonly TrainingContext _context;
        private readonly JsonElement _fixture;
        private readonly string _planReply;

        public Phase2DPlanSessionContractTests()
        {
            _fixture = JsonDocument.Parse(File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "phase2d_plan_session_contract.json"))).RootElement;
            var systemTemplateId = _fixture.GetProperty("systemTemplateId").GetInt32();
            var aiExerciseName = _fixture.GetProperty("aiExerciseName").GetString()!;

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _context = new TrainingContext(new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_connection).Options);
            _context.Database.EnsureCreated();
            _context.Users.Add(new User { Id = UserId, Name = "u", Username = "u", Email = "u@x.com", PasswordHash = "h" });
            _context.ExerciseTemplates.AddRange(
                new ExerciseTemplate { Id = systemTemplateId, Name = aiExerciseName },
                new ExerciseTemplate { Id = 2, Name = aiExerciseName, IsCustom = true, CreatedByUserId = UserId },
                new ExerciseTemplate { Id = 3, Name = "Dumbbell Bench Press" });
            _context.SaveChanges();

            _planReply =
                "Plan.\n\n```json\n" +
                "{\"programName\":\"P\",\"totalWeeks\":1,\"sessions\":[{\"name\":\"Day 1\",\"type\":\"strength\"," +
                "\"exercises\":[" +
                $"{{\"name\":{JsonSerializer.Serialize(aiExerciseName)},\"sets\":3,\"reps\":8,\"repsMax\":10,\"restTime\":90}}," +
                "{\"name\":\"DB Bench Press\",\"sets\":3,\"reps\":12,\"restTime\":60}" +
                "]}]}\n```\n";
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task AiName_Resolves_IntoPlanJson_AndSurvivesIntoMaterializedSession()
        {
            var body = await GeneratePlan();
            var workout = await _context.ProgramWorkouts.AsNoTracking()
                .Where(w => w.ProgramId == body.DraftProgramId && !w.IsRestDay).OrderBy(w => w.Id).FirstAsync();

            var entries = JsonDocument.Parse(workout.ExercisesJson).RootElement;
            AssertSubset(_fixture.GetProperty("programWorkoutExercise"), entries[0]);
            Assert.Equal(JsonValueKind.Null, entries[1].GetProperty("exerciseTemplateId").ValueKind); // unresolved, not fuzzy
            Assert.False(entries[1].TryGetProperty("repsMax", out var rm) && rm.ValueKind != JsonValueKind.Null);
            var key = entries[0].GetProperty("occurrenceKey").GetString();
            Assert.False(string.IsNullOrWhiteSpace(key));

            var sessions = new SessionsController(_context, new SessionCreateService(_context, NullLogger<SessionCreateService>.Instance))
            {
                ControllerContext = new ControllerContext { HttpContext = AuthedContext() },
            };
            var result = await sessions.CreateSessionFromProgramWorkout(
                new CreateSessionFromProgramWorkoutDto
                {
                    ProgramId = body.DraftProgramId!.Value,
                    ProgramWorkoutId = workout.Id,
                    ClientOperationId = Guid.NewGuid(),
                },
                CancellationToken.None);
            var session = result.Result switch
            {
                CreatedAtActionResult c => Assert.IsType<Session>(c.Value),
                OkObjectResult o => Assert.IsType<Session>(o.Value),
                _ => throw new Xunit.Sdk.XunitException($"unexpected {result.Result?.GetType().Name}"),
            };

            var first = session.Exercises.OrderBy(e => e.SortOrder).First();
            var wire = JsonSerializer.SerializeToElement(first, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            });
            AssertSubset(_fixture.GetProperty("sessionExercise"), wire);
            Assert.Equal(key, first.OccurrenceKey);
        }

        private async Task<ConversationDetailResponse> GeneratePlan()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AISettings:DefaultProvider"] = "Groq" })
                .Build();
            var ai = new AIService(new FakeProviderFactory(config, new[] { FakeProvider.Returning("Groq", _planReply) }), config, NullLogger<AIService>.Instance);
            var chat = new ChatController(_context, ai, new CurrentMeasurementsService(_context), NullLogger<ChatController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = AuthedContext() },
            };
            var result = await chat.GenerateWorkoutPlan(new GenerateWorkoutPlanRequest
            {
                Goal = "Build muscle", ExperienceLevel = "beginner", DaysPerWeek = 1, Equipment = "full gym",
            });
            var body = Assert.IsType<ConversationDetailResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.NotNull(body.DraftProgramId);
            return body;
        }

        private static DefaultHttpContext AuthedContext() => new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, UserId.ToString()) }, "TestAuth")),
        };

        private static void AssertSubset(JsonElement expected, JsonElement actual)
        {
            foreach (var p in expected.EnumerateObject())
            {
                Assert.True(actual.TryGetProperty(p.Name, out var v), $"missing '{p.Name}'");
                Assert.Equal(p.Value.GetRawText(), v.GetRawText());
            }
        }
    }
}
