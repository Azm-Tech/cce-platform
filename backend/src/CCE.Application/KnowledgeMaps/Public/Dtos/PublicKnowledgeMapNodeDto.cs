using CCE.Domain.KnowledgeMaps;

namespace CCE.Application.KnowledgeMaps.Public.Dtos;

public sealed record PublicKnowledgeMapNodeDto(
    System.Guid Id,
    System.Guid MapId,
    string NameAr,
    string NameEn,
    NodeType NodeType,
    string? DescriptionAr,
    string? DescriptionEn,
    string? IconUrl,
    double LayoutX,
    double LayoutY,
    int OrderIndex);
