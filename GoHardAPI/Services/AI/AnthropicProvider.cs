using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using GoHardAPI.Models;

namespace GoHardAPI.Services.AI
{
    /// <summary>
    /// Anthropic Claude AI provider implementation
    /// </summary>
    public class AnthropicProvider : IAIProvider
    {
        private readonly AnthropicClient _client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AnthropicProvider> _logger;

        /// <summary>Model used when AISettings:Anthropic:Model is not configured (mirrors appsettings.json).</summary>
        public const string DefaultModel = "claude-haiku-4-5";

        public string ProviderName => "Anthropic";

        public AnthropicProvider(IConfiguration configuration, ILogger<AnthropicProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // appsettings.json ships an empty ApiKey, which would otherwise shadow the
            // ANTHROPIC_API_KEY environment variable: treat empty/whitespace as "not configured".
            var configuredKey = configuration["AISettings:Anthropic:ApiKey"];
            var apiKey = !string.IsNullOrWhiteSpace(configuredKey)
                ? configuredKey
                : Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Anthropic API key not configured");
            }

            _client = new AnthropicClient(apiKey);
        }

        public async Task<AIResponse> SendMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string systemPrompt)
        {
            try
            {
                var messages = BuildMessages(conversationHistory, userMessage);
                var model = _configuration["AISettings:Anthropic:Model"] ?? DefaultModel;
                var maxTokens = int.Parse(_configuration["AISettings:MaxTokens"] ?? "4096");

                var parameters = new MessageParameters
                {
                    Messages = messages,
                    MaxTokens = maxTokens,
                    Model = model,
                    Stream = false,
                    Temperature = 1.0m
                };

                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    parameters.System = new List<SystemMessage> { new SystemMessage(systemPrompt) };
                }

                var response = await _client.Messages.GetClaudeMessageAsync(parameters);

                var textContent = response.Content.FirstOrDefault() as TextContent;

                // A reply cut off at max_tokens, or with no text, is a failure so AIService
                // can fall back instead of saving a truncated/empty answer.
                if (response.StopReason == "max_tokens" || string.IsNullOrWhiteSpace(textContent?.Text))
                {
                    throw new Exception($"Anthropic API returned an incomplete response (stop_reason: {response.StopReason ?? "none"})");
                }

                return new AIResponse
                {
                    Content = textContent.Text,
                    Model = response.Model,
                    InputTokens = response.Usage.InputTokens,
                    OutputTokens = response.Usage.OutputTokens,
                    Provider = ProviderName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Anthropic API error");
                throw;
            }
        }

        public async IAsyncEnumerable<string> StreamMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string systemPrompt)
        {
            var messages = BuildMessages(conversationHistory, userMessage);
            var model = _configuration["AISettings:Anthropic:Model"] ?? DefaultModel;
            var maxTokens = int.Parse(_configuration["AISettings:MaxTokens"] ?? "4096");

            var parameters = new MessageParameters
            {
                Messages = messages,
                MaxTokens = maxTokens,
                Model = model,
                Stream = true,
                Temperature = 1.0m
            };

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                parameters.System = new List<SystemMessage> { new SystemMessage(systemPrompt) };
            }

            await foreach (var response in _client.Messages.StreamClaudeMessageAsync(parameters))
            {
                if (response.Delta?.Text != null)
                {
                    yield return response.Delta.Text;
                }
            }
        }

        private List<Message> BuildMessages(List<ChatMessage> history, string newMessage)
        {
            var messages = new List<Message>();

            // Limit conversation context to avoid token limits
            var contextLimit = int.Parse(_configuration["AISettings:ConversationContextLimit"] ?? "10");
            var recentHistory = history.OrderByDescending(m => m.CreatedAt)
                .Take(contextLimit)
                .Reverse()
                .ToList();

            foreach (var msg in recentHistory)
            {
                messages.Add(new Message
                {
                    Role = msg.Role == "user" ? RoleType.User : RoleType.Assistant,
                    Content = new List<ContentBase> { new TextContent { Text = msg.Content } }
                });
            }

            // Add new user message
            messages.Add(new Message
            {
                Role = RoleType.User,
                Content = new List<ContentBase> { new TextContent { Text = newMessage } }
            });

            return messages;
        }
    }
}
