namespace CCE.Application.Community.Public.Dtos;

public sealed record MentionableUserDto(
    System.Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    bool IsFollowed,
    bool IsMember);
