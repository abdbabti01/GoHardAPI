using GoHardAPI.Models;
using GoHardAPI.Services.AI;

namespace GoHardAPI.Services
{
    /// <summary>
    /// High-level AI service that uses provider factory
    /// Controllers use this service to interact with AI providers
    /// </summary>
    public class AIService
    {
        private readonly AIProviderFactory _providerFactory;
        private readonly ILogger<AIService> _logger;

        public AIService(AIProviderFactory providerFactory, ILogger<AIService> logger)
        {
            _providerFactory = providerFactory;
            _logger = logger;
        }

        /// <summary>
        /// Send a message to AI and get response
        /// </summary>
        /// <param name="userMessage">User's message</param>
        /// <param name="conversationHistory">Previous conversation messages</param>
        /// <param name="conversationType">Type of conversation (general, workout_plan, meal_plan, progress_analysis)</param>
        /// <param name="providerName">Optional provider override (null uses default)</param>
        /// <returns>AI response</returns>
        public async Task<AIResponse> SendMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string conversationType = "general",
            string? providerName = null)
        {
            var provider = _providerFactory.GetProvider(providerName);
            var systemPrompt = GetSystemPrompt(conversationType);

            _logger.LogInformation($"Using {provider.ProviderName} for conversation type: {conversationType}");

            return await provider.SendMessageAsync(userMessage, conversationHistory, systemPrompt);
        }

        /// <summary>
        /// Stream AI response for real-time display
        /// </summary>
        public IAsyncEnumerable<string> StreamMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string conversationType = "general",
            string? providerName = null)
        {
            var provider = _providerFactory.GetProvider(providerName);
            var systemPrompt = GetSystemPrompt(conversationType);

            _logger.LogInformation($"Streaming with {provider.ProviderName} for conversation type: {conversationType}");

            return provider.StreamMessageAsync(userMessage, conversationHistory, systemPrompt);
        }

        /// <summary>
        /// Get system prompt based on conversation type
        /// </summary>
        private string GetSystemPrompt(string conversationType)
        {
            return conversationType switch
            {
                "workout_plan" => @"You are an expert fitness coach and personal trainer. You help users create personalized workout plans based on their goals, experience level, and available equipment.

When generating workout plans:
- Ask about user's fitness goals, experience level, available days per week
- Consider any injuries or limitations
- Provide structured plans with exercises, sets, reps, and rest periods
- Include progressive overload recommendations
- Format responses with clear structure using markdown

Be encouraging and motivational while maintaining professional expertise.",

                "meal_plan" => @"You are a certified nutritionist and meal planning expert. You help users create personalized meal plans based on their dietary goals, preferences, and restrictions.

When generating meal plans:
- Ask about caloric goals, macronutrient preferences, dietary restrictions
- Consider allergies, food preferences, and cooking skill level
- Provide balanced meal suggestions with approximate macros and calories
- Include meal prep tips and shopping lists when appropriate
- Format responses with clear structure using markdown

Be supportive and educational while providing evidence-based nutrition guidance.",

                "progress_analysis" => @"You are a fitness analytics expert. You help users understand their training progress, identify plateaus, and optimize their workout performance.

When analyzing progress:
- Review workout history and performance metrics
- Identify trends, improvements, and areas needing attention
- Suggest modifications to overcome plateaus
- Celebrate achievements and provide constructive feedback
- Use data-driven insights while remaining encouraging

Be analytical yet supportive, helping users understand their fitness journey.",

                _ => @"You are a knowledgeable fitness and wellness AI assistant for GoHard, a workout tracking app. You help users with:
- Workout and training questions
- Exercise form and technique guidance
- Fitness and nutrition advice
- Motivation and goal setting
- General fitness knowledge

Be friendly, encouraging, and provide accurate, evidence-based information. When unsure, acknowledge limitations and suggest consulting professionals for medical or injury-related concerns."
            };
        }
    }
}
