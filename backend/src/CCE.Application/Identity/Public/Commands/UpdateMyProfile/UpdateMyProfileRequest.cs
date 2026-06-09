namespace CCE.Application.Identity.Public.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileRequest(
    string FirstName,
    string LastName,
    string JobTitle,
    string OrganizationName,
    string LocalePreference,
    Domain.Identity.KnowledgeLevel KnowledgeLevel,
    string? AvatarUrl,
    System.Guid? CountryId,
    System.Guid? CountryCodeId);
