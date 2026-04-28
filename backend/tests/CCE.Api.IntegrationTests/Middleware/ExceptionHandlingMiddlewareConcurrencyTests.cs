using System.Net;
using System.Text.Json;
using CCE.Api.Common.Middleware;
using CCE.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Middleware;

public class ExceptionHandlingMiddlewareConcurrencyTests
{
    private static IHost BuildHost(Exception toThrow) =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<CorrelationIdMiddleware>();
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.Run(_ => throw toThrow);
                });
            })
            .Start();

    [Fact]
    public async Task ConcurrencyException_returns_409_problem_details()
    {
        using var host = BuildHost(new ConcurrencyException("test conflict"));
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("status").GetInt32().Should().Be(409);
        doc.GetProperty("title").GetString().Should().Be("Concurrent edit");
        doc.GetProperty("type").GetString().Should().Be("https://cce.moenergy.gov.sa/problems/concurrency");
        doc.GetProperty("detail").GetString().Should().Be("test conflict");
    }

    [Fact]
    public async Task DuplicateException_returns_409_problem_details()
    {
        using var host = BuildHost(new DuplicateException("dup conflict"));
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("status").GetInt32().Should().Be(409);
        doc.GetProperty("title").GetString().Should().Be("Duplicate value");
        doc.GetProperty("type").GetString().Should().Be("https://cce.moenergy.gov.sa/problems/duplicate");
        doc.GetProperty("detail").GetString().Should().Be("dup conflict");
    }

    [Fact]
    public async Task DomainException_returns_400_problem_details()
    {
        using var host = BuildHost(new DomainException("invariant violated"));
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("status").GetInt32().Should().Be(400);
        doc.GetProperty("title").GetString().Should().Be("Invariant violated");
        doc.GetProperty("type").GetString().Should().Be("https://cce.moenergy.gov.sa/problems/invariant");
    }
}
