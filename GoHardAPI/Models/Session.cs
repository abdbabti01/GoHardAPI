using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    public class Session
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public int? Duration { get; set; } // in minutes

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; } // e.g., "Strength", "Cardio", "Mixed"

        [MaxLength(20)]
        public string Status { get; set; } = "draft";  // "draft", "in_progress", "completed"

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}
