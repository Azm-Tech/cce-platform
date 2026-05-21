namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicKnowledgePartnerDto(
    string NameAr,
    string NameEn,
    string? LogoUrl,
    string? WebsiteUrl,
    string? DescriptionAr,
    string? DescriptionEn);
