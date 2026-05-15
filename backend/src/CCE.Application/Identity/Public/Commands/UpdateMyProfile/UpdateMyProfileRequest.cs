namespace CCE.Application.Identity.Public.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileRequest(
    string LocalePreference,
    Domain.Identity.KnowledgeLevel KnowledgeLevel,
    IReadOnlyList<string>? Interests,
    string? AvatarUrl,
    System.Guid? CountryId);
