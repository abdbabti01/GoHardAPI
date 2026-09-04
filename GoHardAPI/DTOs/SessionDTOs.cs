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
    /// Request body for POST /api/v1/sessions.
    ///
    /// Replaces raw <see cref="Session"/> binding: a client can no longer post a
    /// server <c>Id</c>, a <c>UserId</c>, a <c>User</c> navigation, a <c>Version</c>,
    /// or an <c>Exercises</c>/child graph. The authenticated owner comes exclusively
    /// from <c>GetCurrentUserId()</c>; child exercises are created afterwards through
    /// <c>POST /api/v1/sessions/{id}/exercises</c>.
    ///
    /// Only the supported scalar creation fields are accepted, plus the optional
    /// <see cref="ClientOperationId"/> idempotency key.
    /// </summary>
    public class SessionCreateRequestDto
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

        /// <summary>Optional program linkage (unchanged from the legacy raw-Session create).</summary>
        public int? ProgramId { get; set; }
        public int? ProgramWorkoutId { get; set; }

        /// <summary>
        /// Optional client idempotency key. When present, POST becomes idempotent
        /// per <c>(authenticated UserId, ClientOperationId)</c>: the first request
        /// creates exactly one Session, every later request with the same key
        /// returns that same canonical Session unchanged. When absent, legacy
        /// create behavior is preserved (always a new Session, 201).
        /// </summary>
        public Guid? ClientOperationId { get; set; }

        /// <summary>
        /// Projects the accepted scalar fields onto a new Session for the current
        /// user. Intentionally never touches Id, UserId, Version, User or Exercises.
        /// </summary>
        public Session ToNewSession(int userId) => new Session
        {
            UserId = userId,
            Name = Name,
            Type = Type,
            Status = string.IsNullOrWhiteSpace(Status) ? SessionStatus.Draft : Status,
            Date = Date,
            Duration = Duration,
            Notes = Notes,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            PausedAt = PausedAt,
            ProgramId = ProgramId,
            ProgramWorkoutId = ProgramWorkoutId,
            ClientOperationId = ClientOperationId,
        };
    }

    /// <summary>
    /// Fixed, non-sensitive error codes returned by the keyed POST /api/v1/sessions
    /// state machine. Stable strings — clients branch on these.
    /// </summary>
    public static class SessionCreateErrorCodes
    {
        /// <summary>
        /// A supplied <c>programId</c> / <c>programWorkoutId</c> is missing OR owned by
        /// another user (deliberately indistinguishable, so it is not a cross-user
        /// existence oracle). HTTP 404.
        /// </summary>
        public const string ProgramNotFound = "program_not_found";

        /// <summary>
        /// Replay of an operation that was canceled. HTTP 409.
        /// DORMANT in P1 — nothing writes <c>SessionCreateOperation.CanceledAt</c> until the
        /// P2 DELETE-by-operation-key endpoint.
        /// </summary>
        public const string OperationCanceled = "operation_canceled";

        /// <summary>Replay of a completed operation whose Session no longer exists. HTTP 410.</summary>
        public const string OperationTargetDeleted = "operation_target_deleted";

        /// <summary>
        /// The operation record is in an indeterminate state (present but never
        /// completed and not canceled). Fail closed — no second Session is created. HTTP 409.
        /// </summary>
        public const string OperationIncomplete = "operation_incomplete";
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
