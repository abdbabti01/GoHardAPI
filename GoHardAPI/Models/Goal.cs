using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Represents a user's fitness goal with progress tracking
    /// </summary>
    public class Goal
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Type of goal: WorkoutFrequency, Weight, Volume, Exercise, BodyFat
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string GoalType { get; set; } = string.Empty;

        /// <summary>
        /// Target value to achieve
        /// </summary>
        [Required]
        public decimal TargetValue { get; set; }

        /// <summary>
        /// Current progress value
        /// </summary>
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// Unit of measurement: workouts, kg, lbs, volume, percentage
        /// </summary>
        [MaxLength(20)]
        public string? Unit { get; set; }

        /// <summary>
        /// Time frame: weekly, monthly, total
        /// </summary>
        [MaxLength(20)]
        public string? TimeFrame { get; set; }

        /// <summary>
        /// Goal start date
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Target completion date (optional)
        /// </summary>
        public DateTime? TargetDate { get; set; }

        /// <summary>
        /// Whether the goal is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether the goal has been completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// When the goal was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// When the goal was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
        public ICollection<GoalProgress> ProgressHistory { get; set; } = new List<GoalProgress>();

        // Calculated property
        public double ProgressPercentage =>
            TargetValue > 0 ? (double)(CurrentValue / TargetValue * 100) : 0;
    }

    /// <summary>
    /// Tracks progress history for a goal
    /// </summary>
    public class GoalProgress
    {
        public int Id { get; set; }

        [Required]
        public int GoalId { get; set; }

        /// <summary>
        /// When this progress was recorded
        /// </summary>
        [Required]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Progress value at this point in time
        /// </summary>
        [Required]
        public decimal Value { get; set; }

        /// <summary>
        /// Optional notes about this progress entry
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public Goal? Goal { get; set; }
    }
}
