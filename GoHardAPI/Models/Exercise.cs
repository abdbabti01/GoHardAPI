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

        /// <summary>
        /// Display order within the session (0-indexed). Used for drag-and-drop reordering.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        public int? Duration { get; set; } // in seconds (for planned/target duration)

        public int? RestTime { get; set; } // in seconds (planned rest between sets)

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int? ExerciseTemplateId { get; set; } // Link to template if used

        /// <summary>
        /// Persistent occurrence identity copied verbatim from the source
        /// <c>ProgramWorkout.ExercisesJson</c> entry's <c>occurrenceKey</c> field at Session
        /// materialization time (see <see cref="Services.ProgramWorkoutSessionMaterializer"/>
        /// and <see cref="Services.ProgramWorkoutExerciseOccurrences"/>). Identifies a specific
        /// exercise *occurrence* within one program workout — not the exercise type
        /// (<see cref="ExerciseTemplateId"/>) and not globally unique across all Exercises: the
        /// same key legitimately repeats across Exercises materialized from the same
        /// ProgramWorkout into different Sessions. Null for exercises with no program-workout
        /// source (ad-hoc <c>POST /sessions/{id}/exercises</c>) or materialized before this
        /// field existed / before the source workout's entries were normalized.
        /// </summary>
        [MaxLength(100)]
        public string? OccurrenceKey { get; set; }

        // Version tracking for conflict resolution (Issue #13)
        public int Version { get; set; } = 1;

        // Navigation properties
        public Session? Session { get; set; }
        public ExerciseTemplate? ExerciseTemplate { get; set; }
        public ICollection<ExerciseSet> ExerciseSets { get; set; } = new List<ExerciseSet>();
    }
}
