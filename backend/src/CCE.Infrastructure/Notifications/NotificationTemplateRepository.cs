using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Notifications;

public sealed class NotificationTemplateRepository : EntityRepository<NotificationTemplate, System.Guid>, INotificationTemplateRepository
{
    public NotificationTemplateRepository(CceDbContext db) : base(db) { }

    public async Task<NotificationTemplate?> GetAsync(System.Guid id, CancellationToken ct)
        => await Db.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);

    public async Task<NotificationTemplate?> GetActiveByCodeAndChannelAsync(
        string code,
        NotificationChannel channel,
        CancellationToken ct)
        => await Db.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Code == code && t.Channel == channel && t.IsActive, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<NotificationTemplate>> ListActiveByCodeAsync(
        string code,
        CancellationToken ct)
        => await Db.NotificationTemplates
            .Where(t => t.Code == code && t.IsActive)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
