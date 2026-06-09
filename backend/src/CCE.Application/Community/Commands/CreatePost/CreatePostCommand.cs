using System.Collections.Generic;
using CCE.Application.Common;
using CCE.Application.Common.Caching;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.CreatePost;

/// <summary>
/// US026 — create a post. When <see cref="SaveAsDraft"/> is false the handler publishes it in the
/// same unit of work; when true it is saved as an author-private draft (D9).
/// </summary>
public sealed record CreatePostCommand(
    Guid CommunityId,
    Guid TopicId,
    PostType Type,
    string Title,
    string? Content,
    string Locale,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<PostAttachmentInput> Attachments,
    PollInput? Poll,
    bool SaveAsDraft) : IRequest<Response<Guid>>, ICacheInvalidatingRequest
{
    public IReadOnlyCollection<string> CacheRegionsToEvict { get; } =
        [CacheRegions.Posts, CacheRegions.Feed];
}
