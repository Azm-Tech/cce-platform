using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.KnowledgeMaps.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.KnowledgeMaps;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMapNodes;

public sealed class ListKnowledgeMapNodesQueryHandler
    : IRequestHandler<ListKnowledgeMapNodesQuery, Response<IReadOnlyList<PublicKnowledgeMapNodeDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListKnowledgeMapNodesQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyList<PublicKnowledgeMapNodeDto>>> Handle(
        ListKnowledgeMapNodesQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.KnowledgeMapNodes
            .Where(n => n.MapId == request.MapId)
            .OrderBy(n => n.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<PublicKnowledgeMapNodeDto> list = items.Select(MapToDto).ToList();
        return _msg.Ok(list, MessageKeys.General.ITEMS_LISTED);
    }

    internal static PublicKnowledgeMapNodeDto MapToDto(KnowledgeMapNode n) => new(
        n.Id,
        n.MapId,
        n.NameAr,
        n.NameEn,
        n.NodeType,
        n.DescriptionAr,
        n.DescriptionEn,
        n.IconUrl,
        n.LayoutX,
        n.LayoutY,
        n.OrderIndex);
}
