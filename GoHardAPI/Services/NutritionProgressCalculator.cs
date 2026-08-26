using GoHardAPI.Data;
using GoHardAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GoHardAPI.Services
{
    /// <summary>
    /// Single authoritative source for a user's daily planned/consumed nutrition totals.
    /// Derives values live from MealLog/MealEntry data on every call instead of trusting
    /// the persisted NutritionProgress aggregate, which does not stay in sync with meal
    /// edits (see nutrition audit blocking finding #1). All dashboard/progress endpoints
    /// must call this so they cannot implement diverging formulas.
    /// </summary>
    public static class NutritionProgressCalculator
    {
        /// <summary>
        /// consumed = sum of MealEntry totals where IsConsumed == true
        /// planned  = sum of all MealEntry totals, whether consumed or not
        /// </summary>
        public static async Task<NutritionProgressDto> CalculateAsync(
            TrainingContext context,
            int userId,
            DateTime date,
            int? nutritionGoalId,
            CancellationToken cancellationToken = default)
        {
            var targetDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

            var mealLog = await context.MealLogs
                .AsNoTracking()
                .Include(ml => ml.MealEntries)
                .FirstOrDefaultAsync(ml => ml.UserId == userId && ml.Date == targetDate, cancellationToken);

            var dto = new NutritionProgressDto
            {
                Id = 0,
                UserId = userId,
                Date = targetDate,
                NutritionGoalId = nutritionGoalId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            if (mealLog == null)
            {
                return dto;
            }

            var planned = mealLog.GetPlannedTotals();
            var consumed = mealLog.GetConsumedTotals();

            dto.PlannedCalories = planned.Calories;
            dto.PlannedProtein = planned.Protein;
            dto.PlannedCarbohydrates = planned.Carbohydrates;
            dto.PlannedFat = planned.Fat;
            dto.PlannedFiber = planned.Fiber;

            dto.ConsumedCalories = consumed.Calories;
            dto.ConsumedProtein = consumed.Protein;
            dto.ConsumedCarbohydrates = consumed.Carbohydrates;
            dto.ConsumedFat = consumed.Fat;
            dto.ConsumedFiber = consumed.Fiber;

            // MealLog only tracks a single WaterIntake value (no planned/unconsumed split
            // exists in the data model), so all logged water counts as consumed, matching
            // the sibling GetProgress endpoint's existing semantics (mealLog.WaterIntake).
            dto.ConsumedWater = mealLog.WaterIntake;

            return dto;
        }
    }
}
