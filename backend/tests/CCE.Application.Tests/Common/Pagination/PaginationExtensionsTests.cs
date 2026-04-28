using CCE.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Tests.Common.Pagination;

public class PaginationExtensionsTests
{
    [Fact]
    public async Task ToPagedResultAsync_returns_first_page_with_total()
    {
        var data = Enumerable.Range(1, 25).ToList();
        var query = data.AsQueryable();

        var result = await query.ToPagedResultAsync(page: 1, pageSize: 10, ct: CancellationToken.None);

        result.Total.Should().Be(25);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(10);
        result.Items[0].Should().Be(1);
        result.Items[9].Should().Be(10);
    }

    [Fact]
    public async Task ToPagedResultAsync_clamps_pageSize_to_max_100()
    {
        var data = Enumerable.Range(1, 200).ToList();
        var query = data.AsQueryable();

        var result = await query.ToPagedResultAsync(page: 1, pageSize: 500, ct: CancellationToken.None);

        result.PageSize.Should().Be(100);
        result.Items.Should().HaveCount(100);
    }

    [Fact]
    public async Task ToPagedResultAsync_clamps_pageSize_to_min_1()
    {
        var data = Enumerable.Range(1, 5).ToList();
        var query = data.AsQueryable();

        var result = await query.ToPagedResultAsync(page: 1, pageSize: 0, ct: CancellationToken.None);

        result.PageSize.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ToPagedResultAsync_clamps_page_to_min_1()
    {
        var data = Enumerable.Range(1, 5).ToList();
        var query = data.AsQueryable();

        var result = await query.ToPagedResultAsync(page: 0, pageSize: 10, ct: CancellationToken.None);

        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task ToPagedResultAsync_returns_empty_items_for_page_past_end()
    {
        var data = Enumerable.Range(1, 5).ToList();
        var query = data.AsQueryable();

        var result = await query.ToPagedResultAsync(page: 99, pageSize: 10, ct: CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(5);
        result.Page.Should().Be(99);
    }
}
