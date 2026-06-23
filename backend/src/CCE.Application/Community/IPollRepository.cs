using CCE.Domain.Community;

namespace CCE.Application.Community;

/// <summary>Write-side repository for the poll aggregate (§A.1).</summary>
public interface IPollRepository
{
    void AddPoll(Poll poll);
    Task<Poll?> GetWithOptionsAsync(Guid pollId, CancellationToken ct);
    void AddVote(PollVote vote);
    Task<IReadOnlyList<PollVote>> RemoveVotesAsync(Guid pollId, Guid userId, CancellationToken ct);
}
