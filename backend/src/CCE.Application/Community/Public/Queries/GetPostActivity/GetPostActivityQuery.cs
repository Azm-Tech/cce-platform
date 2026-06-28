using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetPostActivity;

/// <summary>
/// Phase 3 reconnect catch-up: returns the delta for a post since a client-supplied
/// timestamp. Called by mobile/desktop clients on <c>onreconnected</c> so they get the
/// freshest counts and any replies missed while the WebSocket was down — without doing
/// per-event refetches. Reads SQL directly; no Redis.
/// </summary>
public sealed record GetPostActivityQuery(
    System.Guid PostId,
    System.DateTimeOffset Since,
    System.Guid? UserId = null) : IRequest<Response<PostActivityDto>>;