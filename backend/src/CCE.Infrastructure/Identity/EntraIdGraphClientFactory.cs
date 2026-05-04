using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace CCE.Infrastructure.Identity;

/// <summary>
/// Sub-11 Phase 01 — builds <see cref="GraphServiceClient"/> instances
/// backed by an app-only <see cref="ClientSecretCredential"/>. Single
/// composition root so test code can subclass and inject a fake
/// pointing at WireMock instead of <c>graph.microsoft.com</c>.
/// </summary>
public class EntraIdGraphClientFactory
{
    private static readonly string[] DefaultGraphScopes = { "https://graph.microsoft.com/.default" };

    private readonly IOptions<EntraIdOptions> _options;

    public EntraIdGraphClientFactory(IOptions<EntraIdOptions> options) => _options = options;

    /// <summary>
    /// Creates a new <see cref="GraphServiceClient"/>. Virtual to allow
    /// test fakes to override and inject a WireMock-pointed HttpClient.
    /// </summary>
    public virtual GraphServiceClient Create()
    {
        var opts = _options.Value;
        var credential = new ClientSecretCredential(
            tenantId: opts.GraphTenantId,
            clientId: opts.ClientId,
            clientSecret: opts.ClientSecret);
        return new GraphServiceClient(credential, DefaultGraphScopes);
    }
}
