using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Domain.Common;

namespace CCE.Application.Community.Public;

/// <summary>
/// Shared poll-data fetch used by all three feed/listing paths.
/// Accepts the Post IDs of Poll-type posts on the current page and returns a
/// dictionary keyed by PostId. Non-poll posts should not be passed in — they
/// simply won't match any row in the polls table and the result will be empty.
/// </summary>
internal static class PollHydrator
{
    internal static async Task<Dictionary<System.Guid, PollSummaryDto>> FetchAsync(
        ICceDbContext         db,
        ISystemClock          clock,
        IReadOnlyList<System.Guid> pollPostIds,
        System.Guid?          userId,
        CancellationToken     ct)
    {
        var result = new Dictionary<System.Guid, PollSummaryDto>();
        if (pollPostIds.Count == 0)
            return result;

        var now = clock.UtcNow;

        var rawPolls = await db.Polls
            .Where(p => pollPostIds.Contains(p.PostId))
            .Select(p => new
            {
                p.Id,
                p.PostId,
                p.Deadline,
                p.AllowMultiple,
                p.IsAnonymous,
                p.ShowResultsBeforeClose,
                Options = p.Options
                    .OrderBy(o => o.SortOrder)
                    .Select(o => new { o.Id, o.Label, o.SortOrder, o.VoteCount })
                    .ToList(),
                TotalVotes = p.Options.Sum(o => o.VoteCount),
            })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        if (rawPolls.Count == 0)
            return result;

        // User vote lookup — one batch query for all polls on the page.
        var votedOptionsByPoll = new Dictionary<System.Guid, System.Collections.Generic.HashSet<System.Guid>>();
        if (userId.HasValue)
        {
            var pollIds  = rawPolls.Select(p => p.Id).ToList();
            var userVotes = await db.PollVotes
                .Where(v => pollIds.Contains(v.PollId) && v.UserId == userId.Value)
                .Select(v => new { v.PollId, v.PollOptionId })
                .ToListAsyncEither(ct)
                .ConfigureAwait(false);

            foreach (var v in userVotes)
            {
                if (!votedOptionsByPoll.TryGetValue(v.PollId, out var set))
                    votedOptionsByPoll[v.PollId] = set = new System.Collections.Generic.HashSet<System.Guid>();
                set.Add(v.PollOptionId);
            }
        }

        foreach (var raw in rawPolls)
        {
            var isClosed       = now >= raw.Deadline;
            var resultsVisible = isClosed || raw.ShowResultsBeforeClose;
            var totalVotes     = resultsVisible ? raw.TotalVotes : 0;

            votedOptionsByPoll.TryGetValue(raw.Id, out var votedSet);
            votedSet ??= new System.Collections.Generic.HashSet<System.Guid>();

            var options = raw.Options.Select(o => new FeedPollOptionDto(
                o.Id, o.Label, o.SortOrder,
                resultsVisible ? o.VoteCount : 0,
                resultsVisible && raw.TotalVotes > 0
                    ? System.Math.Round(o.VoteCount * 100.0 / raw.TotalVotes, 1)
                    : 0,
                votedSet.Contains(o.Id))).ToList();

            result[raw.PostId] = new PollSummaryDto(
                raw.Id, raw.Deadline, isClosed,
                raw.AllowMultiple, raw.IsAnonymous, raw.ShowResultsBeforeClose,
                resultsVisible, totalVotes, options);
        }

        return result;
    }
}
