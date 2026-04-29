using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.ListResourceCategories;

public sealed record ListResourceCategoriesQuery(
    int Page = 1,
    int PageSize = 20,
    System.Guid? ParentId = null,
    bool? IsActive = null) : IRequest<PagedResult<ResourceCategoryDto>>;
