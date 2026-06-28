using System.Net;
using System.Text.Json;
using CCE.Api.Common.Middleware;
using CCE.Application.Localization;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static IHost BuildHost(RequestDelegate handler, ILocalizationService? localization = null) =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<CorrelationIdMiddleware>();
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.Run(handler);
                });
                if (localization is not null)
                {
                    web.ConfigureTestServices(s => s.AddSingleton(localization));
                }
            })
            .Start();

    [Fact]
    public async Task Returns_500_response_on_unhandled_exception()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("boom"));
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.GetProperty("code").GetString().Should().Be("ERR900");
        doc.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Returns_400_response_on_validation_exception()
    {
        var failures = new List<ValidationFailure>
        {
            new("Name", "must not be empty"),
            new("Age", "must be positive")
        };
        using var host = BuildHost(_ => throw new ValidationException(failures));
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.GetProperty("code").GetString().Should().Be("VAL001");
        doc.GetProperty("errors").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Includes_trace_id_in_response_body()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("x"));
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Returns_401_response_on_unauthorized_access_exception()
    {
        using var host = BuildHost(_ => throw new UnauthorizedAccessException("nope"));
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.GetProperty("code").GetString().Should().Be("ERR901");
    }

    [Fact]
    public async Task Message_language_follows_Accept_Language_header()
    {
        var localization = new StubLocalization();
        using var host = BuildHost(_ => throw new UnauthorizedAccessException(), localization);
        var client = host.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("en"));
        var resp = await client.SendAsync(request);

        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("message").GetString().Should().Be("en:UNAUTHORIZED_ACCESS");
    }

    [Fact]
    public async Task Includes_correlation_id_in_response_body()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("x"));
        var client = host.GetTestClient();

        var correlationId = "abc-123-correlation";
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("correlationId").GetString().Should().Be(correlationId);
    }

    private sealed class StubLocalization : ILocalizationService
    {
        public string GetString(string key, string? culture = null)
            => culture == "en" ? $"en:{key}" : $"ar:{key}";

        public string GetStringOrDefault(string key, string defaultMessage, string? culture = null)
            => GetString(key, culture);

        public LocalizedMessage GetLocalizedMessage(string key)
            => new($"ar:{key}", $"en:{key}");
    }
}
