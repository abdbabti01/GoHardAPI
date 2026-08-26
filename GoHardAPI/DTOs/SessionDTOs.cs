using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using GoHardAPI.Converters;
using GoHardAPI.Models;

namespace GoHardAPI.DTOs
{
    public class CreateSessionFromProgramWorkoutDto
    {
        public int ProgramWorkoutId { get; set; }
        public int ProgramId { get; set; } // Pass programId to fix old data issue
        public DateTime? ScheduledDate { get; set; } // Optional: client-calculated date (in local timezone)
    }

    /// <summary>
    /// Request body for PUT /api/v1/sessions/{id}.
    /// Intentionally excludes Id and UserId: the route segment identifies the session,
    /// and the JWT identifies its owner. Neither is client-assignable.
    /// </summary>
    public class SessionUpdateRequestDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = SessionStatus.Draft;

        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public int? Duration { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? PausedAt { get; set; }

        /// <summary>
        /// Optimistic-concurrency version the client last observed for this session.
        /// Null is accepted for legacy-client compatibility and is resolved to 1
        /// (see SessionsController.UpdateSession) — it does not bypass conflict detection.
        /// </summary>
        public int? Version { get; set; }
    }

    /// <summary>
    /// Authoritative session representation returned to clients after a successful
    /// update, and as the "serverData" payload of a 409 conflict response.
    /// Never expose the EF Session entity directly (navigation properties, tracking state).
    /// </summary>
    public class SessionResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime Date { get; set; }

        public int? Duration { get; set; }
        public string? Notes { get; set; }
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string Status { get; set; } = SessionStatus.Draft;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public int? ProgramId { get; set; }
        public int? ProgramWorkoutId { get; set; }
        public int Version { get; set; }

        /// <summary>
        /// Centralized entity-to-DTO mapping. Used for both the success response
        /// and the conflict response's serverData, so the two never drift apart.
        /// </summary>
        public static SessionResponseDto FromEntity(Session session)
        {
            return new SessionResponseDto
            {
                Id = session.Id,
                UserId = session.UserId,
                Date = session.Date,
                Duration = session.Duration,
                Notes = session.Notes,
                Type = session.Type,
                Name = session.Name,
                Status = session.Status,
                StartedAt = session.StartedAt,
                CompletedAt = session.CompletedAt,
                PausedAt = session.PausedAt,
                ProgramId = session.ProgramId,
                ProgramWorkoutId = session.ProgramWorkoutId,
                Version = session.Version,
            };
        }
    }

    public class StartPlannedWorkoutRequest
    {
        public DateTime? Date { get; set; }
        public DateTime? StartedAt { get; set; }
    }

    /// <summary>
    /// Request to reorder exercises within a session
    /// </summary>
    public class ReorderExercisesRequest
    {
        /// <summary>
        /// List of exercise IDs in the desired order.
        /// The index in this list becomes the new SortOrder.
        /// </summary>
        public required List<int> ExerciseIds { get; set; }
    }
}
