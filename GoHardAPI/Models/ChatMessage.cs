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

        /// <summary>
        /// Type of structured content: 'text', 'workout_plan', 'meal_plan', 'progress_analysis'
        /// Used for rendering rich preview cards in the app
        /// </summary>
        [MaxLength(50)]
        public string ContentType { get; set; } = "text";

        /// <summary>
        /// JSON-serialized structured data for rich preview cards.
        /// Schema depends on ContentType (e.g., workout sessions, meal plan days).
        /// Null for regular text messages.
        /// </summary>
        public string? StructuredData { get; set; }

        // Navigation property
        public ChatConversation? Conversation { get; set; }
    }
}
