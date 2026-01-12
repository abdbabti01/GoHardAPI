using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    public class ExerciseSet
    {
        public int Id { get; set; }

        [Required]
        public int ExerciseId { get; set; }

        [Required]
        public int SetNumber { get; set; }  // 1, 2, 3, etc.

        public int? Reps { get; set; }

        public double? Weight { get; set; }  // in kg

        public int? Duration { get; set; }  // in seconds (for timed exercises like cardio)

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }

        // Version tracking for conflict resolution (Issue #13)
        public int Version { get; set; } = 1;

        // Navigation property
        public Exercise? Exercise { get; set; }
    }
}
