using StackExchange.Redis;

namespace CCE.Infrastructure.Tests.Redis;

public class RedisConnectionTests
{
    private const string ConnectionString = "localhost:6379";

    [Fact]
    public async Task Sets_and_gets_a_value()
    {
        await using var muxer = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
        var db = muxer.GetDatabase();
        var key = $"foundation:smoke:{Guid.NewGuid()}";

        await db.StringSetAsync(key, "ok", TimeSpan.FromSeconds(30));
        var value = await db.StringGetAsync(key);

        value.HasValue.Should().BeTrue();
        value.ToString().Should().Be("ok");

        await db.KeyDeleteAsync(key);
    }

    [Fact]
    public async Task Ping_succeeds()
    {
        await using var muxer = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
        var db = muxer.GetDatabase();

        var latency = await db.PingAsync();

        latency.Should().BeGreaterThan(TimeSpan.Zero);
        latency.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }
}
