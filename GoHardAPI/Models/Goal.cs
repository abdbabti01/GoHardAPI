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

        // Calculated properties

        /// <summary>
        /// Determine if this is a decrease goal (lower is better) or increase goal (higher is better)
        /// </summary>
        public bool IsDecreaseGoal
        {
            get
            {
                var type = GoalType?.ToLower() ?? string.Empty;
                // Weight loss and body fat are decrease goals
                // Muscle gain is an INCREASE goal
                return (type.Contains("weight") && type.Contains("loss")) ||
                       type.Contains("bodyfat") ||
                       type.Contains("body fat") ||
                       type.Contains("fat");
            }
        }

        /// <summary>
        /// Get the starting value (CurrentValue represents starting weight/value)
        /// </summary>
        public decimal StartValue => CurrentValue;

        /// <summary>
        /// Calculate total cumulative progress from all progress history entries
        /// For weight loss: sum of all pounds lost
        /// For increase goals: sum of all progress made
        /// </summary>
        public decimal TotalProgress
        {
            get
            {
                if (ProgressHistory == null || !ProgressHistory.Any())
                {
                    return 0;
                }
                // Sum all progress values (each entry is a delta/increment)
                return ProgressHistory.Sum(p => p.Value);
            }
        }

        /// <summary>
        /// Calculate progress percentage correctly for both increase and decrease goals
        /// Progress entries represent incremental changes (e.g., 2 lbs lost, 3 workouts completed)
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                if (IsDecreaseGoal)
                {
                    // For weight loss: total progress / goal amount * 100
                    // Example: Start 200, Target 150, Progress 12 = 12/50 = 24%
                    var goalAmount = CurrentValue - TargetValue; // 200 - 150 = 50
                    if (goalAmount <= 0) return 0;

                    return Math.Clamp((double)(TotalProgress / goalAmount * 100), 0, 100);
                }
                else
                {
                    // For increase goals: total progress / target * 100
                    // Example: Target 20 workouts, Progress 5 = 5/20 = 25%
                    if (TargetValue <= 0) return 0;
                    return Math.Clamp((double)(TotalProgress / TargetValue * 100), 0, 100);
                }
            }
        }

        /// <summary>
        /// Get progress description (e.g., "Lost 12.0 / 50.0 lb" or "Gained 5.0 / 20.0 lb")
        /// </summary>
        public string GetProgressDescription()
        {
            if (IsDecreaseGoal)
            {
                var goalAmount = CurrentValue - TargetValue; // Total to lose
                return $"Lost {TotalProgress:F1} / {goalAmount:F1} {Unit ?? string.Empty}".Trim();
            }
            else
            {
                var type = GoalType?.ToLower() ?? string.Empty;
                // Check if it's a weight/muscle gain goal to show "Gained"
                if (type.Contains("weight") || type.Contains("muscle"))
                {
                    var goalAmount = TargetValue - CurrentValue; // Total to gain
                    return $"Gained {TotalProgress:F1} / {goalAmount:F1} {Unit ?? string.Empty}".Trim();
                }
                else
                {
                    // For other goals (workouts, volume, etc.)
                    return $"{TotalProgress:F1} / {TargetValue:F1} {Unit ?? string.Empty}".Trim();
                }
            }
        }
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
