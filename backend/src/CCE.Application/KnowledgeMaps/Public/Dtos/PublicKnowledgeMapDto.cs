namespace CCE.Application.KnowledgeMaps.Public.Dtos;

public sealed record PublicKnowledgeMapDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string DescriptionAr,
    string DescriptionEn,
    string Slug,
    bool IsActive);
