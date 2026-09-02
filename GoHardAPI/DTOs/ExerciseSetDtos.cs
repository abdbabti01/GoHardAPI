using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.DTOs
{
    /// <summary>
    /// Request body for PUT /api/v1/exercisesets/{id}.
    /// <para>
    /// The route segment is the identity of record: <see cref="Id"/> must equal it.
    /// <see cref="ExerciseId"/> is the set's <b>existing</b> parent exercise - it is
    /// verified against the stored row, never used to move the set to a different
    /// exercise. The parent FK and the optimistic-concurrency Version are
    /// intentionally not client-assignable through this contract.
    /// </para>
    /// </summary>
    public class ExerciseSetUpdateRequestDto
    {
        /// <summary>Must match the {id} route segment.</summary>
        public int Id { get; set; }

        /// <summary>The set's current parent exercise id. Must match the stored row.</summary>
        public int ExerciseId { get; set; }

        public int SetNumber { get; set; }

        public int? Reps { get; set; }

        public double? Weight { get; set; }

        public int? Duration { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }
    }
}
