using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListPages;

public sealed class ListPagesQueryHandler : IRequestHandler<ListPagesQuery, Response<PagedResult<PageDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListPagesQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<PageDto>>> Handle(ListPagesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Pages
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                p => p.Slug.Contains(request.Search!) ||
                     p.TitleAr.Contains(request.Search!) ||
                     p.TitleEn.Contains(request.Search!))
            .WhereIf(request.PageType.HasValue, p => p.PageType == request.PageType!.Value)
            .OrderBy(p => p.Slug);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(result.Map(MapToDto), MessageKeys.General.ITEMS_LISTED);
    }

    internal static PageDto MapToDto(Page p) => new(
        p.Id, p.Slug, p.PageType, p.TitleAr, p.TitleEn, p.ContentAr, p.ContentEn,
        System.Convert.ToBase64String(p.RowVersion));
}
