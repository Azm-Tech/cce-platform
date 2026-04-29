using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.KnowledgeMaps.Public.Dtos;
using CCE.Domain.KnowledgeMaps;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMapEdges;

public sealed class ListKnowledgeMapEdgesQueryHandler
    : IRequestHandler<ListKnowledgeMapEdgesQuery, IReadOnlyList<PublicKnowledgeMapEdgeDto>>
{
    private readonly ICceDbContext _db;

    public ListKnowledgeMapEdgesQueryHandler(ICceDbContext db) => _db = db;

    public async Task<IReadOnlyList<PublicKnowledgeMapEdgeDto>> Handle(
        ListKnowledgeMapEdgesQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.KnowledgeMapEdges
            .Where(e => e.MapId == request.MapId)
            .OrderBy(e => e.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return items.Select(MapToDto).ToList();
    }

    internal static PublicKnowledgeMapEdgeDto MapToDto(KnowledgeMapEdge e) => new(
        e.Id,
        e.MapId,
        e.FromNodeId,
        e.ToNodeId,
        e.RelationshipType,
        e.OrderIndex);
}
