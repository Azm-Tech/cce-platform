using CCE.Application.Assistant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Infrastructure.Assistant;

/// <summary>
/// Picks the assistant client implementation based on configuration.
/// Phase 02 Task 2.4 flips this to honour Assistant:Provider.
/// </summary>
public static class AssistantClientFactory
{
    public static IServiceCollection AddCceAssistantClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Phase 02 Task 2.4: read Assistant:Provider + ANTHROPIC_API_KEY
        // and register AnthropicSmartAssistantClient when both are set.
        // Phase 00: always register the stub.
        _ = configuration;
        services.AddScoped<ISmartAssistantClient, SmartAssistantClient>();
        return services;
    }
}
