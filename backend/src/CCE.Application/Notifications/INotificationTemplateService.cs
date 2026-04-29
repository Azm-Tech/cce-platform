using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public interface INotificationTemplateService
{
    Task SaveAsync(NotificationTemplate template, CancellationToken ct);
    Task<NotificationTemplate?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(NotificationTemplate template, CancellationToken ct);
}
