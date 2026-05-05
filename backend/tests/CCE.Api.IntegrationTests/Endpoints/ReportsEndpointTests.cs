using System.Net;
using System.Net.Http.Headers;
using CCE.Api.IntegrationTests.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class ReportsEndpointTests :
    IClassFixture<CceTestWebApplicationFactory<CCE.Api.Internal.Program>>,
    IClassFixture<AdminAuthFixture>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.Internal.Program> _factory;
    private readonly AdminAuthFixture _auth;

    public ReportsEndpointTests(
        CceTestWebApplicationFactory<CCE.Api.Internal.Program> factory,
        AdminAuthFixture auth)
    {
        _factory = factory;
        _auth = auth;
    }

    [Fact]
    public async Task UsersRegistrations_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/admin/reports/users-registrations.csv", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UsersRegistrations_super_admin_returns_csv()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/reports/users-registrations.csv", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var body = await resp.Content.ReadAsStringAsync();
        // First line should be the CSV header
        body.Split('\n')[0].Should().Contain("Id");
        body.Split('\n')[0].Should().Contain("Email");
    }

    [Fact]
    public async Task Experts_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/admin/reports/experts.csv", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Experts_super_admin_returns_csv()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/reports/experts.csv", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var body = await resp.Content.ReadAsStringAsync();
        body.Split('\n')[0].Should().Contain("Id");
        body.Split('\n')[0].Should().Contain("UserId");
    }

    [Fact]
    public async Task SatisfactionSurvey_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/admin/reports/satisfaction-survey.csv", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SatisfactionSurvey_super_admin_returns_csv()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/reports/satisfaction-survey.csv", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var body = await resp.Content.ReadAsStringAsync();
        body.Split('\n')[0].Should().Contain("Id");
        body.Split('\n')[0].Should().Contain("Rating");
    }

    [Fact]
    public async Task CommunityPosts_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/admin/reports/community-posts.csv", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CommunityPosts_super_admin_returns_csv()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/reports/community-posts.csv", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var body = await resp.Content.ReadAsStringAsync();
        body.Split('\n')[0].Should().Contain("Id");
        body.Split('\n')[0].Should().Contain("AuthorId");
    }

    [Fact]
    public async Task News_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/admin/reports/news.csv", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task News_super_admin_returns_csv()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/reports/news.csv", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var body = await resp.Content.ReadAsStringAsync();
        body.Split('\n')[0].Should().Contain("Id");
        body.Split('\n')[0].Should().Contain("Slug");
    }

    [Fact]
    public async Task Events_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/admin/reports/events.csv", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Events_super_admin_returns_csv()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/reports/events.csv", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var body = await resp.Content.ReadAsStringAsync();
        body.Split('\n')[0].Should().Contain("Id");
        body.Split('\n')[0].Should().Contain("ICalUid");
    }

    [Fact]
    public async Task Resources_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/admin/reports/resources.csv", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Resources_super_admin_returns_csv()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/reports/resources.csv", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var body = await resp.Content.ReadAsStringAsync();
        body.Split('\n')[0].Should().Contain("Id");
        body.Split('\n')[0].Should().Contain("ResourceType");
    }

    [Fact]
    public async Task CountryProfiles_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/admin/reports/country-profiles.csv", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CountryProfiles_super_admin_returns_csv()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);

        var resp = await client.GetAsync(new Uri("/api/admin/reports/country-profiles.csv", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var body = await resp.Content.ReadAsStringAsync();
        body.Split('\n')[0].Should().Contain("CountryId");
        body.Split('\n')[0].Should().Contain("IsoAlpha3");
    }
}
