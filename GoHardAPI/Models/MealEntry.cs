using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Represents a meal within a day (breakfast, lunch, dinner, snack)
    /// </summary>
    public class MealEntry
    {
        public int Id { get; set; }

        [Required]
        public int MealLogId { get; set; }

        /// <summary>
        /// Type of meal: Breakfast, Lunch, Dinner, Snack
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string MealType { get; set; } = "Snack";

        [MaxLength(100)]
        public string? Name { get; set; } // Optional custom name for the meal

        /// <summary>
        /// When this meal was scheduled/planned for
        /// </summary>
        public DateTime? ScheduledTime { get; set; }

        /// <summary>
        /// Whether this meal was actually consumed
        /// </summary>
        public bool IsConsumed { get; set; } = false;

        /// <summary>
        /// When the meal was actually consumed
        /// </summary>
        public DateTime? ConsumedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Calculated totals from food items
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbohydrates { get; set; }
        public decimal TotalFat { get; set; }
        public decimal? TotalFiber { get; set; }
        public decimal? TotalSodium { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public MealLog? MealLog { get; set; }
        public ICollection<FoodItem> FoodItems { get; set; } = new List<FoodItem>();

        /// <summary>
        /// Recalculate totals from all food items
        /// </summary>
        public void RecalculateTotals()
        {
            TotalCalories = FoodItems.Sum(f => f.Calories);
            TotalProtein = FoodItems.Sum(f => f.Protein);
            TotalCarbohydrates = FoodItems.Sum(f => f.Carbohydrates);
            TotalFat = FoodItems.Sum(f => f.Fat);
            TotalFiber = FoodItems.Sum(f => f.Fiber ?? 0);
            TotalSodium = FoodItems.Sum(f => f.Sodium ?? 0);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Enum-like constants for meal types
    /// </summary>
    public static class MealTypes
    {
        public const string Breakfast = "Breakfast";
        public const string Lunch = "Lunch";
        public const string Dinner = "Dinner";
        public const string Snack = "Snack";

        public static readonly string[] All = { Breakfast, Lunch, Dinner, Snack };
    }
}
