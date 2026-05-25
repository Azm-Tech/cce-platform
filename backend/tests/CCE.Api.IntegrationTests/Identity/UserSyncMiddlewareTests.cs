using System.Net;
using System.Security.Claims;
using CCE.Api.Common.Identity;
using CCE.Application.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Identity;

public class UserSyncMiddlewareTests
{
    [Fact]
    public async Task First_authenticated_request_calls_sync_service()
    {
        var sync = Substitute.For<IUserSyncRepository>();
        var sub = Guid.NewGuid();
        using var host = BuildHost(sync, authenticated: true, sub: sub.ToString());
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        await sync.Received(1).EnsureUserExistsAsync(
            sub,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Repeat_request_uses_cache_and_does_not_call_sync_service_again()
    {
        var sync = Substitute.For<IUserSyncRepository>();
        using var host = BuildHost(sync, authenticated: true, sub: Guid.NewGuid().ToString());
        var client = host.GetTestClient();

        await client.GetAsync(new Uri("/", UriKind.Relative));
        await client.GetAsync(new Uri("/", UriKind.Relative));
        await client.GetAsync(new Uri("/", UriKind.Relative));

        await sync.Received(1).EnsureUserExistsAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Anonymous_request_does_not_invoke_sync_service()
    {
        var sync = Substitute.For<IUserSyncRepository>();
        using var host = BuildHost(sync, authenticated: false);
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        await sync.DidNotReceiveWithAnyArgs().EnsureUserExistsAsync(
            default, default!, default!, default!, default);
    }

    [Fact]
    public async Task Authenticated_request_with_unparseable_sub_does_not_invoke_sync_service()
    {
        var sync = Substitute.For<IUserSyncRepository>();
        using var host = BuildHost(sync, authenticated: true, sub: "not-a-guid");
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        await sync.DidNotReceiveWithAnyArgs().EnsureUserExistsAsync(
            default, default!, default!, default!, default);
    }

    private static IHost BuildHost(IUserSyncRepository sync, bool authenticated, string sub = "")
    {
        return new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddMemoryCache();
                    services.AddSingleton(sync);
                    services.AddLogging();
                });
                web.Configure(app =>
                {
                    app.Use(async (ctx, next) =>
                    {
                        if (authenticated)
                        {
                            var claims = new List<Claim>
                            {
                                new("sub", sub),
                                new("email", "test@cce.local"),
                                new("preferred_username", "testuser"),
                                new("groups", "SuperAdmin"),
                            };
                            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
                        }
                        await next();
                    });
                    app.UseMiddleware<UserSyncMiddleware>();
                    app.Run(_ => Task.CompletedTask);
                });
            })
            .Start();
    }
}
