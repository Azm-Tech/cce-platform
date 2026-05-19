namespace CCE.Application.Identity.Public.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileRequest(
    string LocalePreference,
    Domain.Identity.KnowledgeLevel KnowledgeLevel,
    string? AvatarUrl,
    System.Guid? CountryId);
