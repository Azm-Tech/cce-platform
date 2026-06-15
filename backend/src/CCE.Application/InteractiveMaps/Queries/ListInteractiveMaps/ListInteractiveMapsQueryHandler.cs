using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.InteractiveMaps.Dtos;
using CCE.Application.Messages;
using CCE.Domain.InteractiveMaps;
using MediatR;

namespace CCE.Application.InteractiveMaps.Queries.ListInteractiveMaps;

internal sealed class ListInteractiveMapsQueryHandler
    : IRequestHandler<ListInteractiveMapsQuery, Response<PagedResult<InteractiveMapDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListInteractiveMapsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<InteractiveMapDto>>> Handle(
        ListInteractiveMapsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.InteractiveMaps
            .WhereIf(request.IsActive.HasValue, m => m.IsActive == request.IsActive!.Value)
            .OrderBy(m => m.NameEn);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(result.Map(MapToDto), "ITEMS_LISTED");
    }

    internal static InteractiveMapDto MapToDto(InteractiveMap m) => new(
        m.Id,
        m.NameAr,
        m.NameEn,
        m.DescriptionAr,
        m.DescriptionEn,
        m.Slug,
        m.IsActive);
}
