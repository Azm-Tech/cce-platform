using MediatR;

namespace CCE.Application.Community.Commands.FollowTopic;

public sealed record FollowTopicCommand(Guid TopicId) : IRequest<Unit>;
