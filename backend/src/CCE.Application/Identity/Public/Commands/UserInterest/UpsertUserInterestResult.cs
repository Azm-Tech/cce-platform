namespace CCE.Application.Identity.Public.Commands.UserInterest;

public sealed record UpsertUserInterestResult(
    IReadOnlyList<string> Interests,
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed);
