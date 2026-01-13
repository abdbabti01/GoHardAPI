using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Represents a structured training program (e.g., 12-week periodization plan)
    /// </summary>
    public class Program
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Program title (e.g., "Weight Loss Program", "Muscle Gain 12-Week Plan")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the program
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Related goal ID (optional link to Goals table)
        /// </summary>
        public int? GoalId { get; set; }

        /// <summary>
        /// Total number of weeks in the program (default: 12)
        /// </summary>
        [Required]
        public int TotalWeeks { get; set; } = 12;

        /// <summary>
        /// Current week user is on (1-indexed)
        /// </summary>
        [Required]
        public int CurrentWeek { get; set; } = 1;

        /// <summary>
        /// Current day of the week (1-7, where 1=Monday)
        /// </summary>
        [Required]
        public int CurrentDay { get; set; } = 1;

        /// <summary>
        /// When the program started
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Expected end date (calculated: StartDate + TotalWeeks * 7 days)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Whether the program is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether the program has been completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// When the program was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// When the program was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Program structure/phases (e.g., "Phase 1: Foundation, Phase 2: Growth, Phase 3: Peak")
        /// Stored as JSON
        /// </summary>
        [MaxLength(2000)]
        public string? ProgramStructure { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Goal? Goal { get; set; }
        public ICollection<ProgramWorkout> Workouts { get; set; } = new List<ProgramWorkout>();

        // Calculated properties

        /// <summary>
        /// Calculate progress percentage through the program
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                var totalDays = TotalWeeks * 7;
                var completedDays = ((CurrentWeek - 1) * 7) + CurrentDay;
                return Math.Clamp((double)completedDays / totalDays * 100, 0, 100);
            }
        }

        /// <summary>
        /// Get the current phase based on week (assumes 3 phases)
        /// </summary>
        public int CurrentPhase
        {
            get
            {
                var phaseLength = TotalWeeks / 3;
                if (CurrentWeek <= phaseLength) return 1;
                if (CurrentWeek <= phaseLength * 2) return 2;
                return 3;
            }
        }

        /// <summary>
        /// Get phase name based on current week
        /// </summary>
        public string PhaseName
        {
            get
            {
                return CurrentPhase switch
                {
                    1 => "Foundation",
                    2 => "Progressive Overload",
                    3 => "Peak Performance",
                    _ => "Unknown"
                };
            }
        }
    }

    /// <summary>
    /// Represents a single workout within a training program
    /// </summary>
    public class ProgramWorkout
    {
        public int Id { get; set; }

        [Required]
        public int ProgramId { get; set; }

        /// <summary>
        /// Week number (1-indexed)
        /// </summary>
        [Required]
        public int WeekNumber { get; set; }

        /// <summary>
        /// Day number (1-7, where 1=Monday)
        /// </summary>
        [Required]
        public int DayNumber { get; set; }

        /// <summary>
        /// Day name (e.g., "Monday", "Tuesday")
        /// </summary>
        [MaxLength(20)]
        public string? DayName { get; set; }

        /// <summary>
        /// Workout name (e.g., "Upper Body Strength", "Full Body Circuit")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string WorkoutName { get; set; } = string.Empty;

        /// <summary>
        /// Workout type (e.g., "Strength", "Cardio", "Rest", "Active Recovery")
        /// </summary>
        [MaxLength(50)]
        public string? WorkoutType { get; set; }

        /// <summary>
        /// Brief description or focus of the workout
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Estimated duration in minutes
        /// </summary>
        public int? EstimatedDuration { get; set; }

        /// <summary>
        /// Exercises and details stored as JSON array
        /// Format: [{name, sets, reps, rest, weight, notes}, ...]
        /// </summary>
        [Required]
        public string ExercisesJson { get; set; } = "[]";

        /// <summary>
        /// Warm-up routine (stored as text or JSON)
        /// </summary>
        [MaxLength(1000)]
        public string? WarmUp { get; set; }

        /// <summary>
        /// Cool-down routine (stored as text or JSON)
        /// </summary>
        [MaxLength(1000)]
        public string? CoolDown { get; set; }

        /// <summary>
        /// Whether this workout has been completed by the user
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// When the workout was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Optional notes after completing the workout
        /// </summary>
        [MaxLength(1000)]
        public string? CompletionNotes { get; set; }

        /// <summary>
        /// Order/sequence number for sorting within a week
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// Whether this is a rest day (no workout exercises)
        /// </summary>
        public bool IsRestDay { get; set; }

        // Navigation properties
        public Program? Program { get; set; }
    }
}
