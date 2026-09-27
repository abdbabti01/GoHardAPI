using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoHardAPI.Models;
using GoHardAPI.Services.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoHardAPI.Tests.Services
{
    /// <summary>Resolves providers by name; an unlisted provider behaves as unconfigured.</summary>
    internal sealed class FakeProviderFactory : AIProviderFactory
    {
        private readonly Dictionary<string, FakeProvider> _providers;

        public FakeProviderFactory(IConfiguration config, IEnumerable<FakeProvider> providers)
            : base(new ServiceCollection().BuildServiceProvider(), config, NullLogger<AIProviderFactory>.Instance)
        {
            _providers = providers.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);
        }

        public override IAIProvider GetProvider(string? providerName = null) =>
            _providers.TryGetValue(providerName ?? "", out var provider)
                ? provider
                : throw new InvalidOperationException($"{providerName} API key not configured");
    }

    /// <summary>An <see cref="IAIProvider"/> that returns fixed content or throws, and counts calls.</summary>
    internal sealed class FakeProvider : IAIProvider
    {
        private readonly Func<AIResponse> _behavior;

        private FakeProvider(string name, Func<AIResponse> behavior)
        {
            ProviderName = name;
            _behavior = behavior;
        }

        public static FakeProvider Returning(string name, string content) =>
            new(name, () => new AIResponse { Content = content, Provider = name });

        public static FakeProvider Throwing(string name, Exception failure) =>
            new(name, () => throw failure);

        public string ProviderName { get; }
        public int Calls { get; private set; }
        public string? LastSystemPrompt { get; private set; }

        public Task<AIResponse> SendMessageAsync(string userMessage, List<ChatMessage> conversationHistory, string systemPrompt)
        {
            Calls++;
            LastSystemPrompt = systemPrompt;
            return Task.FromResult(_behavior());
        }

        public IAsyncEnumerable<string> StreamMessageAsync(string userMessage, List<ChatMessage> conversationHistory, string systemPrompt) =>
            throw new NotImplementedException();
    }

    /// <summary>Collects every formatted log message, including exception text.</summary>
    internal sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception) + (exception == null ? "" : " | " + exception));
        }
    }
}
