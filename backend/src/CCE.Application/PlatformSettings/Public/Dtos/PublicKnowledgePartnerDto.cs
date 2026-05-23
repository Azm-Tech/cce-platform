using CCE.Application.PlatformSettings.Dtos;

namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicKnowledgePartnerDto(
    LocalizedTextDto Name,
    string? LogoUrl,
    string? WebsiteUrl,
    LocalizedTextDto? Description);
