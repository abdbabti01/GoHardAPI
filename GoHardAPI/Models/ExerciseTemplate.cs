using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    public class ExerciseTemplate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; } // e.g., "Chest", "Back", "Legs", "Cardio"

        [MaxLength(50)]
        public string? MuscleGroup { get; set; } // e.g., "Pectorals", "Quadriceps"

        [MaxLength(50)]
        public string? Equipment { get; set; } // e.g., "Barbell", "Dumbbell", "Bodyweight"

        [MaxLength(50)]
        public string? Difficulty { get; set; } // e.g., "Beginner", "Intermediate", "Advanced"

        public string? VideoUrl { get; set; }

        public string? ImageUrl { get; set; }

        [MaxLength(2000)]
        public string? Instructions { get; set; }

        public bool IsCustom { get; set; } = false; // false = system template, true = user-created

        public int? CreatedByUserId { get; set; } // null for system templates

        // Navigation property
        public User? CreatedByUser { get; set; }
    }
}
