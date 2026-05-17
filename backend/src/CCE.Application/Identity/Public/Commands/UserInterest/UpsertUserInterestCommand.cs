using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.UserInterest;

public sealed record UpsertUserInterestCommand(
    System.Guid UserId,
    IReadOnlyList<string> Interests) : IRequest<Response<UpsertUserInterestResult>>;
