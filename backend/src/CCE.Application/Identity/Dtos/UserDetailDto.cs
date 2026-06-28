using CCE.Application.InterestManagement.Dtos;
using CCE.Domain.Identity;

namespace CCE.Application.Identity.Dtos;

/// <summary>
/// Full user record for the admin user-detail view (Task 1.2 endpoint).
/// </summary>
public sealed record UserDetailDto(
    System.Guid Id,
    string? Email,
    string? UserName,
    string LocalePreference,
    KnowledgeLevel KnowledgeLevel,
    IReadOnlyList<InterestTopicDto> InterestTopics,
    System.Guid? CountryId,
    string? AvatarUrl,
    IReadOnlyList<string> Roles,
    bool IsActive);
