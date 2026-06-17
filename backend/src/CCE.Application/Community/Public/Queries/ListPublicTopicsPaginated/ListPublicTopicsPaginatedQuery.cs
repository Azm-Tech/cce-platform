using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicTopicsPaginated;

public sealed record ListPublicTopicsPaginatedQuery(
    string? Search,
    string? SortBy,
    int Page,
    int PageSize
) : IRequest<Response<PagedResult<PublicTopicItemDto>>>;
