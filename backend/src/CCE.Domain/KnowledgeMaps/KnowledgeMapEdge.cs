using CCE.Domain.Common;

namespace CCE.Domain.KnowledgeMaps;

/// <summary>Edge between two nodes. <c>FromNodeId ≠ ToNodeId</c> invariant.</summary>
public sealed class KnowledgeMapEdge : Entity<System.Guid>
{
    private KnowledgeMapEdge(System.Guid id, System.Guid mapId, System.Guid fromNodeId,
        System.Guid toNodeId, RelationshipType relationshipType, int orderIndex) : base(id)
    {
        MapId = mapId; FromNodeId = fromNodeId; ToNodeId = toNodeId;
        RelationshipType = relationshipType; OrderIndex = orderIndex;
    }

    public System.Guid MapId { get; private set; }
    public System.Guid FromNodeId { get; private set; }
    public System.Guid ToNodeId { get; private set; }
    public RelationshipType RelationshipType { get; private set; }
    public int OrderIndex { get; private set; }

    public static KnowledgeMapEdge Connect(System.Guid mapId, System.Guid fromNodeId,
        System.Guid toNodeId, RelationshipType relationshipType, int orderIndex = 0)
    {
        if (mapId == System.Guid.Empty) throw new DomainException("MapId is required.");
        if (fromNodeId == System.Guid.Empty) throw new DomainException("FromNodeId is required.");
        if (toNodeId == System.Guid.Empty) throw new DomainException("ToNodeId is required.");
        if (fromNodeId == toNodeId) throw new DomainException("Self-loop not allowed (FromNodeId == ToNodeId).");
        return new KnowledgeMapEdge(System.Guid.NewGuid(), mapId, fromNodeId, toNodeId, relationshipType, orderIndex);
    }
}
