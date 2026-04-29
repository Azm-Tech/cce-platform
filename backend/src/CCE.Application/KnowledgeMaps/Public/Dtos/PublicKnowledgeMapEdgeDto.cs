using CCE.Domain.KnowledgeMaps;

namespace CCE.Application.KnowledgeMaps.Public.Dtos;

public sealed record PublicKnowledgeMapEdgeDto(
    System.Guid Id,
    System.Guid MapId,
    System.Guid FromNodeId,
    System.Guid ToNodeId,
    RelationshipType RelationshipType,
    int OrderIndex);
