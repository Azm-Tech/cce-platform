using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Api.Common.Auth;

/// <summary>
/// Stateless helper that calls the Keycloak token endpoint to rotate a refresh token.
/// Extracted from <see cref="BffSessionMiddleware"/> so it can be injected independently
/// (the middleware itself cannot be resolved from DI because it requires <see cref="Microsoft.AspNetCore.Http.RequestDelegate"/>).
/// </summary>
public sealed class BffTokenRefresher
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptions<BffOptions> _opts;
    private readonly ILogger<BffTokenRefresher> _logger;

    public BffTokenRefresher(
        IHttpClientFactory httpFactory,
        IOptions<BffOptions> opts,
        ILogger<BffTokenRefresher> logger)
    {
        _httpFactory = httpFactory;
        _opts = opts;
        _logger = logger;
    }

    public async Task<BffSession?> TryRefreshAsync(BffSession session, System.Threading.CancellationToken ct)
    {
        var opts = _opts.Value;
        var http = _httpFactory.CreateClient("keycloak-bff");
        using var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", opts.KeycloakClientId),
            new KeyValuePair<string, string>("client_secret", opts.KeycloakClientSecret),
            new KeyValuePair<string, string>("refresh_token", session.RefreshToken),
        });
        var url = $"{opts.KeycloakBaseUrl}/realms/{opts.KeycloakRealm}/protocol/openid-connect/token";
        try
        {
            using var resp = await http.PostAsync(new System.Uri(url), form, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }
            var tokens = await resp.Content.ReadFromJsonAsync<BffTokenResponse>(cancellationToken: ct).ConfigureAwait(false);
            if (tokens is null)
            {
                return null;
            }
            return new BffSession(
                tokens.AccessToken,
                tokens.RefreshToken,
                System.DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn));
        }
        catch (System.Exception ex) when (ex is System.Net.Http.HttpRequestException or System.Threading.Tasks.TaskCanceledException)
        {
            _logger.LogWarning(ex, "BFF token refresh failed");
            return null;
        }
    }
}
