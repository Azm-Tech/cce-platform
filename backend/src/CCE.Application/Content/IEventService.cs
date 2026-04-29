using CCE.Domain.Content;

namespace CCE.Application.Content;

public interface IEventService
{
    Task SaveAsync(Event @event, CancellationToken ct);
    Task<Event?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(Event @event, byte[] expectedRowVersion, CancellationToken ct);
}
