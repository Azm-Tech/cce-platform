namespace CCE.Application.Identity.Public.Commands.UserInterest;

public sealed record UpsertUserInterestResult(
    IReadOnlyList<string> Interests,
    bool Added);
