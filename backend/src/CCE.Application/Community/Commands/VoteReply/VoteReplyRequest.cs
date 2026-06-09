using CCE.Domain.Community;

namespace CCE.Application.Community.Commands.VoteReply;

/// <summary>Request body for the vote-reply endpoint (the reply id comes from the route).</summary>
public sealed record VoteReplyRequest(VoteDirection Direction);
