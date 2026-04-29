using CCE.Domain.Community;

namespace CCE.Application.Community;

public interface ITopicService
{
    Task SaveAsync(Topic topic, CancellationToken ct);
    Task<Topic?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(Topic topic, CancellationToken ct);
}
