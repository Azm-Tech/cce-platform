using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Community.Services;

public sealed class MentionService : IMentionService
{
    private static readonly Regex MentionTagPattern = new(
        @"@\[([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}):[^\]]*\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private readonly IReplyRepository _repo;
    private readonly ISystemClock _clock;

    public MentionService(IReplyRepository repo, ISystemClock clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public async Task<IReadOnlyList<System.Guid>> ExtractAndPersistAsync(
        string sanitizedContent,
        MentionSourceType sourceType,
        System.Guid sourceId,
        System.Guid postId,
        System.Guid communityId,
        string snippet,
        System.Guid authorId,
        CancellationToken ct)
    {
        var candidates = MentionTagPattern.Matches(sanitizedContent)
            .Select(m => System.Guid.TryParse(m.Groups[1].Value, out var id) ? id : System.Guid.Empty)
            .Where(id => id != System.Guid.Empty && id != authorId)
            .Distinct()
            .Take(10)
            .ToList();

        if (candidates.Count == 0) return System.Array.Empty<System.Guid>();

        var visible = await _repo.FilterVisibleUsersAsync(communityId, candidates, ct).ConfigureAwait(false);

        foreach (var userId in visible)
        {
            _repo.AddMention(Mention.Create(
                sourceType, sourceId, postId, communityId, snippet, userId, authorId, _clock));
        }

        return visible;
    }
}
