using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.PublishPost;

/// <summary>US026 — publish a draft post (author only). Raises PostCreatedEvent (notifications).</summary>
public sealed record PublishPostCommand(Guid PostId) : IRequest<Response<VoidData>>;
