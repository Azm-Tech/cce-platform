using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListPages;

public sealed record ListPagesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    PageType? PageType = null) : IRequest<PagedResult<PageDto>>;
