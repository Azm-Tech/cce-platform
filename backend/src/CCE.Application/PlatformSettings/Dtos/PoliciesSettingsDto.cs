namespace CCE.Application.PlatformSettings.Dtos;

public sealed record PoliciesSettingsDto(
    System.Guid Id,
    System.Collections.Generic.IReadOnlyList<PolicySectionDto> Sections);
