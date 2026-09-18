using System;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Controllers;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace GoHardAPI.Tests.Controllers
{
    /// <summary>
    /// Historical nutrition targets through <see cref="NutritionGoalsController"/>: the
    /// new date-aware for-date/for-dates endpoints, and that Update/Activate/Delete no
    /// longer mutate or erase history now that they're routed through
    /// <see cref="Services.NutritionTargetService"/>.
    /// </summary>
    public class NutritionGoalsDateAwareTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly TrainingContext _context;
        private const int UserId = 1;

        public NutritionGoalsDateAwareTests()
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

        private static NutritionGoalsController Controller(TrainingContext ctx, int userId) =>
            new(ctx)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth")),
                    },
                },
            };

        [Fact]
        public async Task GetGoalForDate_BeforeAnyTargetExists_ReturnsHasTargetFalse_NotAGuess()
        {
            var result = await Controller(_context, UserId).GetGoalForDate(new DateTime(2020, 1, 1));

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<NutritionTargetForDateResponse>(ok.Value);
            Assert.False(body.HasTarget);
            Assert.Null(body.Goal);
        }

        [Fact]
        public async Task UpdateNutritionGoal_InsertsANewRow_PastDateStillResolvesTheOldValues()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var created = await Controller(_context, UserId).CreateNutritionGoal(new NutritionGoal
            {
                DailyCalories = 1800,
                DailyProtein = 100,
                DailyCarbohydrates = 100,
                DailyFat = 50,
                IsActive = true,
                EffectiveDate = jan1,
            });
            var createdGoal = Assert.IsType<NutritionGoal>(
                Assert.IsType<CreatedAtActionResult>(created.Result).Value);

            var mar1 = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var updateResult = await Controller(_context, UserId).UpdateNutritionGoal(createdGoal.Id, new NutritionGoal
            {
                DailyCalories = 2200,
                DailyProtein = 150,
                DailyCarbohydrates = 200,
                DailyFat = 70,
                EffectiveDate = mar1,
            });
            Assert.IsType<NoContentResult>(updateResult);

            // Original row's own fields are untouched.
            var storedOriginal = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == createdGoal.Id);
            Assert.Equal(1800, storedOriginal.DailyCalories);

            // A date between the two effective dates still resolves the old value.
            var februaryResult = await Controller(_context, UserId).GetGoalForDate(new DateTime(2026, 2, 1));
            var februaryBody = Assert.IsType<NutritionTargetForDateResponse>(
                Assert.IsType<OkObjectResult>(februaryResult.Result).Value);
            Assert.True(februaryBody.HasTarget);
            Assert.Equal(1800, februaryBody.Goal!.DailyCalories);

            // On/after the new effective date, the new value applies.
            var aprilResult = await Controller(_context, UserId).GetGoalForDate(new DateTime(2026, 4, 1));
            var aprilBody = Assert.IsType<NutritionTargetForDateResponse>(
                Assert.IsType<OkObjectResult>(aprilResult.Result).Value);
            Assert.True(aprilBody.HasTarget);
            Assert.Equal(2200, aprilBody.Goal!.DailyCalories);

            // Exactly 2 rows exist - nothing was mutated away, one new row was added.
            Assert.Equal(2, await _context.NutritionGoals.CountAsync(g => g.UserId == UserId));
        }

        [Fact]
        public async Task DeleteNutritionGoal_IsASoftDelete_RowStillExistsForHistory()
        {
            var created = await Controller(_context, UserId).CreateNutritionGoal(new NutritionGoal
            {
                DailyCalories = 1800,
                IsActive = true,
                EffectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
            var createdGoal = Assert.IsType<NutritionGoal>(
                Assert.IsType<CreatedAtActionResult>(created.Result).Value);

            var deleteResult = await Controller(_context, UserId).DeleteNutritionGoal(createdGoal.Id);

            Assert.IsType<NoContentResult>(deleteResult);
            // Row still present (soft delete), just flagged.
            var stored = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == createdGoal.Id);
            Assert.NotNull(stored.DeletedAt);

            // A date before the deletion still resolves it.
            var pastResult = await Controller(_context, UserId).GetGoalForDate(new DateTime(2026, 1, 1));
            var pastBody = Assert.IsType<NutritionTargetForDateResponse>(
                Assert.IsType<OkObjectResult>(pastResult.Result).Value);
            Assert.True(pastBody.HasTarget);
        }

        [Fact]
        public async Task DeleteNutritionGoal_UnknownId_ReturnsNotFound()
        {
            var result = await Controller(_context, UserId).DeleteNutritionGoal(9999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ActivateGoal_CopiesValuesIntoANewRow_OriginalHistoricalRowUntouched()
        {
            var jan1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var created = await Controller(_context, UserId).CreateNutritionGoal(new NutritionGoal
            {
                DailyCalories = 1600,
                IsActive = false,
                EffectiveDate = jan1,
            });
            var preset = Assert.IsType<NutritionGoal>(
                Assert.IsType<CreatedAtActionResult>(created.Result).Value);

            var activateResult = await Controller(_context, UserId).ActivateGoal(preset.Id);
            Assert.IsType<NoContentResult>(activateResult);

            // The preset row itself is untouched (still its original EffectiveDate).
            var storedPreset = await _context.NutritionGoals.AsNoTracking().FirstAsync(g => g.Id == preset.Id);
            Assert.Equal(jan1, storedPreset.EffectiveDate);

            // A new row now carries the active flag, effective today.
            Assert.Equal(2, await _context.NutritionGoals.CountAsync(g => g.UserId == UserId));
            var activeCount = await _context.NutritionGoals.CountAsync(g => g.UserId == UserId && g.IsActive);
            Assert.Equal(1, activeCount);
        }

        [Fact]
        public async Task GetGoalsForDateRange_EndBeforeStart_IsBadRequest()
        {
            var result = await Controller(_context, UserId).GetGoalsForDateRange(
                new DateTime(2026, 2, 1), new DateTime(2026, 1, 1));

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }
}
