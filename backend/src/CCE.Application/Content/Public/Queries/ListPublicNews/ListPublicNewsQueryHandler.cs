using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicNews;

public sealed class ListPublicNewsQueryHandler : IRequestHandler<ListPublicNewsQuery, PagedResult<PublicNewsDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicNewsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PublicNewsDto>> Handle(ListPublicNewsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<News> query = _db.News.Where(n => n.PublishedOn != null);

        if (request.IsFeatured is { } isFeatured)
        {
            query = query.Where(n => n.IsFeatured == isFeatured);
        }

        query = query.OrderByDescending(n => n.PublishedOn);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<PublicNewsDto>(items, page.Page, page.PageSize, page.Total);
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
