using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.UserInterest;

public sealed record UpsertUserInterestCommand(
    System.Guid UserId,
    IReadOnlyList<System.Guid> InterestTopicIds) : IRequest<Response<UpsertUserInterestResult>>;
