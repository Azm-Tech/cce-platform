using CCE.Application.Common;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.SetTopicFollow;

/// <summary>Idempotent follow upsert for a topic. <see cref="Status"/> sets the desired state.</summary>
public sealed record SetTopicFollowCommand(Guid TopicId, FollowStatus Status) : IRequest<Response<VoidData>>;
