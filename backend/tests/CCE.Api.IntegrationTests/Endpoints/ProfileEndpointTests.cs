using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class ProfileEndpointTests : IClassFixture<CceTestWebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.External.Program> _factory;

    public ProfileEndpointTests(CceTestWebApplicationFactory<CCE.Api.External.Program> factory) => _factory = factory;

    [Fact]
    public async Task Register_anonymous_with_empty_body_returns_400()
    {
        // Sub-11d: /api/users/register is back to anonymous self-service.
        // Sub-11 Phase 01 made it admin-only as a stop-gap; Sub-11d Tasks A+B
        // added IEmailSender so the temp password can be delivered via email
        // instead of returned in the response. Anonymous can call now; the
        // welcome email is the user's only credential channel.
        //
        // Empty body fails validation (GivenName/Surname/Email/MailNickname
        // required) → 400 Bad Request, not 401. Happy-path coverage lives in
        // EntraIdRegistrationTests against WireMock.
        using var client = _factory.CreateClient();
        using var content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(new Uri("/api/users/register", UriKind.Relative), content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMe_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/me", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutMe_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var resp = await client.PutAsync(new Uri("/api/me", UriKind.Relative), content);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Submit_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(new Uri("/api/users/expert-request", UriKind.Relative), content);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Status_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/me/expert-status", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
