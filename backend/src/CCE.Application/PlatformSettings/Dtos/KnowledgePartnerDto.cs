namespace CCE.Application.PlatformSettings.Dtos;

public sealed record KnowledgePartnerDto(
    System.Guid Id,
    LocalizedTextDto Name,
    string? LogoUrl,
    string? WebsiteUrl,
    LocalizedTextDto? Description,
    int OrderIndex);
