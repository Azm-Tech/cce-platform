using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using MediatR;

namespace CCE.Application.Search.Queries;

public sealed record SearchQuery(
    string Q,
    SearchableType? Type = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Response<PagedResult<SearchHitDto>>>;
