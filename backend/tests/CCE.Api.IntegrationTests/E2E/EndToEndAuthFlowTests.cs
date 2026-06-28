using System.Net;
using System.Net.Http.Headers;
using CCE.Api.Internal;

namespace CCE.Api.IntegrationTests.E2E;

public class EndToEndAuthFlowTests : IClassFixture<CceTestWebApplicationFactory<CCE.Api.Internal.Program>>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.Internal.Program> _factory;

    public EndToEndAuthFlowTests(CceTestWebApplicationFactory<CCE.Api.Internal.Program> factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_health_returns_200()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/health", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Authenticated_endpoint_accepts_test_bearer_token()
    {
        // Sub-11 Phase 04 deleted the Keycloak token-endpoint dependency. The
        // CceTestWebApplicationFactory wires TestAuthHandler in place of M.I.W's
        // JwtBearer so a literal `Bearer <role-name>` synthesizes a principal
        // with that role. This test proves the end-to-end auth-then-authorize
        // flow still works: bearer header → AuthN → claims transformer →
        // permission policy → endpoint handler.
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "cce-admin");

        var resp = await client.GetAsync(new Uri("/auth/echo", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ready_returns_200_when_dependencies_up()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Anonymous_auth_endpoint_returns_401()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/auth/echo", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
