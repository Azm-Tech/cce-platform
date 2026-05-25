using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListPages;

public sealed class ListPagesQueryHandler : IRequestHandler<ListPagesQuery, PagedResult<PageDto>>
{
    private readonly ICceDbContext _db;

    public ListPagesQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<PageDto>> Handle(ListPagesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Pages
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                p => p.Slug.Contains(request.Search!) ||
                     p.TitleAr.Contains(request.Search!) ||
                     p.TitleEn.Contains(request.Search!))
            .WhereIf(request.PageType.HasValue, p => p.PageType == request.PageType!.Value)
            .OrderBy(p => p.Slug);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        return result.Map(MapToDto);
    }

    internal static PageDto MapToDto(Page p) => new(
        p.Id, p.Slug, p.PageType, p.TitleAr, p.TitleEn, p.ContentAr, p.ContentEn,
        System.Convert.ToBase64String(p.RowVersion));
}
