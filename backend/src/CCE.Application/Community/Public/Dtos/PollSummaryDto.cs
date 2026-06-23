namespace CCE.Application.Community.Public.Dtos;

/// <summary>One option inside a <see cref="PollSummaryDto"/> embedded in feed / listing items.</summary>
public sealed record FeedPollOptionDto(
    System.Guid Id,
    string       Label,
    int          SortOrder,
    int          VoteCount,   // 0 when ResultsVisible = false
    double       Percentage,  // 0 when ResultsVisible = false
    bool         UserVoted);  // true when the authenticated user selected this option

/// <summary>
/// Lightweight poll snapshot embedded directly on <see cref="CommunityFeedItemDto"/>,
/// <see cref="PublicPostDto"/>, and <see cref="PostDetailDto"/> for <see cref="CCE.Domain.Community.PostType.Poll"/>
/// posts. Null on Info and Question posts. Replaces the separate GET /polls/{id}/results
/// round-trip in list and detail views.
/// </summary>
public sealed record PollSummaryDto(
    System.Guid                                              PollId,
    System.DateTimeOffset                                    Deadline,
    bool                                                     IsClosed,
    bool                                                     AllowMultiple,
    bool                                                     IsAnonymous,
    bool                                                     ShowResultsBeforeClose,
    bool                                                     ResultsVisible,   // IsClosed || ShowResultsBeforeClose
    int                                                      TotalVotes,        // 0 when !ResultsVisible
    System.Collections.Generic.IReadOnlyList<FeedPollOptionDto> Options);
