using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.ListNews;

public sealed record ListNewsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsPublished = null,
    bool? IsFeatured = null) : IRequest<PagedResult<NewsDto>>;
