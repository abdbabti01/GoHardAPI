using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Represents a recurring workout template with scheduling
    /// </summary>
    public class WorkoutTemplate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public string ExercisesJson { get; set; } // JSON array of exercises

        [Required]
        [MaxLength(20)]
        public string RecurrencePattern { get; set; } // "daily", "weekly", "custom"

        [MaxLength(20)]
        public string? DaysOfWeek { get; set; } // e.g., "1,3,5" for Mon, Wed, Fri (1=Mon, 7=Sun)

        public int? IntervalDays { get; set; } // For custom recurrence (e.g., every 3 days)

        public int? EstimatedDuration { get; set; } // in minutes

        [MaxLength(50)]
        public string? Category { get; set; } // Strength, Cardio, etc.

        public bool IsActive { get; set; } = true;

        public int UsageCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }

        public int? CreatedByUserId { get; set; } // Null for system templates

        public bool IsCustom { get; set; } = false; // User-created vs system template

        public double? Rating { get; set; } // Average rating (0-5)

        public int RatingCount { get; set; } = 0;

        // Navigation properties
        public User? CreatedByUser { get; set; }
        public ICollection<WorkoutTemplateRating> Ratings { get; set; } = new List<WorkoutTemplateRating>();
    }

    /// <summary>
    /// Tracks user ratings for workout templates
    /// </summary>
    public class WorkoutTemplateRating
    {
        public int Id { get; set; }

        [Required]
        public int WorkoutTemplateId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public double Rating { get; set; }

        [Required]
        public DateTime RatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public WorkoutTemplate? WorkoutTemplate { get; set; }
        public User? User { get; set; }
    }
}
