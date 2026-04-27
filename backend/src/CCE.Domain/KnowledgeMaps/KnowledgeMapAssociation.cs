using CCE.Domain.Common;

namespace CCE.Domain.KnowledgeMaps;

/// <summary>
/// Polymorphic association from a node to one of (Resource, News, Event). The pair
/// (<see cref="AssociatedType"/>, <see cref="AssociatedId"/>) is the FK; FK enforcement is
/// application-side (Phase 08 doesn't add a real FK because the target table varies).
/// </summary>
public sealed class KnowledgeMapAssociation : Entity<System.Guid>
{
    private KnowledgeMapAssociation(System.Guid id, System.Guid nodeId,
        AssociatedType associatedType, System.Guid associatedId, int orderIndex) : base(id)
    {
        NodeId = nodeId; AssociatedType = associatedType;
        AssociatedId = associatedId; OrderIndex = orderIndex;
    }

    public System.Guid NodeId { get; private set; }
    public AssociatedType AssociatedType { get; private set; }
    public System.Guid AssociatedId { get; private set; }
    public int OrderIndex { get; private set; }

    public static KnowledgeMapAssociation Associate(System.Guid nodeId,
        AssociatedType associatedType, System.Guid associatedId, int orderIndex = 0)
    {
        if (nodeId == System.Guid.Empty) throw new DomainException("NodeId is required.");
        if (associatedId == System.Guid.Empty) throw new DomainException("AssociatedId is required.");
        return new KnowledgeMapAssociation(System.Guid.NewGuid(), nodeId, associatedType, associatedId, orderIndex);
    }
}
