using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordSalt { get; set; } = string.Empty;

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }

        public bool IsActive { get; set; } = true;

        public double? Height { get; set; } // in cm
        public double? Weight { get; set; } // in kg

        [MaxLength(500)]
        public string? Goals { get; set; }

        // Profile Photo
        [MaxLength(500)]
        public string? ProfilePhotoUrl { get; set; }

        // Personal Details
        public DateTime? DateOfBirth { get; set; }
        [MaxLength(20)]
        public string? Gender { get; set; } // Male, Female, Other, PreferNotToSay

        // Body Metrics
        public double? TargetWeight { get; set; } // in kg
        public double? BodyFatPercentage { get; set; }
        public double? BMI { get; set; } // calculated field

        // Fitness Profile
        [MaxLength(50)]
        public string? ExperienceLevel { get; set; } // Beginner, Intermediate, Advanced, Expert
        [MaxLength(100)]
        public string? PrimaryGoal { get; set; } // WeightLoss, MuscleGain, Strength, Endurance, GeneralFitness

        // Preferences
        [MaxLength(20)]
        public string UnitPreference { get; set; } = "Metric"; // Metric or Imperial
        [MaxLength(20)]
        public string? ThemePreference { get; set; } // Light, Dark, System

        // Social/Bio
        [MaxLength(500)]
        public string? Bio { get; set; }
        [MaxLength(1000)]
        public string? FavoriteExercises { get; set; } // JSON array or comma-separated

        // Navigation property
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
