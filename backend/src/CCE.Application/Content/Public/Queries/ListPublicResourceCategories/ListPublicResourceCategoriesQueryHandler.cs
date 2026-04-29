using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicResourceCategories;

public sealed class ListPublicResourceCategoriesQueryHandler
    : IRequestHandler<ListPublicResourceCategoriesQuery, System.Collections.Generic.IReadOnlyList<PublicResourceCategoryDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicResourceCategoriesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<System.Collections.Generic.IReadOnlyList<PublicResourceCategoryDto>> Handle(
        ListPublicResourceCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.ResourceCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return list.Select(MapToDto).ToList();
    }

    internal static PublicResourceCategoryDto MapToDto(ResourceCategory c) => new(
        c.Id,
        c.NameAr,
        c.NameEn,
        c.Slug,
        c.ParentId,
        c.OrderIndex);
}
