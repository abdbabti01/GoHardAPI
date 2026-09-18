using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GoHardAPI.Converters;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Daily nutrition targets for a user
    /// </summary>
    public class NutritionGoal
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; } // e.g., "Weight Loss", "Muscle Building", "Maintenance"

        // Daily calorie target
        public decimal DailyCalories { get; set; } = 2000;

        // Macronutrient targets (in grams)
        public decimal DailyProtein { get; set; } = 150;
        public decimal DailyCarbohydrates { get; set; } = 200;
        public decimal DailyFat { get; set; } = 65;

        // Optional targets
        public decimal? DailyFiber { get; set; } = 25;
        public decimal? DailySodium { get; set; } = 2300; // mg
        public decimal? DailySugar { get; set; } // g
        public decimal? DailyWater { get; set; } = 2000; // ml

        // Macro percentage targets (alternative to grams)
        public decimal? ProteinPercentage { get; set; } // 0-100
        public decimal? CarbohydratesPercentage { get; set; }
        public decimal? FatPercentage { get; set; }

        /// <summary>
        /// Whether this is the currently active goal (i.e. the most recent,
        /// non-deleted row for this user) - a convenience flag kept in sync by
        /// <see cref="Services.NutritionTargetService"/> for callers that only
        /// care about "today's" target. Historical resolution for a specific
        /// date never reads this flag - see <see cref="EffectiveDate"/>.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// The calendar date (UTC midnight, matching <see cref="MealLog.Date"/>'s
        /// convention) from which this target applies. A date's target is
        /// resolved as the row with the greatest <see cref="EffectiveDate"/>
        /// that is `&lt;=` the queried date and not yet deleted as of that date
        /// (see <see cref="Services.NutritionTargetService.ResolveForDateAsync"/>) -
        /// never the single "active" row applied retroactively.
        /// </summary>
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;

        /// <summary>
        /// Soft-delete marker. A deleted row is excluded from resolution for
        /// any date on/after this timestamp's date, but is NOT removed - it
        /// still answers historical queries for dates before it, so deleting
        /// today's target can never erase what applied on past dates.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Explanation of how the nutrition targets were calculated
        /// </summary>
        [MaxLength(1000)]
        public string? Explanation { get; set; }

        /// <summary>
        /// BMR (Basal Metabolic Rate) used in calculation
        /// </summary>
        public decimal? Bmr { get; set; }

        /// <summary>
        /// TDEE (Total Daily Energy Expenditure) used in calculation
        /// </summary>
        public decimal? Tdee { get; set; }

        /// <summary>
        /// Daily calorie adjustment (deficit negative, surplus positive)
        /// </summary>
        public decimal? CalorieAdjustment { get; set; }

        // Navigation properties
        public User? User { get; set; }

        /// <summary>
        /// Calculate macro targets from percentages if not set directly
        /// </summary>
        public void CalculateMacrosFromPercentages()
        {
            if (ProteinPercentage.HasValue)
            {
                // Protein has 4 calories per gram
                DailyProtein = (DailyCalories * (ProteinPercentage.Value / 100)) / 4;
            }
            if (CarbohydratesPercentage.HasValue)
            {
                // Carbs have 4 calories per gram
                DailyCarbohydrates = (DailyCalories * (CarbohydratesPercentage.Value / 100)) / 4;
            }
            if (FatPercentage.HasValue)
            {
                // Fat has 9 calories per gram
                DailyFat = (DailyCalories * (FatPercentage.Value / 100)) / 9;
            }
        }
    }
}
