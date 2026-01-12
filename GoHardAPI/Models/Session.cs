using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    public class Session
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public int? Duration { get; set; } // in minutes

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; } // e.g., "Strength", "Cardio", "Mixed"

        [MaxLength(100)]
        public string? Name { get; set; } // Custom workout name e.g., "Pull Day", "Leg Day"

        [MaxLength(20)]
        public string Status { get; set; } = "draft";  // "draft", "in_progress", "completed"

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
