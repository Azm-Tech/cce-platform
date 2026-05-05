using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class SurveysEndpointTests : IClassFixture<CceTestWebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.External.Program> _factory;

    public SurveysEndpointTests(CceTestWebApplicationFactory<CCE.Api.External.Program> factory)
        => _factory = factory;

    [Fact]
    public async Task Submit_rating_is_publicly_reachable()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/surveys/service-rating",
            new { rating = 5, page = "/home", locale = "en" });

        resp.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        resp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Submit_valid_rating_returns_201_with_id()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/surveys/service-rating",
            new { rating = 4, commentEn = "Good site", page = "/news", locale = "en" });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<IdBody>();
        body!.Id.Should().NotBe(System.Guid.Empty);
    }

    [Fact]
    public async Task Submit_rating_out_of_range_returns_400()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/surveys/service-rating",
            new { rating = 6, page = "/home", locale = "en" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_rating_with_invalid_locale_returns_400()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/surveys/service-rating",
            new { rating = 3, page = "/home", locale = "fr" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Submit_rating_with_arabic_comments_returns_201()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/surveys/service-rating",
            new { rating = 3, commentAr = "موقع جيد", page = "/home", locale = "ar" });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private sealed record IdBody(System.Guid Id);
}
