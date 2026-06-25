using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.KnowledgeMaps.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.KnowledgeMaps;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMapEdges;

public sealed class ListKnowledgeMapEdgesQueryHandler
    : IRequestHandler<ListKnowledgeMapEdgesQuery, Response<IReadOnlyList<PublicKnowledgeMapEdgeDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListKnowledgeMapEdgesQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyList<PublicKnowledgeMapEdgeDto>>> Handle(
        ListKnowledgeMapEdgesQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.KnowledgeMapEdges
            .Where(e => e.MapId == request.MapId)
            .OrderBy(e => e.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<PublicKnowledgeMapEdgeDto> list = items.Select(MapToDto).ToList();
        return _msg.Ok(list, MessageKeys.General.ITEMS_LISTED);
    }

    internal static PublicKnowledgeMapEdgeDto MapToDto(KnowledgeMapEdge e) => new(
        e.Id,
        e.MapId,
        e.FromNodeId,
        e.ToNodeId,
        e.RelationshipType,
        e.OrderIndex);
}
