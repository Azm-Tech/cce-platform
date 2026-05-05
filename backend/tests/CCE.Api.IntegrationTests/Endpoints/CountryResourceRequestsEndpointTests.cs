using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CCE.Api.IntegrationTests.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class CountryResourceRequestsEndpointTests :
    IClassFixture<CceTestWebApplicationFactory<CCE.Api.Internal.Program>>,
    IClassFixture<AdminAuthFixture>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.Internal.Program> _factory;
    private readonly AdminAuthFixture _auth;

    public CountryResourceRequestsEndpointTests(
        CceTestWebApplicationFactory<CCE.Api.Internal.Program> factory,
        AdminAuthFixture auth)
    {
        _factory = factory;
        _auth = auth;
    }

    [Fact]
    public async Task Approve_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new { adminNotesAr = (string?)null, adminNotesEn = (string?)null });

        var resp = await client.PostAsync(new Uri($"/api/admin/country-resource-requests/{System.Guid.NewGuid()}/approve", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Approve_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var body = JsonContent.Create(new { adminNotesAr = (string?)null, adminNotesEn = (string?)null });

        var resp = await client.PostAsync(new Uri($"/api/admin/country-resource-requests/{System.Guid.NewGuid()}/approve", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reject_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new { adminNotesAr = "غير", adminNotesEn = "Insufficient." });

        var resp = await client.PostAsync(new Uri($"/api/admin/country-resource-requests/{System.Guid.NewGuid()}/reject", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reject_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var body = JsonContent.Create(new { adminNotesAr = "غير", adminNotesEn = "Insufficient." });

        var resp = await client.PostAsync(new Uri($"/api/admin/country-resource-requests/{System.Guid.NewGuid()}/reject", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
