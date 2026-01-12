using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    public class Exercise
    {
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public int? Duration { get; set; } // in seconds (for planned/target duration)

        public int? RestTime { get; set; } // in seconds (planned rest between sets)

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int? ExerciseTemplateId { get; set; } // Link to template if used

        // Version tracking for conflict resolution (Issue #13)
        public int Version { get; set; } = 1;

        // Navigation properties
        public Session? Session { get; set; }
        public ExerciseTemplate? ExerciseTemplate { get; set; }
        public ICollection<ExerciseSet> ExerciseSets { get; set; } = new List<ExerciseSet>();
    }
}
