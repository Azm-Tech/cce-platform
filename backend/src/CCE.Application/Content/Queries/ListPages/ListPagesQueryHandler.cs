using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListPages;

public sealed class ListPagesQueryHandler : IRequestHandler<ListPagesQuery, PagedResult<PageDto>>
{
    private readonly ICceDbContext _db;

    public ListPagesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PageDto>> Handle(ListPagesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Page> query = _db.Pages;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.Slug.Contains(term) ||
                p.TitleAr.Contains(term) ||
                p.TitleEn.Contains(term));
        }

        if (request.PageType is { } pageType)
        {
            query = query.Where(p => p.PageType == pageType);
        }

        query = query.OrderBy(p => p.Slug);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<PageDto>(items, page.Page, page.PageSize, page.Total);
    }

    internal static PageDto MapToDto(Page p) => new(
        p.Id, p.Slug, p.PageType, p.TitleAr, p.TitleEn, p.ContentAr, p.ContentEn,
        System.Convert.ToBase64String(p.RowVersion));
}
