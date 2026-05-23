using CCE.Application.Notifications.Public;
using CCE.Domain.Common;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Notifications;

public sealed class UserNotificationRepository : EntityRepository<UserNotification, System.Guid>, IUserNotificationRepository
{
    public UserNotificationRepository(CceDbContext db) : base(db) { }

    public async Task<UserNotification?> GetAsync(System.Guid id, CancellationToken ct)
        => await Db.UserNotifications.FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);

    public async Task<int> MarkAllSentAsReadAsync(
        System.Guid userId,
        ISystemClock clock,
        CancellationToken ct)
    {
        var notifications = await Db.UserNotifications
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Sent)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var n in notifications)
        {
            n.MarkRead(clock);
        }

        await Db.SaveChangesAsync(ct).ConfigureAwait(false);
        return notifications.Count;
    }
}
