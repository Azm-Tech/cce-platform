using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.ListResources;

public sealed record ListResourcesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    System.Guid? CategoryId = null,
    System.Guid? CountryId = null,
    bool? IsPublished = null) : IRequest<PagedResult<ResourceDto>>;
