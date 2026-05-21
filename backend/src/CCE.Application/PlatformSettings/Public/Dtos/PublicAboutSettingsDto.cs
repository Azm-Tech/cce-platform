namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicAboutSettingsDto(
    string DescriptionAr,
    string DescriptionEn,
    string? HowToUseVideoUrl,
    System.Collections.Generic.IReadOnlyList<PublicGlossaryEntryDto> Glossary,
    System.Collections.Generic.IReadOnlyList<PublicKnowledgePartnerDto> KnowledgePartners);
