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

    public ListResourceCategoriesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ResourceCategoryDto>> Handle(
        ListResourceCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<ResourceCategory> query = _db.ResourceCategories;

        if (request.ParentId is { } parentId)
        {
            query = query.Where(c => c.ParentId == parentId);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(c => c.IsActive == isActive);
        }

        query = query.OrderBy(c => c.OrderIndex);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<ResourceCategoryDto>(items, page.Page, page.PageSize, page.Total);
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
