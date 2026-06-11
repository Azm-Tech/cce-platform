using CCE.Application.Common;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.SetUserFollow;

/// <summary>Idempotent follow upsert for a user. <see cref="Status"/> sets the desired state.</summary>
public sealed record SetUserFollowCommand(Guid UserId, FollowStatus Status) : IRequest<Response<VoidData>>;
