using System.Net;
using CCE.Api.Common.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.RateLimiting;

public class RateLimiterTests
{
    // Build a host with a tight 3-per-window limit for deterministic tests.
    private static IHost BuildTestHost() =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(s => s.AddCceRateLimiter(testLimit: 3));
                web.Configure(app =>
                {
                    app.UseRateLimiter();
                    app.MapWhen(_ => true, branch =>
                    {
                        branch.Run(c => c.Response.WriteAsync("ok"));
                    });
                });
            })
            .Start();

    [Fact]
    public async Task Allows_requests_under_the_limit()
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();

        for (var i = 0; i < 3; i++)
        {
            var resp = await client.GetAsync(new Uri("/", UriKind.Relative));
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Returns_429_after_exceeding_the_limit()
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();

        for (var i = 0; i < 3; i++)
        {
            await client.GetAsync(new Uri("/", UriKind.Relative));
        }
        var over = await client.GetAsync(new Uri("/", UriKind.Relative));

        over.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
