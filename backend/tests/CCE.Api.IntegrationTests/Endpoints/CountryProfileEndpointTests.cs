using System.Net;
using System.Net.Http.Headers;
using CCE.Api.IntegrationTests.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class CountryProfileEndpointTests :
    IClassFixture<WebApplicationFactory<CCE.Api.Internal.Program>>,
    IClassFixture<AdminAuthFixture>
{
    private readonly WebApplicationFactory<CCE.Api.Internal.Program> _factory;
    private readonly AdminAuthFixture _auth;

    public CountryProfileEndpointTests(
        WebApplicationFactory<CCE.Api.Internal.Program> factory,
        AdminAuthFixture auth)
    {
        _factory = factory;
        _auth = auth;
    }

    [Fact]
    public async Task Get_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync(
            new Uri($"/api/admin/countries/{System.Guid.NewGuid()}/profile", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_unknown_countryId_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(
            new Uri($"/api/admin/countries/{System.Guid.NewGuid()}/profile", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        using var body = System.Net.Http.Json.JsonContent.Create(new
        {
            descriptionAr = "وصف",
            descriptionEn = "Description",
            keyInitiativesAr = "مبادرات",
            keyInitiativesEn = "Initiatives",
            rowVersion = string.Empty,
        });

        var resp = await client.PutAsync(
            new Uri($"/api/admin/countries/{System.Guid.NewGuid()}/profile", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
