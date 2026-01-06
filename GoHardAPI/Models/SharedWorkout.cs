using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Represents a workout shared to the community by a user
    /// </summary>
    public class SharedWorkout
    {
        public int Id { get; set; }

        [Required]
        public int OriginalId { get; set; } // ID of original Session or Template

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } // "session" or "template"

        [Required]
        public int SharedByUserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string WorkoutName { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public string ExercisesJson { get; set; } // JSON array of exercises

        [Required]
        public int Duration { get; set; } // in minutes

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } // Strength, Cardio, HIIT, etc.

        [MaxLength(20)]
        public string? Difficulty { get; set; } // Beginner, Intermediate, Advanced

        public int LikeCount { get; set; } = 0;

        public int SaveCount { get; set; } = 0;

        public int CommentCount { get; set; } = 0;

        [Required]
        public DateTime SharedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User? SharedByUser { get; set; }
        public ICollection<SharedWorkoutLike> Likes { get; set; } = new List<SharedWorkoutLike>();
        public ICollection<SharedWorkoutSave> Saves { get; set; } = new List<SharedWorkoutSave>();
    }

    /// <summary>
    /// Tracks which users liked which shared workouts
    /// </summary>
    public class SharedWorkoutLike
    {
        public int Id { get; set; }

        [Required]
        public int SharedWorkoutId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime LikedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public SharedWorkout? SharedWorkout { get; set; }
        public User? User { get; set; }
    }

    /// <summary>
    /// Tracks which users saved which shared workouts
    /// </summary>
    public class SharedWorkoutSave
    {
        public int Id { get; set; }

        [Required]
        public int SharedWorkoutId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public SharedWorkout? SharedWorkout { get; set; }
        public User? User { get; set; }
    }
}
