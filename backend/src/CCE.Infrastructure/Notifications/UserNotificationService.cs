using CCE.Application.Notifications.Public;
using CCE.Domain.Common;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Notifications;

public sealed class UserNotificationService : IUserNotificationService
{
    private readonly CceDbContext _db;
    private readonly ISystemClock _clock;

    public UserNotificationService(CceDbContext db, ISystemClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<UserNotification?> FindAsync(System.Guid id, CancellationToken ct)
        => await _db.UserNotifications.FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);

    public async Task UpdateAsync(UserNotification notification, CancellationToken ct)
        => await _db.SaveChangesAsync(ct).ConfigureAwait(false);

    public async Task<int> MarkAllSentAsReadAsync(System.Guid userId, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        // EF Core 7+ bulk update. Atomic.
        return await _db.UserNotifications
            .Where(n => n.UserId == userId && n.Status == NotificationStatus.Sent)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.Status, NotificationStatus.Read)
                .SetProperty(n => n.ReadOn, (System.DateTimeOffset?)now), ct)
            .ConfigureAwait(false);
    }
}
