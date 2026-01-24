using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// A meal plan template that can be applied to future dates
    /// </summary>
    public class MealPlan
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Number of days this plan spans (e.g., 7 for a weekly plan)
        /// </summary>
        public int DurationDays { get; set; } = 7;

        /// <summary>
        /// Whether this plan is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        // Calculated averages for the plan
        public decimal? AverageDailyCalories { get; set; }
        public decimal? AverageDailyProtein { get; set; }
        public decimal? AverageDailyCarbohydrates { get; set; }
        public decimal? AverageDailyFat { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public ICollection<MealPlanDay> Days { get; set; } = new List<MealPlanDay>();

        /// <summary>
        /// Recalculate average daily nutritional values
        /// </summary>
        public void RecalculateAverages()
        {
            if (Days == null || !Days.Any()) return;

            AverageDailyCalories = Days.Average(d => d.TotalCalories);
            AverageDailyProtein = Days.Average(d => d.TotalProtein);
            AverageDailyCarbohydrates = Days.Average(d => d.TotalCarbohydrates);
            AverageDailyFat = Days.Average(d => d.TotalFat);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// A single day within a meal plan
    /// </summary>
    public class MealPlanDay
    {
        public int Id { get; set; }

        [Required]
        public int MealPlanId { get; set; }

        /// <summary>
        /// Day number within the plan (1-based)
        /// </summary>
        [Required]
        public int DayNumber { get; set; }

        /// <summary>
        /// Optional name for the day (e.g., "Monday", "Rest Day")
        /// </summary>
        [MaxLength(50)]
        public string? Name { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Calculated totals for the day
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbohydrates { get; set; }
        public decimal TotalFat { get; set; }

        // Navigation properties
        public MealPlan? MealPlan { get; set; }
        public ICollection<MealPlanMeal> Meals { get; set; } = new List<MealPlanMeal>();

        /// <summary>
        /// Recalculate totals from all meals
        /// </summary>
        public void RecalculateTotals()
        {
            if (Meals == null) return;

            TotalCalories = Meals.Sum(m => m.TotalCalories);
            TotalProtein = Meals.Sum(m => m.TotalProtein);
            TotalCarbohydrates = Meals.Sum(m => m.TotalCarbohydrates);
            TotalFat = Meals.Sum(m => m.TotalFat);
        }
    }

    /// <summary>
    /// A meal within a meal plan day
    /// </summary>
    public class MealPlanMeal
    {
        public int Id { get; set; }

        [Required]
        public int MealPlanDayId { get; set; }

        /// <summary>
        /// Type of meal: Breakfast, Lunch, Dinner, Snack
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string MealType { get; set; } = "Snack";

        [MaxLength(100)]
        public string? Name { get; set; }

        /// <summary>
        /// Suggested time for this meal
        /// </summary>
        public TimeSpan? ScheduledTime { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Calculated totals
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbohydrates { get; set; }
        public decimal TotalFat { get; set; }

        // Navigation properties
        public MealPlanDay? MealPlanDay { get; set; }
        public ICollection<MealPlanFoodItem> FoodItems { get; set; } = new List<MealPlanFoodItem>();

        /// <summary>
        /// Recalculate totals from all food items
        /// </summary>
        public void RecalculateTotals()
        {
            if (FoodItems == null) return;

            TotalCalories = FoodItems.Sum(f => f.Calories);
            TotalProtein = FoodItems.Sum(f => f.Protein);
            TotalCarbohydrates = FoodItems.Sum(f => f.Carbohydrates);
            TotalFat = FoodItems.Sum(f => f.Fat);
        }
    }

    /// <summary>
    /// A food item within a meal plan meal
    /// </summary>
    public class MealPlanFoodItem
    {
        public int Id { get; set; }

        [Required]
        public int MealPlanMealId { get; set; }

        /// <summary>
        /// Reference to a food template
        /// </summary>
        public int? FoodTemplateId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public decimal Quantity { get; set; } = 1;
        public decimal ServingSize { get; set; } = 100;

        [MaxLength(20)]
        public string ServingUnit { get; set; } = "g";

        // Nutritional values (calculated based on quantity)
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Fat { get; set; }

        // Navigation properties
        public MealPlanMeal? MealPlanMeal { get; set; }
        public FoodTemplate? FoodTemplate { get; set; }
    }
}
