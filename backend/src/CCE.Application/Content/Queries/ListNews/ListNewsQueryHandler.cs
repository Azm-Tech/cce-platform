using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListNews;

public sealed class ListNewsQueryHandler : IRequestHandler<ListNewsQuery, PagedResult<NewsDto>>
{
    private readonly ICceDbContext _db;

    public ListNewsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<NewsDto>> Handle(ListNewsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<News> query = _db.News;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(n =>
                n.TitleAr.Contains(term) ||
                n.TitleEn.Contains(term) ||
                n.Slug.Contains(term));
        }
        if (request.IsPublished is { } isPublished)
        {
            query = isPublished ? query.Where(n => n.PublishedOn != null) : query.Where(n => n.PublishedOn == null);
        }
        if (request.IsFeatured is { } isFeatured)
        {
            query = query.Where(n => n.IsFeatured == isFeatured);
        }
        query = query.OrderByDescending(n => n.PublishedOn ?? System.DateTimeOffset.MinValue)
                     .ThenByDescending(n => n.Id);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<NewsDto>(items, page.Page, page.PageSize, page.Total);
    }

    internal static NewsDto MapToDto(News n) => new(
        n.Id, n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
        n.Slug, n.AuthorId, n.FeaturedImageUrl,
        n.PublishedOn, n.IsFeatured, n.IsPublished,
        System.Convert.ToBase64String(n.RowVersion));
}
