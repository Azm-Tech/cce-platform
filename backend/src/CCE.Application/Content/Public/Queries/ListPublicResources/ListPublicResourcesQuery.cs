using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicResources;

public sealed record ListPublicResourcesQuery(
    int Page = 1,
    int PageSize = 20,
    System.Guid? CategoryId = null,
    System.Guid? CountryId = null,
    ResourceType? ResourceType = null) : IRequest<PagedResult<PublicResourceDto>>;
