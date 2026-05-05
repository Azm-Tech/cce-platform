using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CCE.Api.IntegrationTests.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class NotificationTemplatesEndpointTests :
    IClassFixture<CceTestWebApplicationFactory<CCE.Api.Internal.Program>>,
    IClassFixture<AdminAuthFixture>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.Internal.Program> _factory;
    private readonly AdminAuthFixture _auth;

    public NotificationTemplatesEndpointTests(
        CceTestWebApplicationFactory<CCE.Api.Internal.Program> factory,
        AdminAuthFixture auth)
    {
        _factory = factory;
        _auth = auth;
    }

    [Fact]
    public async Task GetList_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/api/admin/notification-templates", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri($"/api/admin/notification-templates/{System.Guid.NewGuid()}", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri($"/api/admin/notification-templates/{System.Guid.NewGuid()}", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new
        {
            code = "TEST_CODE",
            subjectAr = "عنوان", subjectEn = "Subject",
            bodyAr = "جسم", bodyEn = "Body",
            channel = 0,
            variableSchemaJson = "{}",
        });

        var resp = await client.PostAsync(new Uri("/api/admin/notification-templates", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var body = JsonContent.Create(new
        {
            subjectAr = "عنوان", subjectEn = "Subject",
            bodyAr = "جسم", bodyEn = "Body",
            isActive = true,
        });

        var resp = await client.PutAsync(new Uri($"/api/admin/notification-templates/{System.Guid.NewGuid()}", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_with_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var body = JsonContent.Create(new
        {
            subjectAr = "عنوان", subjectEn = "Subject",
            bodyAr = "جسم", bodyEn = "Body",
            isActive = true,
        });

        var resp = await client.PutAsync(new Uri($"/api/admin/notification-templates/{System.Guid.NewGuid()}", UriKind.Relative), body);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
