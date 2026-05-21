namespace CCE.Application.PlatformSettings.Public.Dtos;

public sealed record PublicPoliciesSettingsDto(
    System.Collections.Generic.IReadOnlyList<PublicPolicySectionDto> Sections);
