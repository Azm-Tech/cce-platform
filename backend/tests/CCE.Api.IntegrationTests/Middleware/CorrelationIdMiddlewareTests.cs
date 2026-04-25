using CCE.Api.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private static IHost BuildTestHost() =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<CorrelationIdMiddleware>();
                    app.Run(async ctx =>
                    {
                        await ctx.Response.WriteAsync(ctx.Items[CorrelationIdMiddleware.ItemKey]?.ToString() ?? "missing");
                    });
                });
            })
            .Start();

    [Fact]
    public async Task Generates_correlation_id_when_header_absent()
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        var id = values!.Single();
        Guid.TryParse(id, out _).Should().BeTrue();
        (await resp.Content.ReadAsStringAsync()).Should().Be(id);
    }

    [Fact]
    public async Task Echoes_provided_correlation_id()
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();
        var sent = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", sent);

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.Headers.GetValues("X-Correlation-Id").Single().Should().Be(sent);
        (await resp.Content.ReadAsStringAsync()).Should().Be(sent);
    }
}
