using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GoHardAPI.Models;
using GoHardAPI.Services;
using GoHardAPI.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    /// <summary>
    /// Provider fallback in <see cref="AIService.SendMessageAsync"/>. A retired model
    /// (Groq 404 model_not_found) used to escape the fallback loop and take every AI
    /// feature down with a 500; any single provider's failure must now move on to the
    /// next distinct provider, each tried at most once.
    /// </summary>
    public class AIServiceFallbackTests
    {
        // Dummy, non-secret values: only used to prove they never reach the logs.
        private const string FakeGroqKey = "gsk_test_not_a_real_key_111";
        private const string FakeAnthropicKey = "sk-ant-test-not-a-real-key-222";

        [Fact]
        public async Task Primary_success_returns_without_trying_fallbacks()
        {
            var groq = FakeProvider.Returning("Groq", "plan");
            var anthropic = FakeProvider.Returning("Anthropic", "unused");
            var service = Service(defaultProvider: "Groq", groq, anthropic);

            var response = await service.SendMessageAsync("hi", new List<ChatMessage>());

            Assert.Equal("plan", response.Content);
            Assert.Equal(1, groq.Calls);
            Assert.Equal(0, anthropic.Calls);
        }

        [Fact]
        public async Task Primary_model_not_found_falls_back_to_next_provider()
        {
            var groq = FakeProvider.Throwing("Groq", NotFound());
            var anthropic = FakeProvider.Returning("Anthropic", "fallback plan");
            var service = Service(defaultProvider: "Groq", groq, anthropic);

            var response = await service.SendMessageAsync("hi", new List<ChatMessage>());

            Assert.Equal("fallback plan", response.Content);
            Assert.Equal(1, groq.Calls);
            Assert.Equal(1, anthropic.Calls);
        }

        [Fact]
        public async Task All_providers_failing_throws_controlled_error_after_one_attempt_each()
        {
            var groq = FakeProvider.Throwing("Groq", NotFound());
            var anthropic = FakeProvider.Throwing("Anthropic", NotFound());
            // Mirrors production: OpenAIProvider is an unimplemented stub.
            var openAi = FakeProvider.Throwing("OpenAI", new NotImplementedException());
            var service = Service(defaultProvider: "Groq", groq, anthropic, openAi);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendMessageAsync("hi", new List<ChatMessage>()));

            Assert.Contains("All AI providers are currently unavailable", ex.Message);
            Assert.IsType<HttpRequestException>(ex.InnerException);
            Assert.Equal(1, groq.Calls);
            Assert.Equal(1, anthropic.Calls);
        }

        public static IEnumerable<object[]> ProviderFailures() => new[]
        {
            new object[] { "429 rate limit", new HttpRequestException("Response status code does not indicate success: 429 (Too Many Requests).", null, HttpStatusCode.TooManyRequests) },
            new object[] { "401 unauthorized", new HttpRequestException("Response status code does not indicate success: 401 (Unauthorized).", null, HttpStatusCode.Unauthorized) },
            new object[] { "missing API key", new InvalidOperationException("Groq API key not configured") },
            new object[] { "provider 5xx", new HttpRequestException("Response status code does not indicate success: 503 (Service Unavailable).", null, HttpStatusCode.ServiceUnavailable) },
            new object[] { "timeout", new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.") },
            new object[] { "empty response", new Exception("Groq API returned no response") },
        };

        [Theory]
        [MemberData(nameof(ProviderFailures))]
        public async Task Provider_failure_moves_on_to_the_next_provider(string _, Exception failure)
        {
            var groq = FakeProvider.Throwing("Groq", failure);
            var anthropic = FakeProvider.Returning("Anthropic", "fallback plan");
            var service = Service(defaultProvider: "Groq", groq, anthropic);

            var response = await service.SendMessageAsync("hi", new List<ChatMessage>());

            Assert.Equal("fallback plan", response.Content);
            Assert.Equal(1, groq.Calls);
        }

        [Fact]
        public async Task Unimplemented_provider_is_skipped()
        {
            var groq = FakeProvider.Throwing("Groq", NotFound());
            var anthropic = FakeProvider.Throwing("Anthropic", NotFound());
            var openAi = FakeProvider.Throwing("OpenAI", new NotImplementedException());
            var service = Service(defaultProvider: "Groq", groq, anthropic, openAi);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendMessageAsync("hi", new List<ChatMessage>()));

            // The last real failure is reported, not the unimplemented stub.
            Assert.IsType<HttpRequestException>(ex.InnerException);
            Assert.Equal(1, openAi.Calls);
        }

        [Fact]
        public async Task Workout_plan_uses_the_configured_default_provider_with_the_workout_prompt()
        {
            var groq = FakeProvider.Returning("Groq", "plan");
            var anthropic = FakeProvider.Returning("Anthropic", "unused");
            var service = Service(defaultProvider: "Groq", groq, anthropic);

            await service.SendMessageAsync("make me a plan", new List<ChatMessage>(), "workout_plan");

            Assert.Equal(1, groq.Calls);
            Assert.Equal(0, anthropic.Calls);
            Assert.Contains("fitness coach", groq.LastSystemPrompt);
        }

        [Fact]
        public async Task Configured_default_provider_is_tried_first()
        {
            var groq = FakeProvider.Returning("Groq", "unused");
            var anthropic = FakeProvider.Returning("Anthropic", "from anthropic");
            var service = Service(defaultProvider: "Anthropic", groq, anthropic);

            var response = await service.SendMessageAsync("hi", new List<ChatMessage>());

            Assert.Equal("from anthropic", response.Content);
            Assert.Equal(0, groq.Calls);
        }

        [Fact]
        public async Task Fallback_logging_never_contains_configured_api_keys()
        {
            var logger = new CapturingLogger<AIService>();
            var groq = FakeProvider.Throwing("Groq", NotFound());
            var anthropic = FakeProvider.Throwing("Anthropic", new HttpRequestException(
                "Response status code does not indicate success: 500 (Internal Server Error).", null, HttpStatusCode.InternalServerError));
            var service = Service(defaultProvider: "Groq", logger, groq, anthropic);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendMessageAsync("hi", new List<ChatMessage>()));

            Assert.NotEmpty(logger.Messages);
            Assert.DoesNotContain(logger.Messages, m => m.Contains(FakeGroqKey) || m.Contains(FakeAnthropicKey));
        }

        // --- helpers ---------------------------------------------------------------

        private static HttpRequestException NotFound() => new(
            "Response status code does not indicate success: 404 (Not Found).", null, HttpStatusCode.NotFound);

        private static AIService Service(string defaultProvider, params FakeProvider[] providers) =>
            Service(defaultProvider, new CapturingLogger<AIService>(), providers);

        private static AIService Service(string defaultProvider, ILogger<AIService> logger, params FakeProvider[] providers)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AISettings:DefaultProvider"] = defaultProvider,
                    ["AISettings:Groq:ApiKey"] = FakeGroqKey,
                    ["AISettings:Anthropic:ApiKey"] = FakeAnthropicKey,
                })
                .Build();
            return new AIService(new FakeProviderFactory(config, providers), config, logger);
        }
    }
}
