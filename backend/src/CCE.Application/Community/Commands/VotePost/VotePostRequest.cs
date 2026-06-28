using CCE.Domain.Community;

namespace CCE.Application.Community.Commands.VotePost;

/// <summary>Request body for the vote-post endpoint (the post id comes from the route).</summary>
public sealed record VotePostRequest(VoteDirection Direction);
