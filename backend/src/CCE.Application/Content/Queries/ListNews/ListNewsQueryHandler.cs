using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListNews;

public sealed class ListNewsQueryHandler : IRequestHandler<ListNewsQuery, PagedResult<NewsDto>>
{
    private readonly ICceDbContext _db;

    public ListNewsQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<NewsDto>> Handle(ListNewsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.News
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                n => n.TitleAr.Contains(request.Search!) ||
                     n.TitleEn.Contains(request.Search!) ||
                     n.Slug.Contains(request.Search!))
            .WhereIf(request.IsPublished == true,  n => n.PublishedOn != null)
            .WhereIf(request.IsPublished == false, n => n.PublishedOn == null)
            .WhereIf(request.IsFeatured.HasValue,  n => n.IsFeatured == request.IsFeatured!.Value)
            .OrderByDescending(n => n.PublishedOn ?? DateTimeOffset.MinValue)
            .ThenByDescending(n => n.Id);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        return result.Map(MapToDto);
    }

    internal static NewsDto MapToDto(News n) => new(
        n.Id, n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
        n.Slug, n.AuthorId, n.FeaturedImageUrl,
        n.PublishedOn, n.IsFeatured, n.IsPublished,
        System.Convert.ToBase64String(n.RowVersion));
}
