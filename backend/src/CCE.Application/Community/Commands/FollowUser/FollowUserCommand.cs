using MediatR;

namespace CCE.Application.Community.Commands.FollowUser;

public sealed record FollowUserCommand(Guid UserId) : IRequest<Unit>;
