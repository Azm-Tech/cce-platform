using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.UserInterest;

public sealed record UpsertUserInterestCommand(
    System.Guid UserId,
    string Interest) : IRequest<Response<UpsertUserInterestResult>>;
