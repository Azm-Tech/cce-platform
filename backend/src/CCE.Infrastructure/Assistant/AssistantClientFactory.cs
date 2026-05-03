using Anthropic.SDK;
using CCE.Application.Assistant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Assistant;

/// <summary>
/// Picks the assistant client implementation based on configuration.
/// Reads Assistant:Provider config + ANTHROPIC_API_KEY env-var.
///   - "stub" (default) → SmartAssistantClient
///   - "anthropic" + key → AnthropicSmartAssistantClient
///   - "anthropic" without key → falls back to stub + warn log
/// </summary>
public static class AssistantClientFactory
{
    public const string AnthropicApiKeyEnvVar = "ANTHROPIC_API_KEY";

    public static IServiceCollection AddCceAssistantClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Assistant:Provider"]?.Trim().ToLowerInvariant() ?? "stub";
        var apiKey = Environment.GetEnvironmentVariable(AnthropicApiKeyEnvVar)
                  ?? configuration[AnthropicApiKeyEnvVar];

        services.Configure<AnthropicOptions>(configuration.GetSection("Assistant:Anthropic"));

        if (provider == "anthropic" && !string.IsNullOrWhiteSpace(apiKey))
        {
            services.TryAddSingleton(_ => new AnthropicClient(new APIAuthentication(apiKey)));
            services.AddScoped<IAnthropicStreamProvider, AnthropicStreamProvider>();
            services.AddScoped<ISmartAssistantClient, AnthropicSmartAssistantClient>();
            return services;
        }

        if (provider == "anthropic" && string.IsNullOrWhiteSpace(apiKey))
        {
            // Bootstrap-time warning. ILogger isn't resolvable from this
            // IConfiguration extension; write to stderr so the operator
            // sees the fallback even when Serilog config is broken.
            Console.Error.WriteLine(
                $"warn: AssistantClientFactory: Assistant:Provider is 'anthropic' " +
                $"but {AnthropicApiKeyEnvVar} is not set. Falling back to the stub.");
        }

        services.AddScoped<ISmartAssistantClient, SmartAssistantClient>();
        return services;
    }
}
