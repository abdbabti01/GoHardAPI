using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GoHardAPI.Models;
using GoHardAPI.Services.AI;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    /// <summary>
    /// <see cref="GroqProvider"/> against a stubbed HTTP endpoint: the configured model is
    /// what gets sent, a retired model's 404 model_not_found surfaces as a status-coded
    /// failure the fallback loop can act on, and the API key never reaches the logs.
    /// </summary>
    public class GroqProviderTests
    {
        private const string FakeKey = "gsk_test_not_a_real_key_333";
        private const string ConfiguredModel = "test-configured-model";

        // Shape of Groq's real response for a retired/unknown model (key-free).
        private const string ModelNotFoundBody =
            "{\"error\":{\"message\":\"The model `test-configured-model` does not exist or you do not have access to it.\"," +
            "\"type\":\"invalid_request_error\",\"code\":\"model_not_found\"}}";

        private const string SuccessBody =
            "{\"id\":\"x\",\"model\":\"test-configured-model\"," +
            "\"choices\":[{\"index\":0,\"finish_reason\":\"stop\",\"message\":{\"role\":\"assistant\",\"content\":\"hello\"}}]," +
            "\"usage\":{\"prompt_tokens\":3,\"completion_tokens\":1}}";

        [Fact]
        public async Task Model_not_found_throws_status_coded_error_and_never_logs_the_key()
        {
            var handler = new StubHandler(HttpStatusCode.NotFound, ModelNotFoundBody);
            var logger = new CapturingLogger<GroqProvider>();
            var provider = Provider(handler, logger);

            var ex = await Assert.ThrowsAsync<HttpRequestException>(
                () => provider.SendMessageAsync("hi", new List<ChatMessage>(), "system"));

            Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
            Assert.Contains(logger.Messages, m => m.Contains("model_not_found"));
            Assert.DoesNotContain(logger.Messages, m => m.Contains(FakeKey));
        }

        [Fact]
        public async Task Sends_the_configured_model_and_returns_the_message_content()
        {
            var handler = new StubHandler(HttpStatusCode.OK, SuccessBody);
            var provider = Provider(handler, new CapturingLogger<GroqProvider>());

            var response = await provider.SendMessageAsync("hi", new List<ChatMessage>(), "system");

            Assert.Equal("hello", response.Content);
            Assert.Equal("Groq", response.Provider);
            using var sent = JsonDocument.Parse(handler.LastRequestBody!);
            Assert.Equal(ConfiguredModel, sent.RootElement.GetProperty("model").GetString());
        }

        [Fact]
        public async Task Unconfigured_model_falls_back_to_the_provider_default()
        {
            var handler = new StubHandler(HttpStatusCode.OK, SuccessBody);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["AISettings:Groq:ApiKey"] = FakeKey })
                .Build();
            var provider = new GroqProvider(factory.Object, config, new CapturingLogger<GroqProvider>());

            await provider.SendMessageAsync("hi", new List<ChatMessage>(), "system");

            using var sent = JsonDocument.Parse(handler.LastRequestBody!);
            Assert.Equal(GroqProvider.DefaultModel, sent.RootElement.GetProperty("model").GetString());
        }

        [Theory]
        // Reasoning models (gpt-oss) spend max_tokens on reasoning first: a reply cut off
        // at the limit, or one with no final content, must fail over, not be saved.
        [InlineData("{\"id\":\"x\",\"choices\":[{\"index\":0,\"finish_reason\":\"length\",\"message\":{\"role\":\"assistant\",\"content\":\"{\\\"programName\\\": \\\"Tru\"}}]}")]
        [InlineData("{\"id\":\"x\",\"choices\":[{\"index\":0,\"finish_reason\":\"length\",\"message\":{\"role\":\"assistant\",\"content\":null,\"reasoning\":\"thinking...\"}}]}")]
        [InlineData("{\"id\":\"x\",\"choices\":[{\"index\":0,\"finish_reason\":\"stop\",\"message\":{\"role\":\"assistant\",\"content\":\"  \"}}]}")]
        public async Task Truncated_or_empty_content_is_a_failure(string body)
        {
            var handler = new StubHandler(HttpStatusCode.OK, body);
            var provider = Provider(handler, new CapturingLogger<GroqProvider>());

            await Assert.ThrowsAsync<Exception>(
                () => provider.SendMessageAsync("hi", new List<ChatMessage>(), "system"));
        }

        [Fact]
        public async Task Empty_choices_is_a_failure_not_an_empty_answer()
        {
            var handler = new StubHandler(HttpStatusCode.OK, "{\"id\":\"x\",\"choices\":[]}");
            var provider = Provider(handler, new CapturingLogger<GroqProvider>());

            await Assert.ThrowsAsync<Exception>(
                () => provider.SendMessageAsync("hi", new List<ChatMessage>(), "system"));
        }

        private static GroqProvider Provider(StubHandler handler, CapturingLogger<GroqProvider> logger)
        {
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AISettings:Groq:ApiKey"] = FakeKey,
                    ["AISettings:Groq:Model"] = ConfiguredModel,
                })
                .Build();
            return new GroqProvider(factory.Object, config, logger);
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;

            public StubHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }

            public string? LastRequestBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json"),
                };
            }
        }
    }
}
