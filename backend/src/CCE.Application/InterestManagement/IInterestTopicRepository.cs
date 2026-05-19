using CCE.Domain.Identity;

namespace CCE.Application.InterestManagement;

public interface IInterestTopicRepository
{
    Task AddAsync(InterestTopic topic, CancellationToken ct);
    Task<InterestTopic?> FindAsync(System.Guid id, CancellationToken ct);
    Task Update(InterestTopic topic);
    Task Delete(InterestTopic topic);
}
