using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Notifications;

public sealed class NotificationLogRepository : EntityRepository<NotificationLog, System.Guid>, INotificationLogRepository
{
    public NotificationLogRepository(CceDbContext db) : base(db) { }

    public async Task<NotificationLog?> GetAsync(System.Guid id, CancellationToken ct)
        => await Db.NotificationLogs.FirstOrDefaultAsync(l => l.Id == id, ct).ConfigureAwait(false);
}
