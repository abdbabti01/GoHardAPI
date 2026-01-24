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
        /// Recalculate totals from all meal entries
        /// </summary>
        public void RecalculateTotals()
        {
            TotalCalories = MealEntries.Sum(e => e.TotalCalories);
            TotalProtein = MealEntries.Sum(e => e.TotalProtein);
            TotalCarbohydrates = MealEntries.Sum(e => e.TotalCarbohydrates);
            TotalFat = MealEntries.Sum(e => e.TotalFat);
            TotalFiber = MealEntries.Sum(e => e.TotalFiber ?? 0);
            TotalSodium = MealEntries.Sum(e => e.TotalSodium ?? 0);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
