using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Search;
using CCE.Application.Search.Queries;

namespace CCE.Application.Tests.Search.Queries;

public class SearchQueryHandlerTests
{
    [Fact]
    public async Task Calls_search_client_with_request_params()
    {
        var client = Substitute.For<ISearchClient>();
        var logger = Substitute.For<ISearchQueryLogger>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        client.SearchAsync(default!, default, default, default, default)
              .ReturnsForAnyArgs(new PagedResult<SearchHitDto>([], 1, 20, 0));

        var sut = new SearchQueryHandler(client, logger, currentUser);
        var query = new SearchQuery("carbon", SearchableType.News, 2, 10);

        await sut.Handle(query, CancellationToken.None);

        await client.Received(1).SearchAsync("carbon", SearchableType.News, 2, 10, CancellationToken.None);
    }

    [Fact]
    public async Task Returns_search_client_result()
    {
        var expected = new PagedResult<SearchHitDto>(
            [new SearchHitDto(System.Guid.NewGuid(), SearchableType.News, "عنوان", "Title", "مقتطف", "Excerpt", 0.9)],
            1, 20, 1);

        var client = Substitute.For<ISearchClient>();
        var logger = Substitute.For<ISearchQueryLogger>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        client.SearchAsync(default!, default, default, default, default)
              .ReturnsForAnyArgs(expected);

        var sut = new SearchQueryHandler(client, logger, currentUser);
        var query = new SearchQuery("carbon");

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Records_analytics_row_after_search()
    {
        var search = Substitute.For<ISearchClient>();
        var logger = Substitute.For<ISearchQueryLogger>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var pagedResult = new PagedResult<SearchHitDto>(System.Array.Empty<SearchHitDto>(), 1, 20, 0);
        search.SearchAsync(default!, default, default, default, default).ReturnsForAnyArgs(pagedResult);

        var sut = new SearchQueryHandler(search, logger, currentUser);
        await sut.Handle(new SearchQuery("carbon", null, 1, 20), CancellationToken.None);

        // Allow the fire-and-forget task time to run.
        await Task.Delay(200);

        await logger.Received(1).RecordAsync(
            Arg.Any<System.Guid?>(),
            "carbon",
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
