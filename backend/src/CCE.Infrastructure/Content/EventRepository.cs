using CCE.Application.Content;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class EventRepository : IEventRepository
{
    private readonly CceDbContext _db;

    public EventRepository(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(Event @event, CancellationToken ct)
    {
        _db.Events.Add(@event);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<Event?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.Events.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Event @event, byte[] expectedRowVersion, CancellationToken ct)
    {
        _db.SetExpectedRowVersion(@event, expectedRowVersion);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
