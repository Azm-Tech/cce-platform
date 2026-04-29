using CCE.Domain.Identity;

namespace CCE.Application.Identity.Public.Dtos;

public sealed record UserProfileDto(
    System.Guid Id,
    string? Email,
    string? UserName,
    string LocalePreference,
    KnowledgeLevel KnowledgeLevel,
    IReadOnlyList<string> Interests,
    System.Guid? CountryId,
    string? AvatarUrl);
