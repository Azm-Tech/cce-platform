using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CCE.Api.IntegrationTests.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using JsonContent = System.Net.Http.Json.JsonContent;

namespace CCE.Api.IntegrationTests.Endpoints;

public class ResourcesEndpointTests :
    IClassFixture<WebApplicationFactory<CCE.Api.Internal.Program>>,
    IClassFixture<AdminAuthFixture>
{
    private readonly WebApplicationFactory<CCE.Api.Internal.Program> _factory;
    private readonly AdminAuthFixture _auth;

    public ResourcesEndpointTests(
        WebApplicationFactory<CCE.Api.Internal.Program> factory,
        AdminAuthFixture auth)
    {
        _factory = factory;
        _auth = auth;
    }

    [Fact]
    public async Task Anonymous_request_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/api/admin/resources", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuperAdmin_request_returns_200_with_paged_result_shape()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/resources?page=1&pageSize=20", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        doc.GetProperty("page").GetInt32().Should().Be(1);
        doc.GetProperty("pageSize").GetInt32().Should().Be(20);
        doc.GetProperty("total").GetInt64().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Post_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new
        {
            titleAr = "عنوان", titleEn = "Title",
            descriptionAr = "وصف", descriptionEn = "Description",
            resourceType = 0,
            categoryId = System.Guid.NewGuid(),
            countryId = (System.Guid?)null,
            assetFileId = System.Guid.NewGuid(),
        });

        var resp = await client.PostAsync(new Uri("/api/admin/resources", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new
        {
            titleAr = "ar", titleEn = "en",
            descriptionAr = "desc-ar", descriptionEn = "desc-en",
            resourceType = 0,
            categoryId = System.Guid.NewGuid(),
            rowVersion = System.Convert.ToBase64String(new byte[8]),
        });

        var resp = await client.PutAsync(new Uri($"/api/admin/resources/{System.Guid.NewGuid()}", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_with_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var body = JsonContent.Create(new
        {
            titleAr = "ar", titleEn = "en",
            descriptionAr = "desc-ar", descriptionEn = "desc-en",
            resourceType = 0,
            categoryId = System.Guid.NewGuid(),
            rowVersion = System.Convert.ToBase64String(new byte[8]),
        });

        var resp = await client.PutAsync(new Uri($"/api/admin/resources/{System.Guid.NewGuid()}", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Publish_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new { });

        var resp = await client.PostAsync(new Uri($"/api/admin/resources/{System.Guid.NewGuid()}/publish", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Publish_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var body = JsonContent.Create(new { });

        var resp = await client.PostAsync(new Uri($"/api/admin/resources/{System.Guid.NewGuid()}/publish", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
