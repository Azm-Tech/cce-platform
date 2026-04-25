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
    public async Task Returns_500_problem_details_on_unhandled_exception()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("boom"));
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("status").GetInt32().Should().Be(500);
        doc.GetProperty("correlationId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Returns_400_problem_details_on_validation_exception()
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
        doc.GetProperty("status").GetInt32().Should().Be(400);
        doc.GetProperty("errors").GetProperty("Name").EnumerateArray().First().GetString().Should().Be("must not be empty");
        doc.GetProperty("errors").GetProperty("Age").EnumerateArray().First().GetString().Should().Be("must be positive");
    }

    [Fact]
    public async Task Includes_correlation_id_in_response_body()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("x"));
        var client = host.GetTestClient();
        var sent = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", sent);

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("correlationId").GetString().Should().Be(sent);
    }
}
