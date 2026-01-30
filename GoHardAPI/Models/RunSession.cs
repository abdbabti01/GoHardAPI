using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Represents a running/cardio session with GPS tracking
    /// </summary>
    public class RunSession
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [MaxLength(100)]
        public string? Name { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Distance in kilometers
        /// </summary>
        public double? Distance { get; set; }

        /// <summary>
        /// Duration in seconds
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// Average pace in min/km
        /// </summary>
        public double? AveragePace { get; set; }

        /// <summary>
        /// Estimated calories burned
        /// </summary>
        public int? Calories { get; set; }

        /// <summary>
        /// Session status: draft, in_progress, completed
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = "draft";

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? PausedAt { get; set; }

        /// <summary>
        /// GPS route stored as JSON array of points
        /// Each point: { latitude, longitude, altitude?, timestamp, speed?, accuracy? }
        /// </summary>
        [Column(TypeName = "text")]
        public string? RouteJson { get; set; }

        // Navigation properties
        public User? User { get; set; }
    }

    /// <summary>
    /// DTO for GPS point (used for JSON serialization)
    /// </summary>
    public class GpsPointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public DateTime Timestamp { get; set; }
        public double? Speed { get; set; }
        public double? Accuracy { get; set; }
    }
}
