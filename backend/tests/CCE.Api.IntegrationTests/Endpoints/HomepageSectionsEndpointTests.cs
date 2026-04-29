using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CCE.Api.IntegrationTests.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class HomepageSectionsEndpointTests :
    IClassFixture<WebApplicationFactory<CCE.Api.Internal.Program>>,
    IClassFixture<AdminAuthFixture>
{
    private readonly WebApplicationFactory<CCE.Api.Internal.Program> _factory;
    private readonly AdminAuthFixture _auth;

    public HomepageSectionsEndpointTests(
        WebApplicationFactory<CCE.Api.Internal.Program> factory,
        AdminAuthFixture auth)
    {
        _factory = factory;
        _auth = auth;
    }

    [Fact]
    public async Task List_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/api/admin/homepage-sections", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_SuperAdmin_returns_200_with_array_shape()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/homepage-sections", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Post_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new
        {
            sectionType = 0,
            orderIndex = 0,
            contentAr = "ar",
            contentEn = "en",
        });

        var resp = await client.PostAsync(new Uri("/api/admin/homepage-sections", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new
        {
            contentAr = "ar",
            contentEn = "en",
            isActive = true,
        });

        var resp = await client.PutAsync(new Uri($"/api/admin/homepage-sections/{System.Guid.NewGuid()}", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var body = JsonContent.Create(new
        {
            contentAr = "ar",
            contentEn = "en",
            isActive = true,
        });

        var resp = await client.PutAsync(new Uri($"/api/admin/homepage-sections/{System.Guid.NewGuid()}", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.DeleteAsync(new Uri($"/api/admin/homepage-sections/{System.Guid.NewGuid()}", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.DeleteAsync(new Uri($"/api/admin/homepage-sections/{System.Guid.NewGuid()}", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reorder_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new
        {
            assignments = new[] { new { id = System.Guid.NewGuid(), orderIndex = 0 } }
        });

        var resp = await client.PostAsync(new Uri("/api/admin/homepage-sections/reorder", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
