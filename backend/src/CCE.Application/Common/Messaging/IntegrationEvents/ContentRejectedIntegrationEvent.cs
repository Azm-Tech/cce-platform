namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Published at the moment moderation actually takes content down (soft-delete) — whether by the
/// AI auto-reject path or a human reviewer. Drives the author-facing "your content was removed"
/// notification. NOT published for <c>Flagged</c> (still visible) or for a <c>Rejected</c> status
/// that did not result in a takedown (e.g. <c>AutoRejectOnViolation=false</c>).
/// </summary>
public sealed record ContentRejectedIntegrationEvent(
    System.Guid ContentId,
    string      ContentType,
    System.Guid AuthorId,
    string      Locale);
