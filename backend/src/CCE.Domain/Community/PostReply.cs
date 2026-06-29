using CCE.Domain.Common;

namespace CCE.Domain.Community;

[Audited]
public sealed class PostReply : SoftDeletableEntity<System.Guid>
{
    public const int MaxContentLength = 8000;
    public const int MaxDepth = 8;

    private PostReply(
        System.Guid id, System.Guid postId, System.Guid authorId,
        string content, string locale, System.Guid? parentReplyId,
        bool isByExpert, int depth, string threadPath) : base(id)
    {
        PostId = postId; AuthorId = authorId;
        Content = content; Locale = locale;
        ParentReplyId = parentReplyId; IsByExpert = isByExpert;
        Depth = depth; ThreadPath = threadPath;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid AuthorId { get; private set; }
    public string Content { get; private set; }
    public string Locale { get; private set; }
    public System.Guid? ParentReplyId { get; private set; }
    public bool IsByExpert { get; private set; }

    // ─── Threading (materialized path; index on ThreadPath enables one-read subtree fetch) ───
    public int Depth { get; private set; }
    public string ThreadPath { get; private set; } = string.Empty;
    public int ChildCount { get; private set; }

    public ModerationStatus ModerationStatus { get; private set; } = ModerationStatus.Pending;

    public void SetModerationStatus(ModerationStatus status) => ModerationStatus = status;

    // ─── Denormalized vote counters (source of truth = ReplyVote rows) ───
    public int UpvoteCount { get; private set; }
    public int DownvoteCount { get; private set; }

    /// <summary>Reddit-style hot rank; orders sibling replies. See <see cref="VoteScore"/>.</summary>
    public double Score { get; private set; }

    /// <summary>Creates a top-level comment on a post.</summary>
    public static PostReply CreateRoot(
        System.Guid postId, System.Guid authorId,
        string content, string locale, bool isByExpert, ISystemClock clock)
    {
        Validate(postId, authorId, content, locale);
        var id = System.Guid.NewGuid();
        var r = new PostReply(id, postId, authorId, content, locale, null, isByExpert, 0, $"/{id}/");
        r.MarkAsCreated(authorId, clock);
        r.Score = VoteScore.Hot(0, 0, r.CreatedOn);
        return r;
    }

    /// <summary>
    /// Creates a nested reply under <paramref name="parent"/>, computing depth and the materialized
    /// <see cref="ThreadPath"/>, and incrementing the parent's <see cref="ChildCount"/>. Rejects
    /// nesting deeper than <see cref="MaxDepth"/>.
    /// </summary>
    public static PostReply CreateChild(
        PostReply parent, System.Guid authorId,
        string content, string locale, bool isByExpert, ISystemClock clock)
    {
        if (parent is null) throw new DomainException("Parent reply is required.");
        Validate(parent.PostId, authorId, content, locale);
        var depth = parent.Depth + 1;
        if (depth > MaxDepth) throw new DomainException($"Reply nesting exceeds the maximum depth of {MaxDepth}.");
        var id = System.Guid.NewGuid();
        var r = new PostReply(id, parent.PostId, authorId, content, locale, parent.Id, isByExpert,
            depth, parent.ThreadPath + id + "/");
        r.MarkAsCreated(authorId, clock);
        r.Score = VoteScore.Hot(0, 0, r.CreatedOn);
        parent.ChildCount++;
        return r;
    }

    private static void Validate(System.Guid postId, System.Guid authorId, string content, string locale)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (authorId == System.Guid.Empty) throw new DomainException("AuthorId is required.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
            throw new DomainException($"Content exceeds {MaxContentLength} chars.");
        if (locale != "ar" && locale != "en")
            throw new DomainException("locale must be 'ar' or 'en'.");
    }

    /// <summary>
    /// Adjusts the denormalized vote counters when a user's vote changes from
    /// <paramref name="oldValue"/> to <paramref name="newValue"/> (each ∈ {+1, 0, -1}) and
    /// recomputes <see cref="Score"/>. Idempotent when the value is unchanged.
    /// </summary>
    public void ApplyVote(int oldValue, int newValue)
    {
        if (oldValue == newValue) return;
        if (oldValue == 1) UpvoteCount--;
        else if (oldValue == -1) DownvoteCount--;
        if (newValue == 1) UpvoteCount++;
        else if (newValue == -1) DownvoteCount++;
        if (UpvoteCount < 0) UpvoteCount = 0;
        if (DownvoteCount < 0) DownvoteCount = 0;
        Score = VoteScore.Hot(UpvoteCount, DownvoteCount, CreatedOn);
    }

    public void EditContent(string content, Guid by, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
        {
            throw new DomainException($"Content exceeds {MaxContentLength} chars.");
        }
        Content = content;
        MarkAsModified(by, clock);
    }
}
