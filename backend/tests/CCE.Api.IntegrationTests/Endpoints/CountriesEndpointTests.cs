using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CCE.Api.IntegrationTests.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class CountriesEndpointTests :
    IClassFixture<CceTestWebApplicationFactory<CCE.Api.Internal.Program>>,
    IClassFixture<AdminAuthFixture>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.Internal.Program> _factory;
    private readonly AdminAuthFixture _auth;

    public CountriesEndpointTests(
        CceTestWebApplicationFactory<CCE.Api.Internal.Program> factory,
        AdminAuthFixture auth)
    {
        _factory = factory;
        _auth = auth;
    }

    [Fact]
    public async Task List_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/api/admin/countries", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_SuperAdmin_returns_200_with_paged_result_shape()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/countries?page=1&pageSize=20", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        doc.GetProperty("page").GetInt32().Should().Be(1);
        doc.GetProperty("pageSize").GetInt32().Should().Be(20);
        doc.GetProperty("total").GetInt64().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetById_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri($"/api/admin/countries/{System.Guid.NewGuid()}", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri($"/api/admin/countries/{System.Guid.NewGuid()}", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new
        {
            nameAr = "أمريكا",
            nameEn = "United States",
            regionAr = "أمريكا الشمالية",
            regionEn = "North America",
            isActive = true,
        });

        var resp = await client.PutAsync(new Uri($"/api/admin/countries/{System.Guid.NewGuid()}", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var body = JsonContent.Create(new
        {
            nameAr = "أمريكا",
            nameEn = "United States",
            regionAr = "أمريكا الشمالية",
            regionEn = "North America",
            isActive = true,
        });

        var resp = await client.PutAsync(new Uri($"/api/admin/countries/{System.Guid.NewGuid()}", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
