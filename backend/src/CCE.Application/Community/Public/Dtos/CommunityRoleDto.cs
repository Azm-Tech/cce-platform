namespace CCE.Application.Community.Public.Dtos;

/// <summary>
/// A fixed community membership role and the capabilities it grants. Static config — there is no
/// per-community role storage.
/// </summary>
public sealed record CommunityRoleDto(
    string Key,
    string NameEn,
    string NameAr,
    string DescriptionEn,
    string DescriptionAr,
    System.Collections.Generic.IReadOnlyList<string> Capabilities);
