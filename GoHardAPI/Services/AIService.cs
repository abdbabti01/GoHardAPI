using GoHardAPI.Models;
using GoHardAPI.Services.AI;

namespace GoHardAPI.Services
{
    /// <summary>
    /// High-level AI service that uses provider factory
    /// Controllers use this service to interact with AI providers
    /// Includes automatic fallback when a provider hits rate limits
    /// </summary>
    public class AIService
    {
        private readonly AIProviderFactory _providerFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIService> _logger;

        // Fallback order when a provider fails
        private static readonly string[] FallbackProviders = { "Groq", "Anthropic", "OpenAI" };

        public AIService(AIProviderFactory providerFactory, IConfiguration configuration, ILogger<AIService> logger)
        {
            _providerFactory = providerFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Send a message to AI and get response with automatic fallback on rate limits
        /// </summary>
        /// <param name="userMessage">User's message</param>
        /// <param name="conversationHistory">Previous conversation messages</param>
        /// <param name="conversationType">Type of conversation (general, workout_plan, meal_plan, progress_analysis)</param>
        /// <param name="providerName">Optional provider override (null uses default with fallback)</param>
        /// <returns>AI response</returns>
        public async Task<AIResponse> SendMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string conversationType = "general",
            string? providerName = null)
        {
            var systemPrompt = GetSystemPrompt(conversationType);
            var providersToTry = GetProvidersToTry(providerName);

            Exception? lastException = null;

            foreach (var currentProvider in providersToTry)
            {
                try
                {
                    var provider = _providerFactory.GetProvider(currentProvider);
                    _logger.LogInformation("Using {Provider} for conversation type: {ConversationType}", provider.ProviderName, conversationType);

                    return await provider.SendMessageAsync(userMessage, conversationHistory, systemPrompt);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("Too Many Requests"))
                {
                    _logger.LogWarning("Provider {Provider} rate limited, trying next provider. Error: {Error}", currentProvider, ex.Message);
                    lastException = ex;
                    continue;
                }
                catch (NotImplementedException)
                {
                    _logger.LogDebug("Provider {Provider} not implemented, skipping", currentProvider);
                    continue;
                }
                catch (Exception ex) when (ex.Message.Contains("API key") || ex.Message.Contains("Unauthorized") || ex.Message.Contains("401"))
                {
                    _logger.LogWarning("Provider {Provider} authentication failed, trying next provider", currentProvider);
                    lastException = ex;
                    continue;
                }
            }

            // All providers failed
            _logger.LogError(lastException, "All AI providers failed or are rate limited");
            throw new InvalidOperationException("All AI providers are currently unavailable. Please try again later.", lastException);
        }

        /// <summary>
        /// Stream AI response for real-time display
        /// Note: Streaming uses the default provider without fallback. If rate limited, will throw.
        /// For reliable AI calls with fallback, use SendMessageAsync instead.
        /// </summary>
        public IAsyncEnumerable<string> StreamMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string conversationType = "general",
            string? providerName = null)
        {
            // For streaming, try to find a working provider before starting
            var providersToTry = GetProvidersToTry(providerName);
            var systemPrompt = GetSystemPrompt(conversationType);

            foreach (var currentProvider in providersToTry)
            {
                try
                {
                    var provider = _providerFactory.GetProvider(currentProvider);
                    _logger.LogInformation("Streaming with {Provider} for conversation type: {ConversationType}", provider.ProviderName, conversationType);
                    return provider.StreamMessageAsync(userMessage, conversationHistory, systemPrompt);
                }
                catch (NotImplementedException)
                {
                    _logger.LogDebug("Provider {Provider} not implemented, skipping", currentProvider);
                    continue;
                }
            }

            throw new InvalidOperationException("No AI providers are available");
        }

        /// <summary>
        /// Get the list of providers to try, starting with the specified or default provider
        /// </summary>
        private List<string> GetProvidersToTry(string? preferredProvider)
        {
            var defaultProvider = preferredProvider ?? _configuration["AISettings:DefaultProvider"] ?? "Groq";

            var providers = new List<string> { defaultProvider };

            // Add fallback providers that aren't already the default
            foreach (var fallback in FallbackProviders)
            {
                if (!providers.Contains(fallback, StringComparer.OrdinalIgnoreCase))
                {
                    providers.Add(fallback);
                }
            }

            return providers;
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

                _ => @"You are a knowledgeable fitness and wellness AI assistant for GoHard, a workout tracking app. You ONLY help users with fitness and training-related topics.

You help users with:
- Workout and training questions
- Exercise form and technique guidance
- Fitness and nutrition advice
- Motivation and goal setting
- General fitness knowledge

IMPORTANT RESTRICTIONS:
- You MUST REFUSE to answer questions unrelated to fitness, training, nutrition, or health
- If asked about non-fitness topics (coding, math, general knowledge, writing, etc.), politely decline and remind users you're a fitness-only assistant
- Example refusal: ""I'm specifically designed to help with fitness and training. I can't assist with that topic, but I'd be happy to help with your workout plan, nutrition, or any fitness-related questions!""

Be friendly, encouraging, and provide accurate, evidence-based fitness information. When unsure, acknowledge limitations and suggest consulting professionals for medical or injury-related concerns."
            };
        }
    }
}
