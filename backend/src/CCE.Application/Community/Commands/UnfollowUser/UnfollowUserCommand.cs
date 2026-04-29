using MediatR;

namespace CCE.Application.Community.Commands.UnfollowUser;

public sealed record UnfollowUserCommand(Guid UserId) : IRequest<Unit>;
