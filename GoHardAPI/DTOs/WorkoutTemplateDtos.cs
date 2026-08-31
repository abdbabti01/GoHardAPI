using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.DTOs
{
    /// <summary>
    /// Public shape of a workout template returned by every WorkoutTemplates read/create endpoint.
    /// Deliberately excludes the <c>CreatedByUser</c> navigation property (and everything on User
    /// beyond its display name) so PasswordHash, Email, FcmToken and other account fields can never
    /// be serialized through this controller. Field names mirror the real domain: ownership is the
    /// nullable <see cref="CreatedByUserId"/>, community visibility is the explicit
    /// <see cref="IsPublic"/>. No synthetic <c>userId</c>, <c>isCommunity</c> or <c>updatedAt</c>
    /// aliases are emitted.
    /// </summary>
    public class WorkoutTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ExercisesJson { get; set; } = string.Empty;
        public string RecurrencePattern { get; set; } = string.Empty;
        public string? DaysOfWeek { get; set; }
        public int? IntervalDays { get; set; }
        public int? EstimatedDuration { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public bool IsCustom { get; set; }
        public bool IsPublic { get; set; }

        /// <summary>Null for system templates; the owning user's id for custom templates.</summary>
        public int? CreatedByUserId { get; set; }

        /// <summary>Display name of the owning user, or null for system templates.</summary>
        public string? CreatedByUserName { get; set; }

        public int UsageCount { get; set; }
        public double? Rating { get; set; }
        public int RatingCount { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// UTC timestamp the template was last used (via increment-usage). Retains its real
        /// meaning — it is not an "updated at" audit column.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }
    }

    /// <summary>
    /// Allowed recurrence patterns for a workout template. Kept as a shared constant so the
    /// controller, request validation and tests agree on one list.
    /// </summary>
    public static class RecurrencePatterns
    {
        public const string Daily = "daily";
        public const string Weekly = "weekly";
        public const string Custom = "custom";

        public static readonly string[] All = { Daily, Weekly, Custom };

        public static bool IsValid(string? value) => value != null && Array.IndexOf(All, value) >= 0;
    }

    /// <summary>
    /// Request body for POST /api/v1/workouttemplates. Contains only client-owned fields.
    /// Server-owned state (Id, CreatedByUserId, IsCustom, UsageCount, Rating, RatingCount,
    /// CreatedAt, LastUsedAt) is not bindable here, so it cannot be over-posted.
    /// </summary>
    public class CreateWorkoutTemplateRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MinLength(1)]
        public string ExercisesJson { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string RecurrencePattern { get; set; } = string.Empty;

        [MaxLength(20)]
        [RegularExpression(@"^\s*\d+(\s*,\s*\d+)*\s*$", ErrorMessage = "DaysOfWeek must be a comma-separated list of day numbers.")]
        public string? DaysOfWeek { get; set; }

        [Range(1, 365)]
        public int? IntervalDays { get; set; }

        [Range(1, 1440)]
        public int? EstimatedDuration { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        /// <summary>Null is treated as active (matches the domain default).</summary>
        public bool? IsActive { get; set; }

        /// <summary>Opt-in community publication. Null or false keeps the template private.</summary>
        public bool? IsPublic { get; set; }
    }

    /// <summary>
    /// Request body for PUT /api/v1/workouttemplates/{id}. The route segment identifies the
    /// template and the JWT identifies its owner, so neither Id nor CreatedByUserId is bindable.
    /// Server-computed fields (UsageCount, Rating, RatingCount, CreatedAt, LastUsedAt) are also
    /// absent and therefore cannot be altered through an update.
    /// </summary>
    public class UpdateWorkoutTemplateRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MinLength(1)]
        public string ExercisesJson { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string RecurrencePattern { get; set; } = string.Empty;

        [MaxLength(20)]
        [RegularExpression(@"^\s*\d+(\s*,\s*\d+)*\s*$", ErrorMessage = "DaysOfWeek must be a comma-separated list of day numbers.")]
        public string? DaysOfWeek { get; set; }

        [Range(1, 365)]
        public int? IntervalDays { get; set; }

        [Range(1, 1440)]
        public int? EstimatedDuration { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        /// <summary>Null is treated as active (matches the domain default).</summary>
        public bool? IsActive { get; set; }

        /// <summary>Null is treated as private. PUT replaces this value like every other field.</summary>
        public bool? IsPublic { get; set; }
    }

    /// <summary>
    /// Request body for POST /api/v1/workouttemplates/{id}/rate. Matches the object shape the
    /// mobile client sends (<c>{ "rating": 4.5 }</c>).
    /// </summary>
    public class RateWorkoutTemplateRequest
    {
        [Range(1, 5)]
        public double Rating { get; set; }
    }
}
