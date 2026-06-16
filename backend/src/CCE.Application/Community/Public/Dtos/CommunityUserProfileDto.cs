namespace CCE.Application.Community.Public.Dtos;

/// <summary>US030 — a user's public community profile.</summary>
public sealed record CommunityUserProfileDto(
    System.Guid UserId,
    string FirstName,
    string LastName,
    string JobTitle,
    string OrganizationName,
    string? AvatarUrl,
    bool IsExpert,
    int PostCount,
    int ReplyCount,
    int FollowerCount,
    int FollowingCount);
