using CCE.Domain.Notifications;

namespace CCE.Application.Notifications.Public;

public interface IUserNotificationService
{
    Task<UserNotification?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(UserNotification notification, CancellationToken ct);
    Task<int> MarkAllSentAsReadAsync(System.Guid userId, CancellationToken ct);
}
