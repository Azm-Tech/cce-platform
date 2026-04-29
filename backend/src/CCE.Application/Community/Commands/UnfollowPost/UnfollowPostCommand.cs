using MediatR;

namespace CCE.Application.Community.Commands.UnfollowPost;

public sealed record UnfollowPostCommand(Guid PostId) : IRequest<Unit>;
