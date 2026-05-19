using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.ExternalApis.Auth;

/// <summary>
/// Acquires and caches an OAuth2 client-credentials token, auto-refreshing
/// before expiry. Safe for singleton use; the underlying <see cref="HttpClient"/>
/// is short-lived inside token acquisition only.
/// </summary>
public sealed class OAuth2ClientCredentialsHandler : DelegatingHandler
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _tokenUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _scope;
    private readonly bool _autoRefresh;
    private readonly ILogger<OAuth2ClientCredentialsHandler> _logger;

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public OAuth2ClientCredentialsHandler(
        string tokenUrl,
        string clientId,
        string clientSecret,
        string scope,
        bool autoRefresh,
        ILogger<OAuth2ClientCredentialsHandler>? logger = null)
    {
        _tokenUrl = tokenUrl;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _scope = scope;
        _autoRefresh = autoRefresh;
        _logger = logger ?? NullLogger<OAuth2ClientCredentialsHandler>.Instance;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_accessToken) || (_autoRefresh && DateTime.UtcNow >= _tokenExpiry.AddSeconds(-60)))
        {
            await AcquireTokenAsync(cancellationToken).ConfigureAwait(false);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task AcquireTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            var requestContent = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            };

            if (!string.IsNullOrEmpty(_scope))
            {
                requestContent["scope"] = _scope;
            }

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _tokenUrl)
            {
                Content = new FormUrlEncodedContent(requestContent)
            };

            var response = await httpClient.SendAsync(tokenRequest, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(json, s_jsonOptions);

            if (tokenResponse is not null)
            {
                _accessToken = tokenResponse.AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);
                _logger.LogDebug("OAuth2 token acquired, expires at {Expiry}", _tokenExpiry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire OAuth2 token from {TokenUrl}", _tokenUrl);
            throw;
        }
    }
}

public sealed class OAuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } = 3600;
    public string? Scope { get; set; }
}
