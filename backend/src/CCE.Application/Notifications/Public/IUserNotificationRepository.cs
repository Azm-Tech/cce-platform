using CCE.Domain.Common;
using CCE.Domain.Notifications;

namespace CCE.Application.Notifications.Public;

public interface IUserNotificationRepository
{
    Task<UserNotification?> GetAsync(System.Guid id, CancellationToken ct);

    Task AddAsync(UserNotification notification, CancellationToken ct);

    Task<int> MarkAllSentAsReadAsync(
        System.Guid userId,
        ISystemClock clock,
        CancellationToken ct);
}
