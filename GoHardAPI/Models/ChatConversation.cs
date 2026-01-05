using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Represents a chat conversation between user and AI
    /// </summary>
    public class ChatConversation
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "New Conversation";

        [MaxLength(50)]
        public string Type { get; set; } = "general"; // 'general', 'workout_plan', 'meal_plan', 'progress_analysis'

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastMessageAt { get; set; }

        public bool IsArchived { get; set; } = false;

        // Navigation properties
        public User? User { get; set; }
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
