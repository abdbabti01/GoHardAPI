using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using GoHardAPI.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GoHardAPI.Tests.Services
{
    /// <summary>
    /// Provider configuration: the shipped appsettings.json and each provider's code default
    /// name the same model (so a model retirement is fixed in one reviewed place), and the
    /// empty ApiKey that appsettings.json ships counts as "not configured" instead of
    /// shadowing the provider's API-key environment variable.
    /// </summary>
    public class AIProviderConfigurationTests
    {
        [Fact]
        public void Code_default_models_match_the_shipped_appsettings()
        {
            var settings = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false)
                .Build();

            Assert.Equal(GroqProvider.DefaultModel, settings["AISettings:Groq:Model"]);
            Assert.Equal(AnthropicProvider.DefaultModel, settings["AISettings:Anthropic:Model"]);
        }

        [Fact]
        public void Empty_configured_groq_key_is_treated_as_not_configured()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GROQ_API_KEY")))
            {
                return; // A real key in this environment would legitimately be used.
            }

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient());

            var ex = Assert.Throws<InvalidOperationException>(
                () => new GroqProvider(factory.Object, Config("Groq", "  "), NullLogger<GroqProvider>.Instance));
            Assert.Equal("Groq API key not configured", ex.Message);
        }

        [Fact]
        public void Empty_configured_anthropic_key_is_treated_as_not_configured()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
            {
                return; // A real key in this environment would legitimately be used.
            }

            var ex = Assert.Throws<InvalidOperationException>(
                () => new AnthropicProvider(Config("Anthropic", ""), NullLogger<AnthropicProvider>.Instance));
            Assert.Equal("Anthropic API key not configured", ex.Message);
        }

        private static IConfiguration Config(string provider, string apiKey) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [$"AISettings:{provider}:ApiKey"] = apiKey })
                .Build();
    }
}
