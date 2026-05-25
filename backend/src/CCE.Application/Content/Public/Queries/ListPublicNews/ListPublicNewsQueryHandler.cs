using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicNews;

public sealed class ListPublicNewsQueryHandler : IRequestHandler<ListPublicNewsQuery, PagedResult<PublicNewsDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicNewsQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<PublicNewsDto>> Handle(ListPublicNewsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.News
            .Where(n => n.PublishedOn != null)
            .WhereIf(request.IsFeatured.HasValue, n => n.IsFeatured == request.IsFeatured!.Value)
            .OrderByDescending(n => n.PublishedOn);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        return result.Map(MapToDto);
    }

    internal static PublicNewsDto MapToDto(News n) => new(
        n.Id,
        n.TitleAr,
        n.TitleEn,
        n.ContentAr,
        n.ContentEn,
        n.Slug,
        n.FeaturedImageUrl,
        n.PublishedOn!.Value,
        n.IsFeatured);
}
