using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Daily container for meal entries and nutrition tracking
    /// </summary>
    public class MealLog
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// The date this meal log represents (stored as UTC midnight)
        /// </summary>
        [Required]
        public DateTime Date { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Water tracking (in ml)
        public decimal WaterIntake { get; set; } = 0;

        // Calculated totals (updated when meal entries change)
        public decimal TotalCalories { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalCarbohydrates { get; set; }
        public decimal TotalFat { get; set; }
        public decimal? TotalFiber { get; set; }
        public decimal? TotalSodium { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public ICollection<MealEntry> MealEntries { get; set; } = new List<MealEntry>();

        /// <summary>
        /// Recalculate totals from meal entries
        /// </summary>
        /// <param name="consumedOnly">If true, only include consumed meals in totals</param>
        public void RecalculateTotals(bool consumedOnly = true)
        {
            var entries = consumedOnly
                ? MealEntries.Where(e => e.IsConsumed).ToList()
                : MealEntries.ToList();

            TotalCalories = entries.Sum(e => e.TotalCalories);
            TotalProtein = entries.Sum(e => e.TotalProtein);
            TotalCarbohydrates = entries.Sum(e => e.TotalCarbohydrates);
            TotalFat = entries.Sum(e => e.TotalFat);
            TotalFiber = entries.Sum(e => e.TotalFiber ?? 0);
            TotalSodium = entries.Sum(e => e.TotalSodium ?? 0);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Get planned totals (all meals regardless of consumption status)
        /// </summary>
        public (decimal Calories, decimal Protein, decimal Carbohydrates, decimal Fat, decimal Fiber, decimal Sodium) GetPlannedTotals()
        {
            return (
                MealEntries.Sum(e => e.TotalCalories),
                MealEntries.Sum(e => e.TotalProtein),
                MealEntries.Sum(e => e.TotalCarbohydrates),
                MealEntries.Sum(e => e.TotalFat),
                MealEntries.Sum(e => e.TotalFiber ?? 0),
                MealEntries.Sum(e => e.TotalSodium ?? 0)
            );
        }

        /// <summary>
        /// Get consumed totals (only consumed meals)
        /// </summary>
        public (decimal Calories, decimal Protein, decimal Carbohydrates, decimal Fat, decimal Fiber, decimal Sodium) GetConsumedTotals()
        {
            var consumed = MealEntries.Where(e => e.IsConsumed).ToList();
            return (
                consumed.Sum(e => e.TotalCalories),
                consumed.Sum(e => e.TotalProtein),
                consumed.Sum(e => e.TotalCarbohydrates),
                consumed.Sum(e => e.TotalFat),
                consumed.Sum(e => e.TotalFiber ?? 0),
                consumed.Sum(e => e.TotalSodium ?? 0)
            );
        }
    }
}
