using CCE.Application.Community;
using CCE.Domain.Community;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Community;

public sealed class PollRepository : IPollRepository
{
    private readonly CceDbContext _db;

    public PollRepository(CceDbContext db) => _db = db;

    public void AddPoll(Poll poll) => _db.Polls.Add(poll);

    public Task<Poll?> GetWithOptionsAsync(Guid pollId, CancellationToken ct)
        => _db.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Id == pollId, ct);

    public void AddVote(PollVote vote) => _db.PollVotes.Add(vote);

    public async Task<IReadOnlyList<PollVote>> RemoveVotesAsync(Guid pollId, Guid userId, CancellationToken ct)
    {
        var votes = await _db.PollVotes
            .Where(v => v.PollId == pollId && v.UserId == userId)
            .ToListAsync(ct);
        _db.PollVotes.RemoveRange(votes);
        return votes;
    }
}
