using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Real-PostgreSQL evidence for the Phase 2 nutrition-consent contract: goal creation never
    /// writes <see cref="NutritionGoal"/> rows (the coupling that used to auto-overwrite targets
    /// lived client-side, in <c>SmartGoalDialog</c>, not in <see cref="GoalsController"/>);
    /// <c>/Nutrition/calculate</c> is a pure preview that never persists; <c>/calculate-and-save</c>
    /// is the only endpoint that applies reviewed values, and it correctly deactivates exactly
    /// the prior goal; and a failed calculate-and-save (missing metrics) leaves the previous
    /// active goal completely untouched — no partial/duplicate state.
    /// </summary>
    [Trait("Category", "PostgresIntegration")]
    [Collection(HistoryPreservationPostgresCollection.Name)]
    public sealed class GoalNutritionConsentTests
    {
        private readonly HistoryPreservationPostgresFixture _pg;

        public GoalNutritionConsentTests(HistoryPreservationPostgresFixture pg) => _pg = pg;

        private static GoalsController Goals(TrainingContext ctx, int userId)
        {
            var controller = new GoalsController(ctx);
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

        private static NutritionController Nutrition(TrainingContext ctx, int userId)
        {
            var controller = new NutritionController(
                ctx,
                new NutritionCalculatorService(),
                new CurrentMeasurementsService(ctx),
                NullLogger<NutritionController>.Instance);
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

        private async Task<int> SeedUser(bool withMetrics)
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
                Gender = "Male",
                DateOfBirth = new DateTime(1990, 1, 1),
            };
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();

            if (withMetrics)
            {
                ctx.BodyMetrics.Add(new BodyMetric
                {
                    UserId = u.Id,
                    RecordedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    Weight = 80,
                    Height = 180,
                    ActivityLevel = "ModeratelyActive",
                });
                await ctx.SaveChangesAsync();
            }

            return u.Id;
        }

        [DockerRequiredFact]
        public async Task CreateGoal_NonNutritionGoalType_NoBodyMetrics_Succeeds_NeverWritesNutritionGoals()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            // "Workout Frequency" needs no body metrics/activity level — goal creation must not
            // require or touch them regardless.
            var userId = await SeedUser(withMetrics: false);

            await using var ctx = _pg.NewContext();
            var goal = new Goal
            {
                GoalType = "Workout Frequency",
                TargetValue = 4,
                CurrentValue = 0,
                Unit = "workouts",
            };

            var response = await Goals(ctx, userId).CreateGoal(goal);
            Assert.IsType<CreatedAtActionResult>(response.Result);

            await using var verifyCtx = _pg.NewContext();
            Assert.Equal(1, await verifyCtx.Goals.CountAsync(g => g.UserId == userId));
            Assert.Equal(0, await verifyCtx.NutritionGoals.CountAsync(n => n.UserId == userId));
        }

        [DockerRequiredFact]
        public async Task Calculate_IsPreviewOnly_NeverPersists()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser(withMetrics: true);

            await using (var ctx = _pg.NewContext())
            {
                var preview = await Nutrition(ctx, userId).CalculateNutrition(
                    new CalculateNutritionRequest { GoalType = "WeightLoss" });
                Assert.IsType<OkObjectResult>(preview.Result);
            }

            await using var verifyCtx = _pg.NewContext();
            Assert.Equal(0, await verifyCtx.NutritionGoals.CountAsync(n => n.UserId == userId));
        }

        [DockerRequiredFact]
        public async Task CalculateAndSave_Confirmed_PersistsTheSameValuesJustPreviewed()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser(withMetrics: true);
            var request = new CalculateNutritionRequest { GoalType = "WeightLoss", TargetWeightChange = 10, TimeframeWeeks = 8 };

            CalculateNutritionResponse preview;
            await using (var ctx = _pg.NewContext())
            {
                var previewResult = await Nutrition(ctx, userId).CalculateNutrition(request);
                var ok = Assert.IsType<OkObjectResult>(previewResult.Result);
                preview = Assert.IsType<CalculateNutritionResponse>(ok.Value);
            }

            CalculateNutritionResponse saved;
            await using (var ctx = _pg.NewContext())
            {
                var saveResult = await Nutrition(ctx, userId).CalculateAndSaveNutrition(request);
                var ok = Assert.IsType<OkObjectResult>(saveResult.Result);
                saved = Assert.IsType<CalculateNutritionResponse>(ok.Value);
            }

            Assert.Equal(preview.DailyCalories, saved.DailyCalories);

            await using var verifyCtx = _pg.NewContext();
            var active = await verifyCtx.NutritionGoals.AsNoTracking()
                .SingleAsync(n => n.UserId == userId && n.IsActive);
            Assert.Equal(saved.DailyCalories, active.DailyCalories);
            Assert.Equal(saved.NutritionGoalId, active.Id);
        }

        [DockerRequiredFact]
        public async Task CalculateAndSave_Confirmed_DeactivatesExactlyThePriorGoal()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser(withMetrics: true);

            int firstGoalId;
            await using (var ctx = _pg.NewContext())
            {
                var first = await Nutrition(ctx, userId).CalculateAndSaveNutrition(
                    new CalculateNutritionRequest { GoalType = "Maintenance" });
                var ok = Assert.IsType<OkObjectResult>(first.Result);
                firstGoalId = Assert.IsType<CalculateNutritionResponse>(ok.Value).NutritionGoalId!.Value;
            }

            await using (var ctx = _pg.NewContext())
            {
                await Nutrition(ctx, userId).CalculateAndSaveNutrition(
                    new CalculateNutritionRequest { GoalType = "WeightLoss", TargetWeightChange = 5, TimeframeWeeks = 6 });
            }

            await using var verifyCtx = _pg.NewContext();
            var goals = await verifyCtx.NutritionGoals.AsNoTracking().Where(n => n.UserId == userId).ToListAsync();
            Assert.Equal(2, goals.Count);
            Assert.Single(goals, g => g.IsActive);
            Assert.False(goals.Single(g => g.Id == firstGoalId).IsActive);
        }

        [DockerRequiredFact]
        public async Task CalculateAndSave_MissingMetrics_Fails_PriorActiveGoalUntouched_NoPartialState()
        {
            Assert.True(_pg.Available, "PostgreSQL container was not available.");

            var userId = await SeedUser(withMetrics: true);

            int firstGoalId;
            await using (var ctx = _pg.NewContext())
            {
                var first = await Nutrition(ctx, userId).CalculateAndSaveNutrition(
                    new CalculateNutritionRequest { GoalType = "Maintenance" });
                var ok = Assert.IsType<OkObjectResult>(first.Result);
                firstGoalId = Assert.IsType<CalculateNutritionResponse>(ok.Value).NutritionGoalId!.Value;
            }

            // Remove the metrics a follow-up calculation would need (skip/failed nutrition setup
            // must never touch what's already active).
            await using (var ctx = _pg.NewContext())
            {
                var metrics = await ctx.BodyMetrics.Where(b => b.UserId == userId).ToListAsync();
                ctx.BodyMetrics.RemoveRange(metrics);
                await ctx.SaveChangesAsync();
            }

            await using (var ctx = _pg.NewContext())
            {
                var failed = await Nutrition(ctx, userId).CalculateAndSaveNutrition(
                    new CalculateNutritionRequest { GoalType = "WeightLoss" });
                Assert.IsType<BadRequestObjectResult>(failed.Result);
            }

            await using var verifyCtx = _pg.NewContext();
            var goals = await verifyCtx.NutritionGoals.AsNoTracking().Where(n => n.UserId == userId).ToListAsync();
            Assert.Single(goals); // the failed attempt created nothing
            Assert.True(goals.Single(g => g.Id == firstGoalId).IsActive); // untouched
        }
    }
}
