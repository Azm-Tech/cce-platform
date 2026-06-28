using CCE.Domain.Common;

namespace CCE.Domain.Notifications;

/// <summary>
/// Tracks every attempted delivery per channel. Supports admin troubleshooting and retry.
/// </summary>
public sealed class NotificationLog : Entity<System.Guid>
{
    private NotificationLog(
        System.Guid id,
        System.Guid? recipientUserId,
        string templateCode,
        System.Guid? templateId,
        NotificationChannel channel,
        string? payloadJson,
        string? correlationId) : base(id)
    {
        RecipientUserId = recipientUserId;
        TemplateCode = templateCode;
        TemplateId = templateId;
        Channel = channel;
        Status = NotificationDeliveryStatus.Pending;
        AttemptCount = 1;
        CreatedOn = System.DateTimeOffset.UtcNow;
        PayloadJson = payloadJson;
        CorrelationId = correlationId;
    }

    public System.Guid? RecipientUserId { get; private set; }
    public string TemplateCode { get; private set; }
    public System.Guid? TemplateId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationDeliveryStatus Status { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? Error { get; private set; }
    public int AttemptCount { get; private set; }
    public System.DateTimeOffset CreatedOn { get; private set; }
    public System.DateTimeOffset? SentOn { get; private set; }
    public System.DateTimeOffset? FailedOn { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? PayloadJson { get; private set; }

    public static NotificationLog Create(
        System.Guid? recipientUserId,
        string templateCode,
        System.Guid? templateId,
        NotificationChannel channel,
        string? payloadJson = null,
        string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(templateCode))
            throw new DomainException("TemplateCode is required.");

        return new NotificationLog(
            System.Guid.NewGuid(),
            recipientUserId,
            templateCode,
            templateId,
            channel,
            payloadJson,
            correlationId);
    }

    public void MarkSent(string? providerMessageId = null)
    {
        if (Status == NotificationDeliveryStatus.Sent)
            throw new DomainException("Log is already marked as sent.");

        Status = NotificationDeliveryStatus.Sent;
        ProviderMessageId = providerMessageId;
        SentOn = System.DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        if (Status == NotificationDeliveryStatus.Sent)
            throw new DomainException("Cannot mark a sent log as failed.");

        Status = NotificationDeliveryStatus.Failed;
        Error = error;
        FailedOn = System.DateTimeOffset.UtcNow;
    }

    public void MarkSkipped(string reason)
    {
        Status = NotificationDeliveryStatus.Skipped;
        Error = reason;
    }

    public void IncrementAttempt()
    {
        AttemptCount++;
        Status = NotificationDeliveryStatus.Pending;
        Error = null;
        FailedOn = null;
    }
}
