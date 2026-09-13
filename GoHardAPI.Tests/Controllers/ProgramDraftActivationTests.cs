using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.DTOs;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Services.AI;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Real-PostgreSQL evidence for the Phase 2 exact-preview program contract:
    /// <c>ChatController.CreateProgramFromPlan</c> activates the already-materialized Draft
    /// program instead of re-deriving it from AI, activation is idempotent/race-safe via
    /// <see cref="ProgramActivationService"/>, foreign users are rejected, and a draft that was
    /// superseded by a "Regenerate Plan" reply in the same conversation is never silently
    /// activated. Every test here constructs <see cref="AIService"/> with no configured
    /// provider/API key (same pattern as <c>MealPlanApplyPostgresTests</c>) — if the endpoint
    /// under test still called AI, these calls would throw/500 instead of succeeding, so a
    /// successful response is itself evidence no AI call happened.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    [Collection(HistoryPreservationPostgresCollection.Name)]
    public sealed class ProgramDraftActivationTests
    {
        private readonly HistoryPreservationPostgresFixture _pg;

        public ProgramDraftActivationTests(HistoryPreservationPostgresFixture pg) => _pg = pg;

        private static ChatController Chat(TrainingContext ctx, int userId)
        {
            var providerFactory = new AIProviderFactory(
                new ServiceCollection().BuildServiceProvider(),
                new ConfigurationBuilder().Build(),
                NullLogger<AIProviderFactory>.Instance);
            var aiService = new AIService(
                providerFactory,
                new ConfigurationBuilder().Build(),
                NullLogger<AIService>.Instance);
            var currentMeasurements = new CurrentMeasurementsService(ctx);

            var controller = new ChatController(ctx, aiService, currentMeasurements, NullLogger<ChatController>.Instance);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"))
                }
            };
            return controller;
        }

        private static ProgramsController Programs(TrainingContext ctx, int userId)
        {
            var controller = new ProgramsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"))
                }
            };
            return controller;
        }

        private async Task<int> SeedUser()
        {
            await using var ctx = _pg.NewContext();
            var u = new User
            {
                Name = "U",
                Username = $"u{Guid.NewGuid():N}"[..20],
                Email = $"u{Guid.NewGuid():N}@x.com",
                PasswordHash = "h",
                DateCreated = DateTime.UtcNow,
                UnitPreference = "Metric",
            };
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();
            return u.Id;
        }

        private static string ExercisesJsonFor(params string[] names)
        {
            var raw = JsonSerializer.Serialize(names.Select(n => new { name = n, sets = 3, reps = 10 }));
            var normalized = ProgramWorkoutExerciseOccurrences.Normalize(raw);
            Assert.True(normalized.IsValid);
            return normalized.Json;
        }

        /// <summary>
        /// Seeds a conversation-linked Draft program with a real, multi-week, keyed schedule —
        /// the same shape <c>ChatController.BuildProgramWorkouts</c> produces — without ever
        /// calling AI (mirrors how <c>MealPlanApplyPostgresTests</c> avoids the AI dependency).
        /// </summary>
        private async Task<(int conversationId, int programId, string[] occurrenceKeys)> SeedDraftAsync(
            int userId, int totalWeeks = 2, int daysPerWeek = 3)
        {
            await using var ctx = _pg.NewContext();

            var conversation = new ChatConversation
            {
                UserId = userId,
                Title = "Workout Plan - Strength",
                Type = "workout_plan",
                CreatedAt = DateTime.UtcNow,
            };
            ctx.ChatConversations.Add(conversation);
            await ctx.SaveChangesAsync();

            var assistantMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = "```json\n{}\n```",
                CreatedAt = DateTime.UtcNow,
                ContentType = "workout_plan",
            };
            ctx.ChatMessages.Add(new ChatMessage
            {
                ConversationId = conversation.Id,
                Role = "user",
                Content = "give me a plan",
                CreatedAt = DateTime.UtcNow,
            });
            ctx.ChatMessages.Add(assistantMessage);
            await ctx.SaveChangesAsync();

            var today = DateTime.UtcNow.Date;
            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            var startDate = today.AddDays(daysUntilMonday);

            var program = new Models.Program
            {
                UserId = userId,
                Title = "AI Strength Plan",
                Description = "AI-generated 3-day workout plan for strength",
                TotalWeeks = totalWeeks,
                CurrentWeek = 1,
                CurrentDay = 1,
                StartDate = startDate,
                EndDate = startDate.AddDays(totalWeeks * 7),
                IsActive = false,
                Status = ProgramStatus.Draft.ToApiString(),
                SourceConversationId = conversation.Id,
                CreatedAt = assistantMessage.CreatedAt.AddSeconds(1),
            };
            ctx.Programs.Add(program);
            await ctx.SaveChangesAsync();

            var occurrenceKeys = new List<string>();
            var workouts = new List<ProgramWorkout>();
            for (int weekDay = 1; weekDay <= totalWeeks * 7; weekDay++)
            {
                var dayNumber = ((weekDay - 1) % 7) + 1;
                var weekNumber = ((weekDay - 1) / 7) + 1;
                var isWorkoutDay = (weekDay - 1) % 7 < daysPerWeek;
                var scheduledDate = startDate.AddDays((weekNumber - 1) * 7 + (dayNumber - 1)).Date;

                if (isWorkoutDay)
                {
                    var exercisesJson = ExercisesJsonFor("Squat", "Bench Press");
                    foreach (var el in JsonDocument.Parse(exercisesJson).RootElement.EnumerateArray())
                    {
                        occurrenceKeys.Add(el.GetProperty("occurrenceKey").GetString()!);
                    }

                    workouts.Add(new ProgramWorkout
                    {
                        ProgramId = program.Id,
                        WeekNumber = weekNumber,
                        DayNumber = dayNumber,
                        WorkoutName = $"Day {dayNumber}",
                        WorkoutType = "Strength",
                        ExercisesJson = exercisesJson,
                        OrderIndex = dayNumber,
                        IsRestDay = false,
                        ScheduledDate = scheduledDate,
                    });
                }
                else
                {
                    workouts.Add(new ProgramWorkout
                    {
                        ProgramId = program.Id,
                        WeekNumber = weekNumber,
                        DayNumber = dayNumber,
                        WorkoutName = "Rest Day",
                        WorkoutType = "Rest",
                        ExercisesJson = "[]",
                        OrderIndex = dayNumber,
                        IsRestDay = true,
                        ScheduledDate = scheduledDate,
                    });
                }
            }

            ctx.ProgramWorkouts.AddRange(workouts);
            await ctx.SaveChangesAsync();

            return (conversation.Id, program.Id, occurrenceKeys.ToArray());
        }

        [DockerRequiredFact]
        public async Task CreateProgramFromPlan_ActivatesTheExactDraft_NoAiCall_KeysPreserved()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser();
            var (conversationId, programId, occurrenceKeys) = await SeedDraftAsync(userId, totalWeeks: 2, daysPerWeek: 3);

            await using var ctx = _pg.NewContext();
            var controller = Chat(ctx, userId);

            var response = await controller.CreateProgramFromPlan(conversationId, new CreateProgramRequest());

            var ok = Assert.IsType<OkObjectResult>(response.Result);
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));

            Assert.Equal(programId, doc.RootElement.GetProperty("program").GetProperty("id").GetInt32());
            Assert.True(doc.RootElement.GetProperty("activated").GetBoolean());

            await using var verifyCtx = _pg.NewContext();
            var activated = await verifyCtx.Programs
                .Include(p => p.Workouts)
                .AsNoTracking()
                .FirstAsync(p => p.Id == programId);

            Assert.Equal(ProgramStatus.Active.ToApiString(), activated.Status);
            Assert.True(activated.IsActive);
            Assert.Equal(2 * 7, activated.Workouts.Count); // full multi-week schedule preserved
            Assert.Equal(2 * 3, activated.Workouts.Count(w => !w.IsRestDay));
            Assert.All(activated.Workouts, w => Assert.NotNull(w.ScheduledDate));

            var persistedKeys = activated.Workouts
                .Where(w => !w.IsRestDay)
                .SelectMany(w => JsonDocument.Parse(w.ExercisesJson).RootElement.EnumerateArray())
                .Select(e => e.GetProperty("occurrenceKey").GetString())
                .OrderBy(k => k)
                .ToArray();

            Assert.Equal(occurrenceKeys.OrderBy(k => k), persistedKeys);
        }

        [DockerRequiredFact]
        public async Task CreateProgramFromPlan_DuplicateCall_ConvergesOnSameProgram_NoDuplicate()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser();
            var (conversationId, programId, _) = await SeedDraftAsync(userId);

            await using (var ctx1 = _pg.NewContext())
            {
                var r1 = await Chat(ctx1, userId).CreateProgramFromPlan(conversationId, new CreateProgramRequest());
                Assert.IsType<OkObjectResult>(r1.Result);
            }

            // Retry after a "lost response" (duplicate tap / client retry) must converge, not duplicate.
            await using (var ctx2 = _pg.NewContext())
            {
                var r2 = await Chat(ctx2, userId).CreateProgramFromPlan(conversationId, new CreateProgramRequest());
                var ok2 = Assert.IsType<OkObjectResult>(r2.Result);
                using var doc2 = JsonDocument.Parse(JsonSerializer.Serialize(ok2.Value));
                Assert.Equal(programId, doc2.RootElement.GetProperty("program").GetProperty("id").GetInt32());
                Assert.False(doc2.RootElement.GetProperty("activated").GetBoolean());
            }

            await using var verifyCtx = _pg.NewContext();
            var programCount = await verifyCtx.Programs.CountAsync(p => p.SourceConversationId == conversationId);
            Assert.Equal(1, programCount);
        }

        [DockerRequiredFact]
        public async Task ActivateAsync_ConcurrentActivation_ExactlyOneWins_BothConvergeOnSameProgram()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser();
            var (_, programId, _) = await SeedDraftAsync(userId);

            await using var ctxA = _pg.NewContext();
            await using var ctxB = _pg.NewContext();

            var programA = await ctxA.Programs.Include(p => p.Workouts).FirstAsync(p => p.Id == programId);
            var programB = await ctxB.Programs.Include(p => p.Workouts).FirstAsync(p => p.Id == programId);

            var taskA = ProgramActivationService.ActivateAsync(ctxA, programA, null, null, null, null);
            var taskB = ProgramActivationService.ActivateAsync(ctxB, programB, null, null, null, null);
            var results = await Task.WhenAll(taskA, taskB);

            Assert.Contains(results, r => r.Outcome == ProgramActivationOutcome.Activated);
            Assert.Contains(results, r => r.Outcome == ProgramActivationOutcome.AlreadyActive);
            Assert.All(results, r => Assert.Equal(programId, r.Program!.Id));

            await using var verifyCtx = _pg.NewContext();
            var finalStatus = await verifyCtx.Programs.AsNoTracking()
                .Where(p => p.Id == programId)
                .Select(p => p.Status)
                .FirstAsync();
            Assert.Equal(ProgramStatus.Active.ToApiString(), finalStatus);
        }

        [DockerRequiredFact]
        public async Task CreateProgramFromPlan_ForeignUser_ReturnsNotFound()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var ownerId = await SeedUser();
            var attackerId = await SeedUser();
            var (conversationId, programId, _) = await SeedDraftAsync(ownerId);

            await using var ctx = _pg.NewContext();
            var response = await Chat(ctx, attackerId).CreateProgramFromPlan(conversationId, new CreateProgramRequest());

            Assert.IsType<NotFoundObjectResult>(response.Result);

            await using var verifyCtx = _pg.NewContext();
            var stillDraft = await verifyCtx.Programs.AsNoTracking().FirstAsync(p => p.Id == programId);
            Assert.Equal(ProgramStatus.Draft.ToApiString(), stillDraft.Status);
        }

        [DockerRequiredFact]
        public async Task CreateProgramFromPlan_LegacyClient_RegeneratedConversation_RejectsStaleDraft()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            // No DraftRevision on the request — exercises the LEGACY fallback heuristic only
            // (ProgramActivationService.CheckDraftFreshnessAsync's "any newer assistant message"
            // path). A new client that echoes the correct DraftRevision would NOT be rejected
            // here even though a newer message exists — see
            // CreateProgramFromPlan_WithMatchingRevision_UnrelatedNewerMessage_StillActivates.
            var userId = await SeedUser();
            var (conversationId, programId, _) = await SeedDraftAsync(userId);

            // Simulate the user tapping "Regenerate Plan" in the same conversation
            // (ChatConversationScreen._regeneratePlan): a NEW assistant reply lands after the
            // draft was already materialized.
            await using (var ctx = _pg.NewContext())
            {
                ctx.ChatMessages.Add(new ChatMessage
                {
                    ConversationId = conversationId,
                    Role = "assistant",
                    Content = "here's a different plan",
                    CreatedAt = DateTime.UtcNow.AddSeconds(5),
                });
                await ctx.SaveChangesAsync();
            }

            await using var actCtx = _pg.NewContext();
            var response = await Chat(actCtx, userId).CreateProgramFromPlan(conversationId, new CreateProgramRequest());

            var conflict = Assert.IsType<ConflictObjectResult>(response.Result);
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(conflict.Value));
            Assert.Equal("DRAFT_STALE", doc.RootElement.GetProperty("code").GetString());

            await using var verifyCtx = _pg.NewContext();
            var stillDraft = await verifyCtx.Programs.AsNoTracking().FirstAsync(p => p.Id == programId);
            Assert.Equal(ProgramStatus.Draft.ToApiString(), stillDraft.Status);
        }

        [DockerRequiredFact]
        public async Task CreateProgramFromPlan_WithMatchingRevision_UnrelatedNewerMessage_StillActivates()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser();
            var (conversationId, programId, _) = await SeedDraftAsync(userId);

            string revision;
            await using (var ctx = _pg.NewContext())
            {
                var program = await ctx.Programs.Include(p => p.Workouts).AsNoTracking().FirstAsync(p => p.Id == programId);
                revision = ProgramActivationService.ComputeContentRevision(program);
            }

            // An unrelated later message in the same conversation (e.g. a stray follow-up
            // question) must NOT be mistaken for a regeneration when the client proves — via a
            // matching content fingerprint — that it previewed exactly this content.
            await using (var ctx = _pg.NewContext())
            {
                ctx.ChatMessages.Add(new ChatMessage
                {
                    ConversationId = conversationId,
                    Role = "assistant",
                    Content = "totally unrelated reply",
                    CreatedAt = DateTime.UtcNow.AddSeconds(5),
                });
                await ctx.SaveChangesAsync();
            }

            await using var actCtx = _pg.NewContext();
            var response = await Chat(actCtx, userId).CreateProgramFromPlan(
                conversationId, new CreateProgramRequest { DraftRevision = revision });

            var ok = Assert.IsType<OkObjectResult>(response.Result);
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
            Assert.True(doc.RootElement.GetProperty("activated").GetBoolean());

            await using var verifyCtx = _pg.NewContext();
            var activated = await verifyCtx.Programs.AsNoTracking().FirstAsync(p => p.Id == programId);
            Assert.Equal(ProgramStatus.Active.ToApiString(), activated.Status);
        }

        [DockerRequiredFact]
        public async Task CreateProgramFromPlan_WithStaleRevision_RejectedWithCurrentRevision()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser();
            var (conversationId, programId, _) = await SeedDraftAsync(userId);

            string actualRevision;
            await using (var ctx = _pg.NewContext())
            {
                var program = await ctx.Programs.Include(p => p.Workouts).AsNoTracking().FirstAsync(p => p.Id == programId);
                actualRevision = ProgramActivationService.ComputeContentRevision(program);
            }

            await using var actCtx = _pg.NewContext();
            var response = await Chat(actCtx, userId).CreateProgramFromPlan(
                conversationId, new CreateProgramRequest { DraftRevision = "not-the-real-revision" });

            var conflict = Assert.IsType<ConflictObjectResult>(response.Result);
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(conflict.Value));
            Assert.Equal("DRAFT_STALE", doc.RootElement.GetProperty("code").GetString());
            Assert.Equal(actualRevision, doc.RootElement.GetProperty("currentRevision").GetString());

            await using var verifyCtx = _pg.NewContext();
            var stillDraft = await verifyCtx.Programs.AsNoTracking().FirstAsync(p => p.Id == programId);
            Assert.Equal(ProgramStatus.Draft.ToApiString(), stillDraft.Status);
        }

        [DockerRequiredFact]
        public async Task CreateProgramFromPlan_DraftEditedThroughWorkoutEndpoint_OldRevisionRejected_NewRevisionAccepted()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            // Proves the gap the old "any newer assistant message" proxy could never close: a
            // Draft's content can change through ProgramsController's ordinary workout endpoints
            // (no chat message involved at all), and the content fingerprint catches it.
            var userId = await SeedUser();
            var (_, programId, _) = await SeedDraftAsync(userId, totalWeeks: 1, daysPerWeek: 1);

            string originalRevision;
            int firstWorkoutId;
            await using (var ctx = _pg.NewContext())
            {
                var program = await ctx.Programs.Include(p => p.Workouts).AsNoTracking().FirstAsync(p => p.Id == programId);
                originalRevision = ProgramActivationService.ComputeContentRevision(program);
                firstWorkoutId = program.Workouts.OrderBy(w => w.DayNumber).First(w => !w.IsRestDay).Id;
            }

            // Edit the workout directly through the existing generic endpoint — no chat message,
            // no regeneration, just a plain edit.
            await using (var ctx = _pg.NewContext())
            {
                var workout = await ctx.ProgramWorkouts.FirstAsync(w => w.Id == firstWorkoutId);
                var edited = new ProgramWorkout
                {
                    Id = workout.Id,
                    WorkoutName = "Edited Name",
                    WorkoutType = workout.WorkoutType,
                    ExercisesJson = ExercisesJsonFor("Deadlift"),
                    WeekNumber = workout.WeekNumber,
                    DayNumber = workout.DayNumber,
                    OrderIndex = workout.OrderIndex,
                };
                var editResult = await Programs(ctx, userId).UpdateWorkout(firstWorkoutId, edited);
                Assert.IsType<NoContentResult>(editResult);
            }

            string newRevision;
            await using (var ctx = _pg.NewContext())
            {
                var program = await ctx.Programs.Include(p => p.Workouts).AsNoTracking().FirstAsync(p => p.Id == programId);
                newRevision = ProgramActivationService.ComputeContentRevision(program);
            }
            Assert.NotEqual(originalRevision, newRevision);

            // Activating with the OLD (now-stale) revision must be rejected.
            await using (var ctx = _pg.NewContext())
            {
                var stalePut = await Programs(ctx, userId).ActivateDraftProgram(
                    programId, new ActivateProgramRequest { DraftRevision = originalRevision });
                Assert.IsType<ConflictObjectResult>(stalePut);
            }

            // Activating with the CURRENT revision succeeds.
            await using (var ctx = _pg.NewContext())
            {
                var freshPut = await Programs(ctx, userId).ActivateDraftProgram(
                    programId, new ActivateProgramRequest { DraftRevision = newRevision });
                Assert.IsType<OkObjectResult>(freshPut);
            }

            await using var verifyCtx = _pg.NewContext();
            var activated = await verifyCtx.Programs.Include(p => p.Workouts).AsNoTracking().FirstAsync(p => p.Id == programId);
            Assert.Equal(ProgramStatus.Active.ToApiString(), activated.Status);
            Assert.Contains(activated.Workouts, w => w.WorkoutName == "Edited Name");
        }

        [DockerRequiredFact]
        public async Task GetConversation_ReturnsRevisionReflectingCurrentDraftContent()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser();
            var (conversationId, programId, _) = await SeedDraftAsync(userId, totalWeeks: 1, daysPerWeek: 1);

            string firstSeenRevision;
            int workoutId;
            await using (var ctx = _pg.NewContext())
            {
                var response = await Chat(ctx, userId).GetConversation(conversationId);
                var ok = Assert.IsType<OkObjectResult>(response.Result);
                var body = Assert.IsType<ConversationDetailResponse>(ok.Value);
                Assert.NotNull(body.DraftRevision);
                firstSeenRevision = body.DraftRevision!;
                Assert.Equal(programId, body.DraftProgramId);
            }

            await using (var ctx = _pg.NewContext())
            {
                var program = await ctx.Programs.Include(p => p.Workouts).AsNoTracking().FirstAsync(p => p.Id == programId);
                workoutId = program.Workouts.First(w => !w.IsRestDay).Id;
            }

            await using (var ctx = _pg.NewContext())
            {
                var workout = await ctx.ProgramWorkouts.FirstAsync(w => w.Id == workoutId);
                var edited = new ProgramWorkout
                {
                    Id = workout.Id,
                    WorkoutName = "Changed",
                    WorkoutType = workout.WorkoutType,
                    ExercisesJson = ExercisesJsonFor("Overhead Press"),
                    WeekNumber = workout.WeekNumber,
                    DayNumber = workout.DayNumber,
                    OrderIndex = workout.OrderIndex,
                };
                await Programs(ctx, userId).UpdateWorkout(workoutId, edited);
            }

            await using (var ctx = _pg.NewContext())
            {
                var response = await Chat(ctx, userId).GetConversation(conversationId);
                var ok = Assert.IsType<OkObjectResult>(response.Result);
                var body = Assert.IsType<ConversationDetailResponse>(ok.Value);
                Assert.NotEqual(firstSeenRevision, body.DraftRevision);

                // Activating with THIS freshly re-fetched revision must succeed.
                var activateResponse = await Chat(ctx, userId).CreateProgramFromPlan(
                    conversationId, new CreateProgramRequest { DraftRevision = body.DraftRevision });
                Assert.IsType<OkObjectResult>(activateResponse.Result);
            }
        }

        [DockerRequiredFact]
        public async Task NeverActivated_DraftStaysInert_AndCanBeExplicitlyDeleted()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser();
            var (_, programId, _) = await SeedDraftAsync(userId);

            // Cancel/leave the preview: create-program is simply never called.
            await using (var verifyCtx = _pg.NewContext())
            {
                var stillDraft = await verifyCtx.Programs.AsNoTracking().FirstAsync(p => p.Id == programId);
                Assert.Equal(ProgramStatus.Draft.ToApiString(), stillDraft.Status);
                Assert.False(stillDraft.IsActive);
            }

            // Explicit discard reuses the existing DELETE /Programs/{id} — no new endpoint needed.
            await using var ctx = _pg.NewContext();
            var deleteResult = await Programs(ctx, userId).DeleteProgram(programId);
            Assert.IsType<NoContentResult>(deleteResult);

            await using var verifyCtx2 = _pg.NewContext();
            var deleted = await verifyCtx2.Programs.AsNoTracking().FirstAsync(p => p.Id == programId);
            Assert.Equal(ProgramStatus.Deleted.ToApiString(), deleted.Status);
        }

        [DockerRequiredFact]
        public async Task ActivateAsync_ConcurrentActivation_DifferentStartDates_LoserReportsTheActualCommittedDate()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser();
            var (_, programId, _) = await SeedDraftAsync(userId);

            // Two Mondays, far enough apart that a mix-up is unmistakable.
            var dateA = DateTime.UtcNow.Date.AddDays(14);
            var mondayA = dateA.AddDays(((int)DayOfWeek.Monday - (int)dateA.DayOfWeek + 7) % 7);
            var dateB = DateTime.UtcNow.Date.AddDays(35);
            var mondayB = dateB.AddDays(((int)DayOfWeek.Monday - (int)dateB.DayOfWeek + 7) % 7);

            await using var ctxA = _pg.NewContext();
            await using var ctxB = _pg.NewContext();

            var programA = await ctxA.Programs.Include(p => p.Workouts).FirstAsync(p => p.Id == programId);
            var programB = await ctxB.Programs.Include(p => p.Workouts).FirstAsync(p => p.Id == programId);

            var taskA = ProgramActivationService.ActivateAsync(ctxA, programA, mondayA, null, null, null);
            var taskB = ProgramActivationService.ActivateAsync(ctxB, programB, mondayB, null, null, null);
            var results = await Task.WhenAll(taskA, taskB);

            var winner = results.Single(r => r.Outcome == ProgramActivationOutcome.Activated);
            var loser = results.Single(r => r.Outcome == ProgramActivationOutcome.AlreadyActive);

            await using var verifyCtx = _pg.NewContext();
            var final = await verifyCtx.Programs.AsNoTracking().FirstAsync(p => p.Id == programId);

            // Whichever date actually won, BOTH responses must agree with the real committed
            // state - the loser must never report its OWN requested date as if it had been
            // applied when it wasn't.
            Assert.Equal(final.StartDate, winner.Program!.StartDate);
            Assert.Equal(final.StartDate, loser.Program!.StartDate);
            Assert.True(final.StartDate == mondayA || final.StartDate == mondayB);
        }

        [DockerRequiredFact]
        public async Task ActivateAsync_FailureBetweenStatusFlipAndFieldUpdates_RollsBackCompletely()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser();
            var (_, programId, _) = await SeedDraftAsync(userId);

            // Program.Title has [MaxLength(200)] -> Postgres "character varying(200)". This
            // forces a real DB-level failure INSIDE the transaction, after the raw-SQL status
            // flip has already run but before/during the EF SaveChangesAsync that persists the
            // rest of the activation fields - exactly the interleaving the atomic-transaction fix
            // exists to protect.
            var overlongTitle = new string('x', 500);

            await using (var ctx = _pg.NewContext())
            {
                var program = await ctx.Programs.Include(p => p.Workouts).FirstAsync(p => p.Id == programId);
                await Assert.ThrowsAnyAsync<Exception>(() =>
                    ProgramActivationService.ActivateAsync(ctx, program, null, overlongTitle, null, null));
            }

            // Full rollback: NOT stuck half-activated (Status flipped but fields still
            // draft-shaped) - the status flip itself must have been undone too.
            await using (var verifyCtx = _pg.NewContext())
            {
                var stillDraft = await verifyCtx.Programs.AsNoTracking().FirstAsync(p => p.Id == programId);
                Assert.Equal(ProgramStatus.Draft.ToApiString(), stillDraft.Status);
                Assert.False(stillDraft.IsActive);
            }

            // A legitimate retry afterward succeeds normally - no manual repair was needed.
            await using var retryCtx = _pg.NewContext();
            var retryProgram = await retryCtx.Programs.Include(p => p.Workouts).FirstAsync(p => p.Id == programId);
            var result = await ProgramActivationService.ActivateAsync(retryCtx, retryProgram, null, null, null, null);
            Assert.Equal(ProgramActivationOutcome.Activated, result.Outcome);
        }

        /// <summary>
        /// Proves the exact race the "transaction around the status flip alone" objection
        /// describes cannot silently activate unseen content, using real Postgres row locks as
        /// the checkpoint - never a sleep. A concurrent edit through the REAL
        /// <see cref="ProgramsController.UpdateWorkout"/> production endpoint executes its
        /// <c>UPDATE</c> (acquiring Postgres's row lock on that <c>ProgramWorkouts</c> row) but is
        /// deliberately held open, uncommitted, in its own explicit transaction. Activation is
        /// then submitted with the revision the user actually reviewed (computed BEFORE the
        /// edit). Because <see cref="ProgramActivationService.IsRevisionStillFreshUnderLockAsync"/>
        /// takes a <c>SELECT ... FOR UPDATE</c> on that same row before comparing revisions,
        /// Postgres guarantees activation's recheck cannot observe (or race past) anything other
        /// than the edit's fully-committed result - it either blocks behind the held lock until
        /// the edit commits, or (if scheduled after) simply sees the already-committed edit
        /// directly. Either way, activation's recheck always reflects the true final content, so
        /// program order alone (edit's UPDATE runs, then activation is submitted, then the edit
        /// commits, then activation's recheck resolves) - not timing - is what makes this
        /// deterministic. No <c>Task.Delay</c>/sleep is used anywhere in this test.
        /// </summary>
        [DockerRequiredFact]
        public async Task ActivateAsync_ConcurrentWorkoutEditRacesTheLockedRecheck_ActivationReportsStale_NeverActivatesUnseenContent()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser();
            var (_, programId, _) = await SeedDraftAsync(userId, totalWeeks: 1, daysPerWeek: 3);

            // The revision the user actually reviewed, before any concurrent edit exists.
            string reviewedRevision;
            ProgramWorkout firstWorkout;
            await using (var readCtx = _pg.NewContext())
            {
                var reviewed = await readCtx.Programs.Include(p => p.Workouts).AsNoTracking()
                    .FirstAsync(p => p.Id == programId);
                reviewedRevision = ProgramActivationService.ComputeContentRevision(reviewed);
                firstWorkout = reviewed.Workouts.First(w => !w.IsRestDay);
            }

            // The concurrent editor: begins its own explicit transaction, executes the REAL
            // UpdateWorkout production endpoint (which runs a real UPDATE and thus acquires a
            // Postgres row lock on this ProgramWorkouts row) - then deliberately does NOT commit
            // yet. The lock stays held until this test calls CommitAsync below.
            await using var editCtx = _pg.NewContext();
            await using var editTransaction = await editCtx.Database.BeginTransactionAsync();

            var editPayload = new ProgramWorkout
            {
                Id = firstWorkout.Id,
                WorkoutName = firstWorkout.WorkoutName,
                WorkoutType = firstWorkout.WorkoutType,
                Description = firstWorkout.Description,
                EstimatedDuration = firstWorkout.EstimatedDuration,
                ExercisesJson = ExercisesJsonFor("Deadlift", "Overhead Press"),
                WarmUp = firstWorkout.WarmUp,
                CoolDown = firstWorkout.CoolDown,
                IsCompleted = firstWorkout.IsCompleted,
                CompletedAt = firstWorkout.CompletedAt,
                CompletionNotes = firstWorkout.CompletionNotes,
                WeekNumber = firstWorkout.WeekNumber,
                DayNumber = firstWorkout.DayNumber,
                OrderIndex = firstWorkout.OrderIndex,
            };
            var editController = Programs(editCtx, userId);
            var editResponse = await editController.UpdateWorkout(firstWorkout.Id, editPayload);
            Assert.IsType<NoContentResult>(editResponse);
            // editTransaction is still open here - the UPDATE's row lock is held, not released.

            // Submit activation with the PRE-EDIT revision, through the same production
            // ActivateAsync entry point the controllers call. Its internal locked recheck can
            // only resolve once editTransaction ends (commit below) - a real DB lock, not a race.
            await using var activateCtx = _pg.NewContext();
            var programForActivation = await activateCtx.Programs.Include(p => p.Workouts)
                .FirstAsync(p => p.Id == programId);
            var activateTask = ProgramActivationService.ActivateAsync(
                activateCtx, programForActivation, null, null, null, null, clientRevision: reviewedRevision);

            // Release the lock. Whether activation's recheck was already blocked on it or had not
            // yet reached that statement, it is only from this point that it can observe (or
            // could ever have observed) the edited row - program order, not timing, is what
            // guarantees this.
            await editTransaction.CommitAsync();

            var result = await activateTask;

            // Required outcome: never silently activate content that was never actually reviewed.
            Assert.Equal(ProgramActivationOutcome.Stale, result.Outcome);

            await using var verifyCtx = _pg.NewContext();
            var finalProgram = await verifyCtx.Programs.AsNoTracking().FirstAsync(p => p.Id == programId);
            Assert.Equal(ProgramStatus.Draft.ToApiString(), finalProgram.Status);
            Assert.False(finalProgram.IsActive);

            // The concurrent edit itself was never clobbered by the aborted activation attempt.
            var finalWorkout = await verifyCtx.ProgramWorkouts.AsNoTracking()
                .FirstAsync(w => w.Id == firstWorkout.Id);
            Assert.Contains("Deadlift", finalWorkout.ExercisesJson);

            // A subsequent activation with the now-current revision succeeds normally.
            await using var retryCtx = _pg.NewContext();
            var currentProgram = await retryCtx.Programs.Include(p => p.Workouts).AsNoTracking()
                .FirstAsync(p => p.Id == programId);
            var currentRevision = ProgramActivationService.ComputeContentRevision(currentProgram);

            await using var finalCtx = _pg.NewContext();
            var trackedProgram = await finalCtx.Programs.Include(p => p.Workouts).FirstAsync(p => p.Id == programId);
            var finalResult = await ProgramActivationService.ActivateAsync(
                finalCtx, trackedProgram, null, null, null, null, clientRevision: currentRevision);
            Assert.Equal(ProgramActivationOutcome.Activated, finalResult.Outcome);
        }
    }
}
