using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Represents body measurements and metrics tracking over time
    /// </summary>
    public class BodyMetric
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// When this measurement was recorded
        /// </summary>
        [Required]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Body weight in kg or lbs
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// Body fat percentage
        /// </summary>
        public decimal? BodyFatPercentage { get; set; }

        /// <summary>
        /// Chest circumference in cm or inches
        /// </summary>
        public decimal? ChestCircumference { get; set; }

        /// <summary>
        /// Waist circumference in cm or inches
        /// </summary>
        public decimal? WaistCircumference { get; set; }

        /// <summary>
        /// Hip circumference in cm or inches
        /// </summary>
        public decimal? HipCircumference { get; set; }

        /// <summary>
        /// Arm circumference in cm or inches
        /// </summary>
        public decimal? ArmCircumference { get; set; }

        /// <summary>
        /// Thigh circumference in cm or inches
        /// </summary>
        public decimal? ThighCircumference { get; set; }

        /// <summary>
        /// Calf circumference in cm or inches
        /// </summary>
        public decimal? CalfCircumference { get; set; }

        /// <summary>
        /// Optional notes about this measurement
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// URL to progress photo (optional)
        /// </summary>
        [MaxLength(500)]
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// When this record was created
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
    }
}
