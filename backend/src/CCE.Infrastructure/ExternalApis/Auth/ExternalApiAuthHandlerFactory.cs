using CCE.Application.ExternalApis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.ExternalApis.Auth;

/// <summary>
/// Factory that creates the correct <see cref="DelegatingHandler"/> for an
/// external API based on its <see cref="ExternalApiAuthConfig"/>.
/// </summary>
public static class ExternalApiAuthHandlerFactory
{
    public static DelegatingHandler? Create(ExternalApiAuthConfig? authConfig, ILoggerFactory? loggerFactory = null)
    {
        if (authConfig is null || authConfig.Type == ExternalApiAuthType.None)
        {
            return null;
        }

        var logger = loggerFactory ?? NullLoggerFactory.Instance;

        return authConfig.Type switch
        {
            ExternalApiAuthType.ApiKey => new ApiKeyAuthHandler(authConfig.KeyName, authConfig.Value, authConfig.KeyLocation),
            ExternalApiAuthType.Bearer => new BearerTokenAuthHandler(authConfig.Token),
            ExternalApiAuthType.Basic => new BasicAuthHandler(authConfig.ClientId, authConfig.ClientSecret),
            ExternalApiAuthType.OAuth2 => new OAuth2ClientCredentialsHandler(
                authConfig.TokenUrl,
                authConfig.ClientId,
                authConfig.ClientSecret,
                authConfig.Scope,
                authConfig.AutoRefresh,
                logger.CreateLogger<OAuth2ClientCredentialsHandler>()),
            _ => null
        };
    }
}
