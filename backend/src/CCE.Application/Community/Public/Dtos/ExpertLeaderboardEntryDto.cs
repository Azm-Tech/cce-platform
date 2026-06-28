namespace CCE.Application.Community.Public.Dtos;

/// <summary>
/// One row of the community experts leaderboard. <see cref="Score"/> is the simple contribution
/// count (<see cref="PostCount"/> + <see cref="ReplyCount"/>); <see cref="Rank"/> is 1-based across
/// the full ordered set.
/// </summary>
public sealed record ExpertLeaderboardEntryDto(
    System.Guid UserId,
    string FirstName,
    string LastName,
    string JobTitle,
    string OrganizationName,
    string? AvatarUrl,
    System.Collections.Generic.IReadOnlyList<string> ExpertiseTags,
    int PostCount,
    int ReplyCount,
    int Score,
    int Rank);
