using GoHardAPI.Models;

namespace GoHardAPI.Services.AI
{
    /// <summary>
    /// OpenAI GPT provider implementation (stub for future use)
    /// To implement: Install OpenAI NuGet package and implement interface methods
    /// </summary>
    public class OpenAIProvider : IAIProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIProvider> _logger;

        public string ProviderName => "OpenAI";

        public OpenAIProvider(IConfiguration configuration, ILogger<OpenAIProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AIResponse> SendMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string systemPrompt)
        {
            // TODO: Implement OpenAI API integration
            // 1. Install NuGet: dotnet add package OpenAI
            // 2. Create ChatClient with API key from config
            // 3. Build messages array with system prompt + conversation history
            // 4. Call ChatClient.CompleteChatAsync
            // 5. Return AIResponse with content and token usage

            _logger.LogWarning("OpenAI provider not yet implemented");
            throw new NotImplementedException(
                "OpenAI provider is not yet implemented. " +
                "Please use Anthropic provider or implement this method.");
        }

        public async IAsyncEnumerable<string> StreamMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string systemPrompt)
        {
            // TODO: Implement OpenAI streaming
            // Use ChatClient.CompleteChatStreamingAsync for streaming responses

            _logger.LogWarning("OpenAI streaming not yet implemented");
            throw new NotImplementedException(
                "OpenAI streaming is not yet implemented. " +
                "Please use Anthropic provider or implement this method.");

#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }
    }
}
