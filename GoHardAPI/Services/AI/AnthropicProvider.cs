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

        public string ProviderName => "Anthropic";

        public AnthropicProvider(IConfiguration configuration, ILogger<AnthropicProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var apiKey = configuration["AISettings:Anthropic:ApiKey"]
                ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
                ?? throw new InvalidOperationException("Anthropic API key not configured");

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
                var model = _configuration["AISettings:Anthropic:Model"] ?? "claude-3-5-sonnet-20241022";
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
                return new AIResponse
                {
                    Content = textContent?.Text ?? "",
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
            var model = _configuration["AISettings:Anthropic:Model"] ?? "claude-3-5-sonnet-20241022";
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
