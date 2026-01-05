using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.Models
{
    /// <summary>
    /// Represents a single message in a chat conversation
    /// </summary>
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "user"; // 'user' or 'assistant'

        [Required]
        public string Content { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Metadata for AI responses
        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }

        [MaxLength(100)]
        public string? Model { get; set; }

        // Navigation property
        public ChatConversation? Conversation { get; set; }
    }
}
