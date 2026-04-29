using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.KnowledgeMaps.Public.Dtos;
using CCE.Domain.KnowledgeMaps;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMapNodes;

public sealed class ListKnowledgeMapNodesQueryHandler
    : IRequestHandler<ListKnowledgeMapNodesQuery, IReadOnlyList<PublicKnowledgeMapNodeDto>>
{
    private readonly ICceDbContext _db;

    public ListKnowledgeMapNodesQueryHandler(ICceDbContext db) => _db = db;

    public async Task<IReadOnlyList<PublicKnowledgeMapNodeDto>> Handle(
        ListKnowledgeMapNodesQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.KnowledgeMapNodes
            .Where(n => n.MapId == request.MapId)
            .OrderBy(n => n.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return items.Select(MapToDto).ToList();
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
