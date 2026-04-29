using MediatR;

namespace CCE.Application.Community.Commands.FollowPost;

public sealed record FollowPostCommand(Guid PostId) : IRequest<Unit>;
