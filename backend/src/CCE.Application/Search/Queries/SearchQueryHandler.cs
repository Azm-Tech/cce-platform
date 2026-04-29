using CCE.Application.Common.Pagination;
using MediatR;

namespace CCE.Application.Search.Queries;

public sealed class SearchQueryHandler : IRequestHandler<SearchQuery, PagedResult<SearchHitDto>>
{
    private readonly ISearchClient _client;

    public SearchQueryHandler(ISearchClient client)
    {
        _client = client;
    }

    public Task<PagedResult<SearchHitDto>> Handle(SearchQuery request, CancellationToken cancellationToken)
        => _client.SearchAsync(request.Q, request.Type, request.Page, request.PageSize, cancellationToken);
}
