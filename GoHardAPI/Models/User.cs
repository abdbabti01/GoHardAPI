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

        // Navigation property
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
