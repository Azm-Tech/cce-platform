using CCE.Domain.Common;

namespace CCE.Domain.Notifications;

/// <summary>
/// User-level opt-in/opt-out for notification channels. A row with null EventCode
/// acts as the default for that channel; explicit EventCode rows override the default.
/// </summary>
public sealed class UserNotificationSettings : Entity<System.Guid>
{
    private UserNotificationSettings(
        System.Guid id,
        System.Guid userId,
        NotificationChannel channel,
        string? eventCode,
        bool isEnabled) : base(id)
    {
        UserId = userId;
        Channel = channel;
        EventCode = eventCode;
        IsEnabled = isEnabled;
        UpdatedOn = System.DateTimeOffset.UtcNow;
    }

    public System.Guid UserId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string? EventCode { get; private set; }
    public bool IsEnabled { get; private set; }
    public System.DateTimeOffset UpdatedOn { get; private set; }

    public static UserNotificationSettings Create(
        System.Guid userId,
        NotificationChannel channel,
        bool isEnabled,
        string? eventCode = null)
    {
        if (userId == System.Guid.Empty)
            throw new DomainException("UserId is required.");

        return new UserNotificationSettings(
            System.Guid.NewGuid(), userId, channel, eventCode, isEnabled);
    }

    public void Update(bool isEnabled)
    {
        IsEnabled = isEnabled;
        UpdatedOn = System.DateTimeOffset.UtcNow;
    }
}
