using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>One choice in a poll. <see cref="VoteCount"/> is denormalized (source of truth = PollVote rows).</summary>
public sealed class PollOption : Entity<System.Guid>
{
    public const int MaxLabelLength = 200;

    private PollOption(System.Guid id, System.Guid pollId, string label, int sortOrder) : base(id)
    {
        PollId = pollId;
        Label = label;
        SortOrder = sortOrder;
    }

    public System.Guid PollId { get; private set; }
    public string Label { get; private set; }
    public int SortOrder { get; private set; }
    public int VoteCount { get; private set; }

    internal static PollOption Create(System.Guid pollId, string label, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new DomainException("Option label is required.");
        if (label.Length > MaxLabelLength) throw new DomainException($"Option label exceeds {MaxLabelLength} chars.");
        return new PollOption(System.Guid.NewGuid(), pollId, label, sortOrder);
    }

    public void IncrementVotes() => VoteCount++;
}
