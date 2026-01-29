using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoHardAPI.Models
{
    public class Friendship
    {
        public int Id { get; set; }

        [Required]
        public int RequesterId { get; set; }

        [Required]
        public int AddresseeId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending, accepted, declined

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        // Navigation properties
        [ForeignKey("RequesterId")]
        public User Requester { get; set; } = null!;

        [ForeignKey("AddresseeId")]
        public User Addressee { get; set; } = null!;
    }
}
