using CCE.Application.ExternalApis;
using CCE.Infrastructure.ExternalApis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Refit;

namespace CCE.Infrastructure.ExternalApis;

/// <summary>
/// Extensions for registering Refit-based external API clients with
/// per-client auth handlers and standard resilience policies.
/// </summary>
public static class ExternalApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers a Refit client <typeparamref name="TClient"/> whose base URL,
    /// timeout and auth scheme are read from <c>ExternalApis:{apiName}</c>.
    /// </summary>
    public static IServiceCollection AddExternalApiClient<TClient>(
        this IServiceCollection services,
        string apiName)
        where TClient : class
    {
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                })
        };

        services.AddRefitClient<TClient>(refitSettings)
            .ConfigureHttpClient((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>()
                    .GetSection($"ExternalApis:{apiName}")
                    .Get<ExternalApiClientConfig>();

                if (config is not null && !string.IsNullOrWhiteSpace(config.BaseUrl))
                {
                    client.BaseAddress = new Uri(config.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 30);
                }
            })
            .AddHttpMessageHandler(sp =>
            {
                var authConfig = sp.GetRequiredService<IConfiguration>()
                    .GetSection($"ExternalApis:{apiName}:Auth")
                    .Get<ExternalApiAuthConfig>();

                var handler = ExternalApiAuthHandlerFactory.Create(authConfig, sp.GetService<ILoggerFactory>());
                return handler ?? new NoOpDelegatingHandler();
            })
            .AddStandardResilienceHandler();

        return services;
    }
}
