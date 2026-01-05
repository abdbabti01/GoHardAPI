namespace GoHardAPI.Services.AI
{
    /// <summary>
    /// Factory for creating AI provider instances based on configuration
    /// Allows switching between Claude, GPT, Gemini by changing config
    /// </summary>
    public class AIProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIProviderFactory> _logger;

        public AIProviderFactory(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AIProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get AI provider instance based on configuration or override
        /// </summary>
        /// <param name="providerName">Optional provider name override (null uses default from config)</param>
        /// <returns>AI provider instance</returns>
        /// <exception cref="ArgumentException">Unknown provider name</exception>
        public IAIProvider GetProvider(string? providerName = null)
        {
            var provider = providerName ?? _configuration["AISettings:DefaultProvider"] ?? "Anthropic";

            _logger.LogInformation($"Getting AI provider: {provider}");

            return provider.ToLower() switch
            {
                "anthropic" => _serviceProvider.GetRequiredService<AnthropicProvider>(),
                "openai" => _serviceProvider.GetRequiredService<OpenAIProvider>(),
                "gemini" => throw new NotImplementedException("Gemini provider not yet implemented. Use 'Anthropic' or 'OpenAI'."),
                _ => throw new ArgumentException($"Unknown AI provider: {provider}. Supported providers: Anthropic, OpenAI")
            };
        }
    }
}
