using System.Collections.Generic;

namespace CCE.Application.Community.Public.Dtos;

public sealed record PollOptionResultDto(System.Guid Id, string Label, int VoteCount, double Percentage);

public sealed record PollResultsDto(
    System.Guid PollId,
    System.DateTimeOffset Deadline,
    bool IsClosed,
    bool AllowMultiple,
    bool ResultsVisible,
    int TotalVotes,
    IReadOnlyList<PollOptionResultDto> Options);
