using System.Collections.Generic;
using CCE.Domain.Community;

namespace CCE.Application.Community.Commands.CreatePost;

/// <summary>Request body for the create-post endpoint.</summary>
public sealed record CreatePostRequest(
    Guid CommunityId,
    Guid TopicId,
    PostType Type,
    string Title,
    string? Content,
    string Locale,
    IReadOnlyList<Guid>? TagIds,
    IReadOnlyList<PostAttachmentInput>? Attachments,
    PollInput? Poll,
    bool SaveAsDraft);
