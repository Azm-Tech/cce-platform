using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicResourceCategories;

public sealed class ListPublicResourceCategoriesQueryHandler
    : IRequestHandler<ListPublicResourceCategoriesQuery, Response<System.Collections.Generic.IReadOnlyList<PublicResourceCategoryDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListPublicResourceCategoriesQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<System.Collections.Generic.IReadOnlyList<PublicResourceCategoryDto>>> Handle(
        ListPublicResourceCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.ResourceCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        return _msg.Ok((System.Collections.Generic.IReadOnlyList<PublicResourceCategoryDto>)list.Select(MapToDto).ToList(), MessageKeys.General.ITEMS_LISTED);
    }

    internal static PublicResourceCategoryDto MapToDto(ResourceCategory c) => new(
        c.Id,
        c.NameAr,
        c.NameEn,
        c.Slug,
        c.ParentId,
        c.OrderIndex);
}
