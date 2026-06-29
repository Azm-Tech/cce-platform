using CCE.Domain.Community;

namespace CCE.Application.Community.Queries.GetModerationQueue;

public sealed record ModerationQueueItemDto(
    System.Guid RecordId,
    ModerationContentType ContentType,
    System.Guid ContentId,
    ModerationStatus Status,
    string Phase,
    string? Provider,
    float? Score,
    string? Category,
    string? Reason,
    System.DateTimeOffset CreatedOn,
    string ContentPreview);
