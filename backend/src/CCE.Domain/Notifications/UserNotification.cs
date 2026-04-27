using CCE.Domain.Common;

namespace CCE.Domain.Notifications;

/// <summary>
/// One rendered notification delivered to a user. State machine:
/// <c>Pending → Sent → (Read | Failed)</c>. NOT audited (high-volume time-series).
/// </summary>
public sealed class UserNotification : Entity<System.Guid>
{
    private UserNotification(System.Guid id, System.Guid userId, System.Guid templateId,
        string renderedSubjectAr, string renderedSubjectEn, string renderedBody,
        string renderedLocale, NotificationChannel channel) : base(id)
    {
        UserId = userId; TemplateId = templateId;
        RenderedSubjectAr = renderedSubjectAr; RenderedSubjectEn = renderedSubjectEn;
        RenderedBody = renderedBody; RenderedLocale = renderedLocale;
        Channel = channel; Status = NotificationStatus.Pending;
    }

    public System.Guid UserId { get; private set; }
    public System.Guid TemplateId { get; private set; }
    public string RenderedSubjectAr { get; private set; }
    public string RenderedSubjectEn { get; private set; }
    public string RenderedBody { get; private set; }
    public string RenderedLocale { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public System.DateTimeOffset? SentOn { get; private set; }
    public System.DateTimeOffset? ReadOn { get; private set; }
    public NotificationStatus Status { get; private set; }

    public static UserNotification Render(System.Guid userId, System.Guid templateId,
        string renderedSubjectAr, string renderedSubjectEn, string renderedBody,
        string renderedLocale, NotificationChannel channel)
    {
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (templateId == System.Guid.Empty) throw new DomainException("TemplateId is required.");
        if (string.IsNullOrWhiteSpace(renderedBody)) throw new DomainException("RenderedBody is required.");
        if (renderedLocale != "ar" && renderedLocale != "en")
            throw new DomainException("RenderedLocale must be 'ar' or 'en'.");
        return new UserNotification(System.Guid.NewGuid(), userId, templateId,
            renderedSubjectAr, renderedSubjectEn, renderedBody, renderedLocale, channel);
    }

    public void MarkSent(ISystemClock clock)
    {
        if (Status != NotificationStatus.Pending)
            throw new DomainException($"Cannot send a {Status} notification — must be Pending.");
        Status = NotificationStatus.Sent;
        SentOn = clock.UtcNow;
    }

    public void MarkFailed(ISystemClock clock)
    {
        _ = clock;
        if (Status != NotificationStatus.Pending)
            throw new DomainException($"Cannot fail a {Status} notification — must be Pending.");
        Status = NotificationStatus.Failed;
    }

    public void MarkRead(ISystemClock clock)
    {
        if (Status != NotificationStatus.Sent)
            throw new DomainException($"Cannot mark {Status} notification as read — must be Sent.");
        Status = NotificationStatus.Read;
        ReadOn = clock.UtcNow;
    }
}
