using CCE.Domain.Community;

namespace CCE.Application.Community.Services;

/// <summary>
/// Parses @[userId:name] mention tags from sanitized HTML content, validates scope,
/// caps at 10 per source, persists Mention rows, and returns the valid recipient IDs
/// for notification dispatch. Shared across CreateReply and PublishPost.
/// </summary>
public interface IMentionService
{
    Task<IReadOnlyList<System.Guid>> ExtractAndPersistAsync(
        string sanitizedContent,
        MentionSourceType sourceType,
        System.Guid sourceId,
        System.Guid postId,
        System.Guid communityId,
        string snippet,
        System.Guid authorId,
        CancellationToken ct);
}
