namespace CCE.Infrastructure.ExternalApis.Auth;

/// <summary>
/// Injects an API key as a header or query parameter.
/// </summary>
public sealed class ApiKeyAuthHandler : DelegatingHandler
{
    private readonly string _keyName;
    private readonly string _keyValue;
    private readonly string _keyLocation;

    public ApiKeyAuthHandler(string keyName, string keyValue, string keyLocation)
    {
        _keyName = keyName;
        _keyValue = keyValue;
        _keyLocation = keyLocation;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_keyLocation.Equals("Query", StringComparison.OrdinalIgnoreCase))
        {
            var uriBuilder = new UriBuilder(request.RequestUri!);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
            query[_keyName] = _keyValue;
            uriBuilder.Query = query.ToString();
            request.RequestUri = uriBuilder.Uri;
        }
        else
        {
            request.Headers.TryAddWithoutValidation(_keyName, _keyValue);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
