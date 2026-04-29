using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Notifications;

public sealed class NotificationTemplateService : INotificationTemplateService
{
    private readonly CceDbContext _db;

    public NotificationTemplateService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(NotificationTemplate template, CancellationToken ct)
    {
        _db.NotificationTemplates.Add(template);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<NotificationTemplate?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(NotificationTemplate template, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
