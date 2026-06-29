using CCE.Domain.Community;

namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Published by the moderation consumer when content is flagged or rejected, so admin
/// subscribers (SignalR moderator room) can be notified in real time.
/// </summary>
public sealed record ContentFlaggedIntegrationEvent(
    System.Guid             ContentId,
    string                  ContentType,
    ModerationStatus        Status,
    string?                 Category,
    string?                 Reason,
    System.DateTimeOffset   OccurredOn);
