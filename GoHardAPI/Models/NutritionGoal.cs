using System.ComponentModel.DataAnnotations;

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
        /// Whether this is the currently active goal
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

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
