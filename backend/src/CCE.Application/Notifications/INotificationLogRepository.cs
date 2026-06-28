using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public interface INotificationLogRepository
{
    Task<NotificationLog?> GetAsync(System.Guid id, CancellationToken ct);

    Task AddAsync(NotificationLog log, CancellationToken ct);
}
