using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CCE.Api.IntegrationTests.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class StateRepAssignmentsEndpointTests :
    IClassFixture<CceTestWebApplicationFactory<CCE.Api.Internal.Program>>,
    IClassFixture<AdminAuthFixture>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.Internal.Program> _factory;
    private readonly AdminAuthFixture _auth;

    public StateRepAssignmentsEndpointTests(
        CceTestWebApplicationFactory<CCE.Api.Internal.Program> factory,
        AdminAuthFixture auth)
    {
        _factory = factory;
        _auth = auth;
    }

    [Fact]
    public async Task Anonymous_request_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/api/admin/state-rep-assignments", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuperAdmin_request_returns_200_with_paged_shape()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/state-rep-assignments?page=1&pageSize=20", UriKind.Relative));

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
        using var body = System.Net.Http.Json.JsonContent.Create(new { userId = System.Guid.NewGuid(), countryId = System.Guid.NewGuid() });

        var resp = await client.PostAsync(new Uri("/api/admin/state-rep-assignments", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_with_unknown_user_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var body = System.Net.Http.Json.JsonContent.Create(new { userId = System.Guid.NewGuid(), countryId = System.Guid.NewGuid() });

        var resp = await client.PostAsync(new Uri("/api/admin/state-rep-assignments", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.DeleteAsync(new Uri($"/api/admin/state-rep-assignments/{System.Guid.NewGuid()}", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_with_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.DeleteAsync(new Uri($"/api/admin/state-rep-assignments/{System.Guid.NewGuid()}", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
