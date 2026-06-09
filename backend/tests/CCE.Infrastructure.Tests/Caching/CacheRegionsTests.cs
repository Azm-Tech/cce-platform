using CCE.Application.Common.Caching;
using FluentAssertions;
using Xunit;

namespace CCE.Infrastructure.Tests.Caching;

public sealed class CacheRegionsTests
{
    [Theory]
    [InlineData("/api/resources", CacheRegions.Resources)]
    [InlineData("/api/resources/3f/details", CacheRegions.Resources)]
    [InlineData("/api/feed/news-events", CacheRegions.Feed)]
    [InlineData("/api/feed/featured-posts", CacheRegions.Feed)]
    [InlineData("/api/community/posts/123", CacheRegions.Posts)]
    [InlineData("/api/news", CacheRegions.News)]
    [InlineData("/api/events/5", CacheRegions.Events)]
    [InlineData("/api/homepage-sections", CacheRegions.Homepage)]
    [InlineData("/api/admin/resources", null)] // admin writes are never cached/region-mapped
    [InlineData("/api/unknown", null)]
    [InlineData("", null)]
    public void ResolveRegion_maps_public_paths_to_regions(string path, string? expected)
        => CacheRegions.ResolveRegion(path).Should().Be(expected);

    [Fact]
    public void TagSetKey_uses_the_out_tag_prefix()
        => CacheRegions.TagSetKey(CacheRegions.Resources).Should().Be("out:tag:resources");

    [Fact]
    public void IsKnownRegion_is_case_insensitive()
    {
        CacheRegions.IsKnownRegion("RESOURCES").Should().BeTrue();
        CacheRegions.IsKnownRegion("not-a-region").Should().BeFalse();
    }
}
