using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.InteractiveMaps.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.InteractiveMaps.Queries.ListInteractiveMapNodes;

internal sealed class ListInteractiveMapNodesQueryHandler
    : IRequestHandler<ListInteractiveMapNodesQuery, Response<PagedResult<InteractiveMapNodeDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListInteractiveMapNodesQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<InteractiveMapNodeDto>>> Handle(
        ListInteractiveMapNodesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _db.InteractiveMapNodes
            .Where(n => n.InteractiveMapId == request.MapId)
            .WhereIf(request.IsActive.HasValue, n => n.IsActive == request.IsActive!.Value)
            .OrderBy(n => n.Category)
            .ToPagedResultAsync(n => new InteractiveMapNodeDto(
                n.Id,
                n.InteractiveMapId,
                n.NameAr,
                n.NameEn,
                n.IconKey,
                n.Category,
                n.CategoryNameAr,
                n.CategoryNameEn,
                n.TitleAr,
                n.TitleEn,
                n.DescriptionAr,
                n.DescriptionEn,
                n.ParentId,
                n.TopicId,
                n.IsActive,
                n.Tags.Select(t => new InteractiveMapTagDto(t.Id, t.NameAr, t.NameEn)).ToList()),
                request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(result, MessageKeys.General.ITEMS_LISTED);
    }
}
