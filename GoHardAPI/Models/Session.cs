using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Defines the possible states of a workout session.
    /// </summary>
    public static class SessionStatus
    {
        public const string Draft = "draft";
        public const string InProgress = "in_progress";
        public const string Completed = "completed";
        public const string Planned = "planned";

        public static readonly string[] ValidStatuses = { Draft, InProgress, Completed, Planned };

        public static bool IsValid(string status) =>
            ValidStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }

    public class Session
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total workout duration in minutes. Calculated from StartedAt to CompletedAt,
        /// excluding paused time. Flutter clients should display this as minutes.
        /// </summary>
        public int? Duration { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; } // e.g., "Strength", "Cardio", "Mixed"

        [MaxLength(100)]
        public string? Name { get; set; } // Custom workout name e.g., "Pull Day", "Leg Day"

        /// <summary>
        /// Session state. Use SessionStatus constants: Draft, InProgress, Completed, Planned
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = SessionStatus.Draft;

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? PausedAt { get; set; }  // When timer was paused (null if running)

        // Program linkage
        public int? ProgramId { get; set; }  // Links to Program if this session is from a program
        public int? ProgramWorkoutId { get; set; }  // Links to ProgramWorkout if this session is from a program workout

        // Version tracking for conflict resolution (Issue #13)
        public int Version { get; set; } = 1;

        // Navigation properties
        public User? User { get; set; }
        public Program? Program { get; set; }
        public ProgramWorkout? ProgramWorkout { get; set; }
        public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}
