using CCE.Application.Common.Caching;
using CCE.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace CCE.Infrastructure.Tests.Caching;

/// <summary>
/// Integration tests for <see cref="RedisOutputCacheInvalidator"/> against a real Redis container
/// (requires Docker). Verifies the tag-set eviction model used by the cache-management endpoints.
/// </summary>
public sealed class RedisOutputCacheInvalidatorTests : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private ConnectionMultiplexer _redis = null!;
    private RedisOutputCacheInvalidator _sut = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        _redis = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString()).ConfigureAwait(false);
        _sut = new RedisOutputCacheInvalidator(_redis, NullLogger<RedisOutputCacheInvalidator>.Instance);
    }

    public async Task DisposeAsync()
    {
        _redis?.Dispose();
        await _container.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task EvictRegions_deletes_member_entries_and_the_tag_set()
    {
        var db = _redis.GetDatabase();
        const string k1 = "out:/api/resources?page=1|lang=en";
        const string k2 = "out:/api/resources?page=2|lang=en";
        var tagKey = CacheRegions.TagSetKey(CacheRegions.Resources);

        await db.StringSetAsync(k1, "a");
        await db.StringSetAsync(k2, "b");
        await db.SetAddAsync(tagKey, [k1, k2]);

        await _sut.EvictRegionsAsync([CacheRegions.Resources], CancellationToken.None);

        (await db.KeyExistsAsync(k1)).Should().BeFalse();
        (await db.KeyExistsAsync(k2)).Should().BeFalse();
        (await db.KeyExistsAsync(tagKey)).Should().BeFalse();
    }

    [Fact]
    public async Task GetStatus_reports_entry_counts_per_region()
    {
        var db = _redis.GetDatabase();
        await db.SetAddAsync(CacheRegions.TagSetKey(CacheRegions.News), ["out:/api/news?page=1|lang=en"]);

        var status = await _sut.GetStatusAsync(CancellationToken.None);

        status.Should().Contain(s => s.Region == CacheRegions.News && s.Entries == 1);
        status.Should().Contain(s => s.Region == CacheRegions.Events && s.Entries == 0);
    }

    [Fact]
    public async Task EvictKey_deletes_a_single_entry()
    {
        var db = _redis.GetDatabase();
        const string key = "out:/api/pages/about|lang=en";
        await db.StringSetAsync(key, "x");

        var removed = await _sut.EvictKeyAsync(key, CancellationToken.None);

        removed.Should().Be(1);
        (await db.KeyExistsAsync(key)).Should().BeFalse();
    }
}
