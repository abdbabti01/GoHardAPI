using System;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// <c>NutritionController</c> resolves weight/height through the SAME
    /// <see cref="CurrentMeasurementsService"/> as the profile, so TDEE inputs
    /// agree with <c>GET /profile</c>, and an unavailable measurement becomes a
    /// specific <c>MISSING_*</c> 400 - never a stale/deleted value, never 0.
    /// </summary>
    public sealed class NutritionMeasurementConsistencyTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public NutritionMeasurementConsistencyTests()
        {
            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();
            using var ctx = NewContext();
            ctx.Database.EnsureCreated();
        }

        public void Dispose() => _conn.Dispose();

        private TrainingContext NewContext() =>
            new(new DbContextOptionsBuilder<TrainingContext>().UseSqlite(_conn).Options);

        private async Task SeedUser(double? legacyWeight = null, double? legacyHeight = null)
        {
            await using var ctx = NewContext();
            ctx.Users.Add(new User
            {
                Id = 1,
                Name = "U",
                Username = "u1",
                Email = "u1@x.com",
                PasswordHash = "h",
                DateCreated = DateTime.UtcNow,
                UnitPreference = "Metric",
                Gender = "Male",
                DateOfBirth = new DateTime(1990, 1, 1),
                Weight = legacyWeight,
                Height = legacyHeight,
            });
            await ctx.SaveChangesAsync();
        }

        private static T WithUser<T>(T controller, int userId) where T : ControllerBase
        {
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

        private BodyMetricsController BodyMetrics(TrainingContext ctx) =>
            WithUser(new BodyMetricsController(ctx), 1);

        private NutritionController Nutrition(TrainingContext ctx) =>
            WithUser(new NutritionController(
                ctx, new NutritionCalculatorService(), new CurrentMeasurementsService(ctx),
                NullLogger<NutritionController>.Instance), 1);

        private ProfileController Profile(TrainingContext ctx) =>
            WithUser(new ProfileController(
                ctx, TestProfilePhotoStorage.UnusedService(), new GoHardAPI.Repositories.UserRepository(ctx),
                new CurrentMeasurementsService(ctx), TestScopeFactory.Unused()), 1);

        private async Task Create(DateTime recordedAt, decimal? weight = null, decimal? height = null)
        {
            await using var ctx = NewContext();
            await BodyMetrics(ctx).CreateBodyMetric(new BodyMetric
            {
                RecordedAt = recordedAt,
                Weight = weight,
                Height = height,
            });
        }

        [Fact]
        public async Task nutrition_and_profile_agree_on_the_derived_weight_and_height()
        {
            await SeedUser();
            var sameDay = new DateTime(2026, 4, 4, 0, 0, 0, DateTimeKind.Utc);
            await Create(sameDay, weight: 88m, height: 179m);
            await Create(sameDay, weight: 90m); // newer Id -> the current weight

            await using var ctx = NewContext();

            var profile = Assert.IsType<GoHardAPI.DTOs.ProfileResponse>(
                Assert.IsType<OkObjectResult>((await Profile(ctx).GetProfile()).Result).Value);

            var nut = Assert.IsType<CalculateNutritionResponse>(
                Assert.IsType<OkObjectResult>((await Nutrition(ctx)
                    .CalculateNutrition(new CalculateNutritionRequest { GoalType = "Maintenance" })).Result).Value);

            Assert.Equal(90, profile.Weight);
            Assert.Equal(179, profile.Height);
            Assert.Equal(90m, nut.UserMetrics!.WeightKg);
            Assert.Equal(179m, nut.UserMetrics.HeightCm);
        }

        [Fact]
        public async Task deleting_the_last_weight_row_makes_nutrition_return_MISSING_WEIGHT_not_a_stale_value()
        {
            await SeedUser(legacyWeight: 100); // pre-change legacy
            int rowId;
            await using (var ctx = NewContext())
            {
                var r = await BodyMetrics(ctx).CreateBodyMetric(new BodyMetric
                {
                    RecordedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    Weight = 84m,
                    Height = 180m,
                });
                rowId = ((BodyMetric)Assert.IsType<CreatedAtActionResult>(r.Result).Value!).Id;
            }

            await using (var ctx = NewContext())
                Assert.IsType<NoContentResult>(await BodyMetrics(ctx).DeleteBodyMetric(rowId));

            await using var verify = NewContext();
            var result = (await Nutrition(verify)
                .CalculateNutrition(new CalculateNutritionRequest { GoalType = "Maintenance" })).Result;

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("MISSING_WEIGHT", System.Text.Json.JsonSerializer.Serialize(bad.Value));
        }

        [Fact]
        public async Task a_zero_weight_row_is_not_used_by_nutrition()
        {
            await SeedUser();
            await Create(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), weight: 77m, height: 178m);
            await Create(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), weight: 0m); // bad entry

            await using var ctx = NewContext();
            var nut = Assert.IsType<CalculateNutritionResponse>(
                Assert.IsType<OkObjectResult>((await Nutrition(ctx)
                    .CalculateNutrition(new CalculateNutritionRequest { GoalType = "Maintenance" })).Result).Value);

            Assert.Equal(77m, nut.UserMetrics!.WeightKg); // the older real row, not 0
        }
    }
}
