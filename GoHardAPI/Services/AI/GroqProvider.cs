using GoHardAPI.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoHardAPI.Services.AI
{
    /// <summary>
    /// Groq AI provider implementation using OpenAI-compatible API
    /// Uses HttpClient for direct API calls (no SDK dependency)
    /// </summary>
    public class GroqProvider : IAIProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GroqProvider> _logger;
        private const string GROQ_API_BASE = "https://api.groq.com";

        public string ProviderName => "Groq";

        public GroqProvider(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GroqProvider> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _logger = logger;

            var apiKey = configuration["AISettings:Groq:ApiKey"]
                ?? Environment.GetEnvironmentVariable("GROQ_API_KEY")
                ?? throw new InvalidOperationException("Groq API key not configured");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.BaseAddress = new Uri(GROQ_API_BASE);
        }

        public async Task<AIResponse> SendMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string systemPrompt)
        {
            try
            {
                var messages = BuildMessages(conversationHistory, userMessage, systemPrompt);
                var model = _configuration["AISettings:Groq:Model"] ?? "llama-3.3-70b-versatile";
                var maxTokens = int.Parse(_configuration["AISettings:MaxTokens"] ?? "4096");

                var requestBody = new
                {
                    model = model,
                    messages = messages,
                    max_tokens = maxTokens,
                    temperature = 1.0,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/openai/v1/chat/completions", content);

                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Groq API error ({response.StatusCode}): {responseJson}");
                    response.EnsureSuccessStatusCode(); // This will throw with the status code
                }
                var result = JsonSerializer.Deserialize<GroqResponse>(responseJson);

                if (result?.Choices == null || result.Choices.Count == 0)
                {
                    throw new Exception("Groq API returned no response");
                }

                return new AIResponse
                {
                    Content = result.Choices[0].Message.Content,
                    Model = result.Model ?? model,
                    InputTokens = result.Usage?.PromptTokens ?? 0,
                    OutputTokens = result.Usage?.CompletionTokens ?? 0,
                    Provider = ProviderName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Groq API error");
                throw;
            }
        }

        public async IAsyncEnumerable<string> StreamMessageAsync(
            string userMessage,
            List<ChatMessage> conversationHistory,
            string systemPrompt)
        {
            var messages = BuildMessages(conversationHistory, userMessage, systemPrompt);
            var model = _configuration["AISettings:Groq:Model"] ?? "llama-3.3-70b-versatile";
            var maxTokens = int.Parse(_configuration["AISettings:MaxTokens"] ?? "4096");

            var requestBody = new
            {
                model = model,
                messages = messages,
                max_tokens = maxTokens,
                temperature = 1.0,
                stream = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/openai/v1/chat/completions")
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;

                var data = line.Substring(6); // Remove "data: " prefix
                if (data == "[DONE]")
                    break;

                var chunk = JsonSerializer.Deserialize<GroqStreamChunk>(data);
                var delta = chunk?.Choices?[0]?.Delta?.Content;
                if (!string.IsNullOrEmpty(delta))
                {
                    yield return delta;
                }
            }
        }

        private List<object> BuildMessages(
            List<ChatMessage> history,
            string newMessage,
            string systemPrompt)
        {
            var messages = new List<object>();

            // Add system prompt if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }

            // Limit conversation context to avoid token limits
            var contextLimit = int.Parse(_configuration["AISettings:ConversationContextLimit"] ?? "10");
            var recentHistory = history.OrderByDescending(m => m.CreatedAt)
                .Take(contextLimit)
                .Reverse()
                .ToList();

            foreach (var msg in recentHistory)
            {
                messages.Add(new
                {
                    role = msg.Role,
                    content = msg.Content
                });
            }

            // Add new user message
            messages.Add(new
            {
                role = "user",
                content = newMessage
            });

            return messages;
        }

        #region Response Models

        private class GroqResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("model")]
            public string? Model { get; set; }

            [JsonPropertyName("choices")]
            public List<GroqChoice>? Choices { get; set; }

            [JsonPropertyName("usage")]
            public GroqUsage? Usage { get; set; }
        }

        private class GroqChoice
        {
            [JsonPropertyName("message")]
            public GroqMessage Message { get; set; } = new();

            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private class GroqMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "";

            [JsonPropertyName("content")]
            public string Content { get; set; } = "";
        }

        private class GroqUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }

        private class GroqStreamChunk
        {
            [JsonPropertyName("choices")]
            public List<GroqStreamChoice>? Choices { get; set; }
        }

        private class GroqStreamChoice
        {
            [JsonPropertyName("delta")]
            public GroqDelta? Delta { get; set; }
        }

        private class GroqDelta
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        #endregion
    }
}
