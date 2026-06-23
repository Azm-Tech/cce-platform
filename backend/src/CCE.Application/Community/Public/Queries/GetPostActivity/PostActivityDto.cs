using System.Collections.Generic;
using CCE.Application.Community.Public.Dtos;

namespace CCE.Application.Community.Public.Queries.GetPostActivity;

/// <summary>
/// Delta payload for the <see cref="GetPostActivityQuery"/> catch-up endpoint. Carries
/// current denormalized counters (always the authoritative totals), any replies created
/// after the client's <c>Since</c> cursor (full nodes — same shape as the NewReply
/// realtime payload, refetched from SQL so they survive a long disconnect window), and
/// the poll snapshot if the post is a poll.
/// </summary>
public sealed record PostActivityDto(
    int UpvoteCount,
    int DownvoteCount,
    double Score,
    int ReplyCount,
    IReadOnlyList<PublicPostReplyDto> NewReplies,
    PollSummaryDto? Poll);