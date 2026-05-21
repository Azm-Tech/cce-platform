namespace CCE.Application.PlatformSettings.Dtos;

public sealed record KnowledgePartnerDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string? LogoUrl,
    string? WebsiteUrl,
    string? DescriptionAr,
    string? DescriptionEn,
    int OrderIndex);
