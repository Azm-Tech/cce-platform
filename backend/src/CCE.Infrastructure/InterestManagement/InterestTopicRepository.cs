using CCE.Application.InterestManagement;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.InterestManagement;

public sealed class InterestTopicRepository : IInterestTopicRepository
{
    private readonly CceDbContext _db;

    public InterestTopicRepository(CceDbContext db) => _db = db;

    public async Task AddAsync(InterestTopic topic, CancellationToken ct)
    {
        await _db.InterestTopics.AddAsync(topic, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task<InterestTopic?> FindAsync(System.Guid id, CancellationToken ct)
        => _db.InterestTopics.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task Update(InterestTopic topic)
    {
        _db.InterestTopics.Update(topic);
        await _db.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task Delete(InterestTopic topic)
    {
        _db.InterestTopics.Remove(topic);
        await _db.SaveChangesAsync().ConfigureAwait(false);
    }
}
