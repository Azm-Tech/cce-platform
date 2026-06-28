using CCE.Application.PlatformSettings.Dtos;

namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicAboutSettingsDto(
    LocalizedTextDto Description,
    string? HowToUseVideoUrl,
    System.Collections.Generic.IReadOnlyList<PublicGlossaryEntryDto> Glossary,
    System.Collections.Generic.IReadOnlyList<PublicKnowledgePartnerDto> KnowledgePartners);
