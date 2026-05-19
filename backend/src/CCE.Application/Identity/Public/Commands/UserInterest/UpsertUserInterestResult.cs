using CCE.Application.InterestManagement.Dtos;

namespace CCE.Application.Identity.Public.Commands.UserInterest;

public sealed record UpsertUserInterestResult(
    IReadOnlyList<InterestTopicDto> InterestTopics,
    IReadOnlyList<System.Guid> Added,
    IReadOnlyList<System.Guid> Removed);
