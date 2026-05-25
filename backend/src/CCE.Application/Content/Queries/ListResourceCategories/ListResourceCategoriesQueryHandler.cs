using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListResourceCategories;

public sealed class ListResourceCategoriesQueryHandler
    : IRequestHandler<ListResourceCategoriesQuery, PagedResult<ResourceCategoryDto>>
{
    private readonly ICceDbContext _db;

    public ListResourceCategoriesQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<ResourceCategoryDto>> Handle(
        ListResourceCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.ResourceCategories
            .WhereIf(request.ParentId.HasValue, c => c.ParentId == request.ParentId!.Value)
            .WhereIf(request.IsActive.HasValue, c => c.IsActive == request.IsActive!.Value)
            .OrderBy(c => c.OrderIndex);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        return result.Map(MapToDto);
    }

    internal static ResourceCategoryDto MapToDto(ResourceCategory c) => new(
        c.Id,
        c.NameAr,
        c.NameEn,
        c.Slug,
        c.ParentId,
        c.OrderIndex,
        c.IsActive);
}
