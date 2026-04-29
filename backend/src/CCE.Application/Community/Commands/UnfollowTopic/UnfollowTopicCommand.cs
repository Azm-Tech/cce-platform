using MediatR;

namespace CCE.Application.Community.Commands.UnfollowTopic;

public sealed record UnfollowTopicCommand(Guid TopicId) : IRequest<Unit>;
