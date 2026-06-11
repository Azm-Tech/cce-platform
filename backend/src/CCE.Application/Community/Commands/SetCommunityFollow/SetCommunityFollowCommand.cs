using CCE.Application.Common;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.SetCommunityFollow;

/// <summary>Idempotent follow upsert for a community. <see cref="Status"/> sets the desired state.</summary>
public sealed record SetCommunityFollowCommand(Guid CommunityId, FollowStatus Status) : IRequest<Response<VoidData>>;
