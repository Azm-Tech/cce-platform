using CCE.Application.Common;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.SetPostFollow;

/// <summary>Idempotent follow upsert for a post. <see cref="Status"/> sets the desired state.</summary>
public sealed record SetPostFollowCommand(Guid PostId, FollowStatus Status) : IRequest<Response<VoidData>>;
