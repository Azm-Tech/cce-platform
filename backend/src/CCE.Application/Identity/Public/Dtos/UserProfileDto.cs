using CCE.Application.InterestManagement.Dtos;
using CCE.Domain.Identity;

namespace CCE.Application.Identity.Public.Dtos;

public sealed record UserProfileDto(
    System.Guid Id,
    string? Email,
    string? UserName,
    string FirstName,
    string LastName,
    string JobTitle,
    string OrganizationName,
    string? PhoneNumber,
    string LocalePreference,
    KnowledgeLevel KnowledgeLevel,
    IReadOnlyList<InterestTopicDto> InterestTopics,
    System.Guid? CountryId,
    string? AvatarUrl);
