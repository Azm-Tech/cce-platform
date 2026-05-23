using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public interface IUserNotificationSettingsRepository
{
    Task<UserNotificationSettings?> GetAsync(
        System.Guid userId,
        NotificationChannel channel,
        string? eventCode,
        CancellationToken ct);

    Task<IReadOnlyList<UserNotificationSettings>> ListForUserAsync(
        System.Guid userId,
        CancellationToken ct);

    Task<IReadOnlyList<UserNotificationSettings>> ListForUserAndChannelsAsync(
        System.Guid userId,
        IReadOnlyCollection<NotificationChannel> channels,
        CancellationToken ct);

    Task AddAsync(UserNotificationSettings settings, CancellationToken ct);
}
