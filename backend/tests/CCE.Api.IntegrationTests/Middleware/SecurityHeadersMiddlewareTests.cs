using CCE.Api.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    // Synchronous helper — keeps `.Start()` out of async test bodies (CA1849).
    private static IHost BuildTestHost() =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<SecurityHeadersMiddleware>();
                    app.Run(c => c.Response.WriteAsync("ok"));
                });
            })
            .Start();

    [Fact]
    public async Task Adds_baseline_security_headers()
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
        resp.Headers.GetValues("Referrer-Policy").Single().Should().Be("strict-origin-when-cross-origin");
        resp.Headers.GetValues("Permissions-Policy").Single().Should().Contain("camera=()");
        resp.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("default-src 'self'");
        resp.Headers.Contains("Strict-Transport-Security").Should().BeFalse(); // off by default in dev
    }
}
