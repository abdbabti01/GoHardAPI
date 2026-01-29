using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoHardAPI.Models
{
    public class DMConversation
    {
        public int Id { get; set; }

        [Required]
        public int User1Id { get; set; } // Lower ID always

        [Required]
        public int User2Id { get; set; } // Higher ID always

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        public int UnreadCountUser1 { get; set; } = 0;

        public int UnreadCountUser2 { get; set; } = 0;

        // Navigation properties
        [ForeignKey("User1Id")]
        public User User1 { get; set; } = null!;

        [ForeignKey("User2Id")]
        public User User2 { get; set; } = null!;
    }
}
