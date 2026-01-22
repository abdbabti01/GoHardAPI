using System.ComponentModel.DataAnnotations;

namespace GoHardAPI.DTOs
{
    // Request DTOs
    public class CreateConversationRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = "";

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "general"; // 'general', 'workout_plan', 'meal_plan', 'progress_analysis'
    }

    public class SendMessageRequest
    {
        [Required]
        [MaxLength(10000)]
        public string Message { get; set; } = "";

        public bool Stream { get; set; } = false;
    }

    public class GenerateWorkoutPlanRequest
    {
        [Required]
        public string Goal { get; set; } = ""; // e.g., "build muscle", "lose weight", "gain strength"

        [Required]
        public string ExperienceLevel { get; set; } = ""; // "beginner", "intermediate", "advanced"

        [Required]
        [Range(1, 7)]
        public int DaysPerWeek { get; set; }

        public string Equipment { get; set; } = ""; // e.g., "full gym", "dumbbells only", "bodyweight"

        public string Limitations { get; set; } = ""; // injuries, time constraints, etc.
    }

    public class GenerateMealPlanRequest
    {
        [Required]
        public string DietaryGoal { get; set; } = ""; // e.g., "muscle gain", "fat loss", "maintenance"

        [Range(1000, 5000)]
        public int? TargetCalories { get; set; }

        public string? Macros { get; set; } // e.g., "40/30/30 protein/carbs/fat"

        public string Restrictions { get; set; } = ""; // allergies, vegetarian, vegan, etc.

        public string Preferences { get; set; } = ""; // favorite foods, meal frequency, etc.
    }

    public class AnalyzeProgressRequest
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? FocusArea { get; set; } // specific muscle group or exercise to analyze
    }

    // Response DTOs
    public class ConversationResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int MessageCount { get; set; }
        public bool IsArchived { get; set; }
    }

    public class ConversationDetailResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool IsArchived { get; set; }
        public List<MessageResponse> Messages { get; set; } = new();
    }

    public class MessageResponse
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }
        public string? Model { get; set; }
    }
}
