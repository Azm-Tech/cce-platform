using System.Collections.Generic;
using System.Linq;
using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// A poll owned 1:1 by a <see cref="PostType.Poll"/> post. Offers 2–10 options and closes at
/// <see cref="Deadline"/>. Settings are explicit columns (queried by the results path).
/// </summary>
public sealed class Poll : Entity<System.Guid>
{
    public const int MinOptions = 2;
    public const int MaxOptions = 10;

    private readonly List<PollOption> _options = new();

    private Poll(System.Guid id, System.Guid postId, System.DateTimeOffset deadline,
        bool allowMultiple, bool isAnonymous, bool showResultsBeforeClose) : base(id)
    {
        PostId = postId;
        Deadline = deadline;
        AllowMultiple = allowMultiple;
        IsAnonymous = isAnonymous;
        ShowResultsBeforeClose = showResultsBeforeClose;
    }

    public System.Guid PostId { get; private set; }
    public System.DateTimeOffset Deadline { get; private set; }
    public bool AllowMultiple { get; private set; }
    public bool IsAnonymous { get; private set; }
    public bool ShowResultsBeforeClose { get; private set; }

    public IReadOnlyCollection<PollOption> Options => _options.AsReadOnly();

    public bool IsClosed(ISystemClock clock) => clock.UtcNow >= Deadline;

    public static Poll Create(
        System.Guid postId, System.DateTimeOffset deadline,
        bool allowMultiple, bool isAnonymous, bool showResultsBeforeClose,
        IReadOnlyList<string> optionLabels, ISystemClock clock)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (deadline <= clock.UtcNow) throw new DomainException("Poll deadline must be in the future.");
        if (optionLabels is null || optionLabels.Count < MinOptions || optionLabels.Count > MaxOptions)
            throw new DomainException($"A poll must have between {MinOptions} and {MaxOptions} options.");

        var poll = new Poll(System.Guid.NewGuid(), postId, deadline, allowMultiple, isAnonymous, showResultsBeforeClose);
        var order = 0;
        foreach (var label in optionLabels)
            poll._options.Add(PollOption.Create(poll.Id, label, order++));
        return poll;
    }

    public PollOption? FindOption(System.Guid optionId) => _options.FirstOrDefault(o => o.Id == optionId);
}
