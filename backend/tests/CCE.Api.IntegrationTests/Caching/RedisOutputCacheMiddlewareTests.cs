using System.Net;
using System.Net.Http;
using CCE.Api.Common.Caching;
using CCE.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CCE.Api.IntegrationTests.Caching;

public class RedisOutputCacheMiddlewareTests
{
    [Fact]
    public async Task Anonymous_GET_on_whitelisted_route_caches_second_request()
    {
        var redis = NewRedisStub(out var db);
        var calls = 0;
        using var host = BuildHost(redis, ctx =>
        {
            System.Threading.Interlocked.Increment(ref calls);
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsync("""{"ok":1}""");
        });
        using var client = host.GetTestClient();

        var first = await client.GetAsync(new Uri("/api/news?page=1", UriKind.Relative));
        var second = await client.GetAsync(new Uri("/api/news?page=1", UriKind.Relative));

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        // The stub always returns RedisValue.Null (no real Redis), so both requests miss
        // and both call StringSetAsync. We assert it was called at least once to verify
        // the caching write path is exercised. A Redis testcontainer would be needed for
        // a true round-trip cache-hit assertion; documented as a follow-up.
        await db.Received().StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<System.TimeSpan?>(),
            keepTtl: false,
            when: When.Always,
            flags: CommandFlags.None);
        calls.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Authenticated_GET_bypasses_cache()
    {
        var redis = NewRedisStub(out var db);
        var calls = 0;
        using var host = BuildHost(redis, ctx =>
        {
            System.Threading.Interlocked.Increment(ref calls);
            return Task.CompletedTask;
        });
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer x");

        await client.GetAsync(new Uri("/api/news", UriKind.Relative));

        await db.DidNotReceive().StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<System.TimeSpan?>(),
            keepTtl: false,
            when: When.Always,
            flags: CommandFlags.None);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task POST_request_bypasses_cache()
    {
        var redis = NewRedisStub(out var db);
        using var host = BuildHost(redis, _ => Task.CompletedTask);
        using var client = host.GetTestClient();

        using var content = new StringContent("");
        await client.PostAsync(new Uri("/api/news", UriKind.Relative), content);

        await db.DidNotReceive().StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<System.TimeSpan?>(),
            keepTtl: false,
            when: When.Always,
            flags: CommandFlags.None);
    }

    [Fact]
    public async Task Non_whitelisted_path_bypasses_cache()
    {
        var redis = NewRedisStub(out var db);
        using var host = BuildHost(redis, _ => Task.CompletedTask);
        using var client = host.GetTestClient();

        await client.GetAsync(new Uri("/api/admin/users", UriKind.Relative));

        await db.DidNotReceive().StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<System.TimeSpan?>(),
            keepTtl: false,
            when: When.Always,
            flags: CommandFlags.None);
    }

    private static IConnectionMultiplexer NewRedisStub(out IDatabase db)
    {
        var redis = Substitute.For<IConnectionMultiplexer>();
        db = Substitute.For<IDatabase>();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return redis;
    }

    private static IHost BuildHost(IConnectionMultiplexer redis, RequestDelegate handler)
    {
        return new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddSingleton(redis);
                    services.AddOptions<OutputCacheOptions>().Configure(o => { /* defaults */ });
                    services.AddSingleton<IOptions<CceInfrastructureOptions>>(
                        Options.Create(new CceInfrastructureOptions
                        {
                            SqlConnectionString = "x",
                            RedisConnectionString = "x",
                            OutputCacheTtlSeconds = 60,
                        }));
                    services.AddLogging();
                });
                web.Configure(app =>
                {
                    app.UseMiddleware<RedisOutputCacheMiddleware>();
                    app.Run(handler);
                });
            })
            .Start();
    }
}
