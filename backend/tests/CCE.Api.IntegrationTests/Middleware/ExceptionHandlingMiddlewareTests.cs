using System.Net;
using System.Text.Json;
using CCE.Api.Common.Middleware;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static IHost BuildHost(RequestDelegate handler) =>
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
}
