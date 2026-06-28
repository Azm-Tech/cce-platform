using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetAsync(System.Guid id, CancellationToken ct);

    Task<NotificationTemplate?> GetActiveByCodeAndChannelAsync(
        string code,
        NotificationChannel channel,
        CancellationToken ct);

    Task<IReadOnlyList<NotificationTemplate>> ListActiveByCodeAsync(
        string code,
        CancellationToken ct);

    Task AddAsync(NotificationTemplate template, CancellationToken ct);
}
