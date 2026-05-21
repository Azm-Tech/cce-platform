namespace CCE.Application.PlatformSettings.Dtos;

public sealed record AboutSettingsDto(
    System.Guid Id,
    string DescriptionAr,
    string DescriptionEn,
    string? HowToUseVideoUrl,
    System.Collections.Generic.IReadOnlyList<GlossaryEntryDto> GlossaryEntries,
    System.Collections.Generic.IReadOnlyList<KnowledgePartnerDto> KnowledgePartners,
    string RowVersion);
