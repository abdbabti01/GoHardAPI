using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GoHardAPI.Converters;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Daily nutrition progress tracking (planned and consumed values)
    /// One record per user per day
    /// </summary>
    public class NutritionProgress
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// The date this progress is for
        /// </summary>
        [Required]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime Date { get; set; }

        /// <summary>
        /// Reference to the active nutrition goal (for targets)
        /// </summary>
        public int? NutritionGoalId { get; set; }

        // ============ Planned Values (from applied meals) ============

        /// <summary>
        /// Total planned calories from applied meals
        /// </summary>
        public decimal PlannedCalories { get; set; } = 0;

        /// <summary>
        /// Total planned protein from applied meals (grams)
        /// </summary>
        public decimal PlannedProtein { get; set; } = 0;

        /// <summary>
        /// Total planned carbohydrates from applied meals (grams)
        /// </summary>
        public decimal PlannedCarbohydrates { get; set; } = 0;

        /// <summary>
        /// Total planned fat from applied meals (grams)
        /// </summary>
        public decimal PlannedFat { get; set; } = 0;

        /// <summary>
        /// Total planned fiber from applied meals (grams)
        /// </summary>
        public decimal PlannedFiber { get; set; } = 0;

        /// <summary>
        /// Total planned water intake (ml)
        /// </summary>
        public decimal PlannedWater { get; set; } = 0;

        // ============ Consumed Values (marked as eaten) ============

        /// <summary>
        /// Total consumed calories (meals marked as eaten)
        /// </summary>
        public decimal ConsumedCalories { get; set; } = 0;

        /// <summary>
        /// Total consumed protein (grams)
        /// </summary>
        public decimal ConsumedProtein { get; set; } = 0;

        /// <summary>
        /// Total consumed carbohydrates (grams)
        /// </summary>
        public decimal ConsumedCarbohydrates { get; set; } = 0;

        /// <summary>
        /// Total consumed fat (grams)
        /// </summary>
        public decimal ConsumedFat { get; set; } = 0;

        /// <summary>
        /// Total consumed fiber (grams)
        /// </summary>
        public decimal ConsumedFiber { get; set; } = 0;

        /// <summary>
        /// Total consumed water intake (ml)
        /// </summary>
        public decimal ConsumedWater { get; set; } = 0;

        // ============ Metadata ============

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // ============ Navigation Properties ============

        public User? User { get; set; }
        public NutritionGoal? NutritionGoal { get; set; }

        // ============ Helper Methods ============

        /// <summary>
        /// Add planned values from a meal
        /// </summary>
        public void AddPlanned(decimal calories, decimal protein, decimal carbs, decimal fat, decimal fiber = 0)
        {
            PlannedCalories += calories;
            PlannedProtein += protein;
            PlannedCarbohydrates += carbs;
            PlannedFat += fat;
            PlannedFiber += fiber;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Add consumed values when meal is marked as eaten
        /// </summary>
        public void AddConsumed(decimal calories, decimal protein, decimal carbs, decimal fat, decimal fiber = 0)
        {
            ConsumedCalories += calories;
            ConsumedProtein += protein;
            ConsumedCarbohydrates += carbs;
            ConsumedFat += fat;
            ConsumedFiber += fiber;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Add water intake
        /// </summary>
        public void AddWater(decimal ml, bool isConsumed = true)
        {
            if (isConsumed)
                ConsumedWater += ml;
            else
                PlannedWater += ml;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
