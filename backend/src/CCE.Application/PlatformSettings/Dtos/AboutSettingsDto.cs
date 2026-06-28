namespace CCE.Application.PlatformSettings.Dtos;

public sealed record AboutSettingsDto(
    System.Guid Id,
    LocalizedTextDto Description,
    string? HowToUseVideoUrl,
    System.Collections.Generic.IReadOnlyList<GlossaryEntryDto> GlossaryEntries,
    System.Collections.Generic.IReadOnlyList<KnowledgePartnerDto> KnowledgePartners);
