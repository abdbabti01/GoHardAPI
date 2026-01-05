using GoHardAPI.Models;

namespace GoHardAPI.Services.AI
{
    /// <summary>
    /// Interface for all AI providers (Claude, GPT, Gemini, etc.)
    /// Allows easy switching between providers via configuration
    /// </summary>
    public interface IAIProvider
    {
        /// <summary>
        /// Send a message and get complete AI response
        /// </summary>
        /// <param name="userMessage">The user's message</param>
        /// <param name="conversationHistory">Previous messages for context</param>
        /// <param name="systemPrompt">System prompt defining AI behavior</param>
        /// <returns>Complete AI response with token usage</returns>
        Task<AIResponse> SendMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string systemPrompt);

        /// <summary>
        /// Stream message response for real-time display
        /// </summary>
        /// <param name="userMessage">The user's message</param>
        /// <param name="conversationHistory">Previous messages for context</param>
        /// <param name="systemPrompt">System prompt defining AI behavior</param>
        /// <returns>Stream of text chunks</returns>
        IAsyncEnumerable<string> StreamMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string systemPrompt);

        /// <summary>
        /// Provider name (e.g., "Anthropic", "OpenAI", "Gemini")
        /// </summary>
        string ProviderName { get; }
    }

    /// <summary>
    /// Standard response from any AI provider
    /// </summary>
    public class AIResponse
    {
        public string Content { get; set; } = "";
        public string Model { get; set; } = "";
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public string Provider { get; set; } = "";
    }
}
