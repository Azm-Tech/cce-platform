using System.Net.Http.Headers;

namespace CCE.Infrastructure.ExternalApis.Auth;

/// <summary>
/// Sets an <c>Authorization: Bearer …</c> header on every request.
/// </summary>
public sealed class BearerTokenAuthHandler : DelegatingHandler
{
    private readonly string _token;

    public BearerTokenAuthHandler(string token) => _token = token;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return base.SendAsync(request, cancellationToken);
    }
}
