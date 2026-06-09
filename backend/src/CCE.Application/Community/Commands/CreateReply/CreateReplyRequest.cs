using System.Collections.Generic;

namespace CCE.Application.Community.Commands.CreateReply;

/// <summary>Request body for the create-reply endpoint (post id comes from the route).</summary>
public sealed record CreateReplyRequest(
    string Content,
    string Locale,
    Guid? ParentReplyId,
    IReadOnlyList<Guid>? MentionedUserIds);
