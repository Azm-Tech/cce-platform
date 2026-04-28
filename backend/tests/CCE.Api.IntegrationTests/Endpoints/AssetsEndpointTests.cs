using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

// TODO(Phase-3.2): The full 201-path integration test (authenticated SuperAdmin upload through
// LocalFileStorage + ClamAV stub) requires the Keycloak token harness and a ClamAV mock to be
// wired into the WebApplicationFactory. Deferred to a later sub-project once that infrastructure
// is available. For now only the anonymous-401 gate is covered here.

namespace CCE.Api.IntegrationTests.Endpoints;

public class AssetsEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.Internal.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.Internal.Program> _factory;

    public AssetsEndpointTests(WebApplicationFactory<CCE.Api.Internal.Program> factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_request_returns_401()
    {
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        content.Add(fileContent, "file", "x.bin");

        var resp = await client.PostAsync(new Uri("/api/admin/assets", UriKind.Relative), content);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
