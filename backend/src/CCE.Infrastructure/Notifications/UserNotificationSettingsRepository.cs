using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Notifications;

public sealed class UserNotificationSettingsRepository : EntityRepository<UserNotificationSettings, System.Guid>, IUserNotificationSettingsRepository
{
    public UserNotificationSettingsRepository(CceDbContext db) : base(db) { }

    public async Task<UserNotificationSettings?> GetAsync(
        System.Guid userId,
        NotificationChannel channel,
        string? eventCode,
        CancellationToken ct)
        => await Db.UserNotificationSettings
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.Channel == channel && s.EventCode == eventCode,
                ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<UserNotificationSettings>> ListForUserAsync(
        System.Guid userId,
        CancellationToken ct)
        => await Db.UserNotificationSettings
            .Where(s => s.UserId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<UserNotificationSettings>> ListForUserAndChannelsAsync(
        System.Guid userId,
        IReadOnlyCollection<NotificationChannel> channels,
        CancellationToken ct)
        => await Db.UserNotificationSettings
            .Where(s => s.UserId == userId && channels.Contains(s.Channel))
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
