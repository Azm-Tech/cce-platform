namespace CCE.Application.PlatformSettings.Dtos;

public sealed record GlossaryEntryDto(
    System.Guid Id,
    string TermAr,
    string TermEn,
    string DefinitionAr,
    string DefinitionEn,
    int OrderIndex);
