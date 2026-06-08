using CCE.Application.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.VotePost;

/// <summary>US027 — up/down vote a post. <c>Direction.None</c> retracts the caller's vote.</summary>
public sealed record VotePostCommand(Guid PostId, VoteDirection Direction)
    : IRequest<Response<VoidData>>;
