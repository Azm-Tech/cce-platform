using CCE.Application.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.VoteReply;

/// <summary>US027 — up/down vote a reply. <c>Direction.None</c> retracts the caller's vote.</summary>
public sealed record VoteReplyCommand(Guid ReplyId, VoteDirection Direction)
    : IRequest<Response<VoidData>>;
