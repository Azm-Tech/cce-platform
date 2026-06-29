using System.Collections.Generic;
using System.Linq;
using CCE.Domain.Common;
using CCE.Domain.Community.Events;
using CCE.Domain.Content;

namespace CCE.Domain.Community;

/// <summary>
/// Community post. Single-language: the author writes in their own language and the entity records
/// that locale. Has a <see cref="PostType"/> (Info / Question / Poll, fixed at creation) and a
/// <see cref="PostStatus"/> draft→published lifecycle (D9): drafts are author-private and excluded
/// from feeds; <see cref="PostCreatedEvent"/> fires only on <see cref="Publish"/>.
/// </summary>
[Audited]
public sealed class Post : AggregateRoot<System.Guid>
{
    public const int MaxContentLength = 8000;
    public const int MaxTitleLength = 150;
    public const int MaxAttachments = 9; // 5 images + 1 video + 3 documents

    private readonly List<Tag> _tags = new();
    private readonly List<PostAttachment> _attachments = new();

    private Post(
        System.Guid id,
        System.Guid communityId,
        System.Guid topicId,
        System.Guid authorId,
        PostType type,
        string? title,
        string? content,
        string locale) : base(id)
    {
        CommunityId = communityId;
        TopicId = topicId;
        AuthorId = authorId;
        Type = type;
        Title = title;
        Content = content;
        Locale = locale;
        IsAnswerable = type == PostType.Question;
        Status = PostStatus.Draft;
    }

    public System.Guid CommunityId { get; private set; }
    public System.Guid TopicId { get; private set; }
    public System.Guid AuthorId { get; private set; }
    public PostType Type { get; private set; }
    public PostStatus Status { get; private set; }
    public string? Title { get; private set; }
    public string? Content { get; private set; }
    public string Locale { get; private set; }
    public bool IsAnswerable { get; private set; }
    public System.Guid? AnsweredReplyId { get; private set; }
    public System.DateTimeOffset? PublishedOn { get; private set; }

    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<PostAttachment> Attachments => _attachments.AsReadOnly();

    // ─── Denormalized vote counters (source of truth = PostVote rows) ───
    public int UpvoteCount { get; private set; }
    public int DownvoteCount { get; private set; }

    /// <summary>Reddit-style hot rank; indexed for <c>ORDER BY score DESC</c>. See <see cref="VoteScore"/>.</summary>
    public double Score { get; private set; }

    /// <summary>Denormalized view count (source of truth = analytics / explicit increments).</summary>
    public int ViewCount { get; private set; }

    /// <summary>Denormalized share count (updated when a post is shared).</summary>
    public int ShareCount { get; private set; }

    /// <summary>Denormalized comment count (source of truth = PostReply rows; updated when a reply is created or deleted).</summary>
    public int CommentsCount { get; private set; }

    /// <summary>
    /// Creates a post in <see cref="PostStatus.Draft"/> with lenient validation (only shape/length caps);
    /// title/content may be empty while drafting. Does NOT raise <see cref="PostCreatedEvent"/>.
    /// </summary>
    public static Post CreateDraft(
        System.Guid communityId,
        System.Guid topicId,
        System.Guid authorId,
        PostType type,
        string? title,
        string? content,
        string locale,
        ISystemClock clock)
    {
        if (communityId == System.Guid.Empty) throw new DomainException("CommunityId is required.");
        if (topicId == System.Guid.Empty) throw new DomainException("TopicId is required.");
        if (authorId == System.Guid.Empty) throw new DomainException("AuthorId is required.");
        if (locale != "ar" && locale != "en") throw new DomainException("locale must be 'ar' or 'en'.");
        if (title is { Length: > MaxTitleLength })
            throw new DomainException($"Title exceeds {MaxTitleLength} chars (got {title.Length}).");
        if (content is { Length: > MaxContentLength })
            throw new DomainException($"Content exceeds {MaxContentLength} chars (got {content.Length}).");

        var p = new Post(System.Guid.NewGuid(), communityId, topicId, authorId, type, title, content, locale);
        p.MarkAsCreated(authorId, clock);
        p.Score = VoteScore.Hot(0, 0, p.CreatedOn);
        return p;
    }

    /// <summary>
    /// Transitions Draft → Published with strict per-type validation and raises
    /// <see cref="PostCreatedEvent"/>. Idempotent: re-publishing a published post is a no-op.
    /// </summary>
    public void Publish(ISystemClock clock)
    {
        if (Status == PostStatus.Published) return;

        if (string.IsNullOrWhiteSpace(Title))
            throw new DomainException("Title is required to publish.");
        if (Title.Length > MaxTitleLength)
            throw new DomainException($"Title exceeds {MaxTitleLength} chars.");
        // Poll posts may have no body (the poll carries the question); Info/Question require content.
        if (Type != PostType.Poll && string.IsNullOrWhiteSpace(Content))
            throw new DomainException("Content is required to publish.");

        Status = PostStatus.Published;
        PublishedOn = clock.UtcNow;
        RaiseDomainEvent(new PostCreatedEvent(Id, CommunityId, TopicId, AuthorId, Locale, Title!, PublishedOn.Value));
    }

    /// <summary>Edits a draft's title/content. Rejected once published (use the moderation/edit path).</summary>
    public void UpdateDraft(string? title, string? content, Guid by, ISystemClock clock)
    {
        if (Status != PostStatus.Draft)
            throw new DomainException("Only drafts can be updated via UpdateDraft.");
        if (title is { Length: > MaxTitleLength })
            throw new DomainException($"Title exceeds {MaxTitleLength} chars.");
        if (content is { Length: > MaxContentLength })
            throw new DomainException($"Content exceeds {MaxContentLength} chars.");
        Title = title;
        Content = content;
        MarkAsModified(by, clock);
    }

    /// <summary>Replaces the post's tag set (relational <c>post_tag</c> join).</summary>
    public void SetTags(IEnumerable<Tag> tags)
    {
        _tags.Clear();
        _tags.AddRange(tags.DistinctBy(t => t.Id));
    }

    /// <summary>Adds a media/document attachment. Enforces the per-post cap (<see cref="MaxAttachments"/>).</summary>
    public void AddAttachment(System.Guid assetFileId, AttachmentKind kind, int sortOrder, string? metadataJson)
    {
        if (_attachments.Count >= MaxAttachments)
            throw new DomainException($"A post may have at most {MaxAttachments} attachments.");
        _attachments.Add(PostAttachment.Create(Id, assetFileId, kind, sortOrder, metadataJson));
    }

    /// <summary>
    /// Adjusts denormalized vote counters when a user's vote changes from <paramref name="oldValue"/>
    /// to <paramref name="newValue"/> (each ∈ {+1, 0, -1}) and recomputes <see cref="Score"/>.
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

    /// <summary>
    /// Applies a vote change (see <see cref="ApplyVote"/>) and raises <see cref="PostVotedEvent"/> so a
    /// bridge handler can fan it onto the bus. Use this from command handlers instead of calling
    /// <see cref="ApplyVote"/> + publishing an integration event inline — keeps the async event atomic
    /// with the save and out of the Application layer.
    /// </summary>
    public void RegisterVote(System.Guid userId, int oldValue, int newValue, ISystemClock clock)
    {
        ApplyVote(oldValue, newValue);
        RaiseDomainEvent(new PostVotedEvent(
            Id, CommunityId, userId, newValue, oldValue, UpvoteCount, DownvoteCount, Score, clock.UtcNow));
    }

    /// <summary>
    /// Records that a reply was created on this post by raising <see cref="ReplyCreatedEvent"/>. The
    /// reply entity itself is persisted by its own repository; this only emits the domain event from the
    /// aggregate so a bridge handler relays it to the Worker for notification fan-out.
    /// </summary>
    public void RegisterReply(
        System.Guid replyId, System.Guid? parentReplyId, System.Guid authorId,
        string contentSnippet, ISystemClock clock)
    {
        RaiseDomainEvent(new ReplyCreatedEvent(
            replyId, Id, parentReplyId, authorId, contentSnippet, clock.UtcNow));
    }

    public void MarkAnswered(System.Guid replyId)
    {
        if (!IsAnswerable)
            throw new DomainException("Only answerable (question) posts can be marked answered.");
        if (replyId == System.Guid.Empty) throw new DomainException("ReplyId is required.");
        AnsweredReplyId = replyId;
    }

    public void ClearAnswer() => AnsweredReplyId = null;

    public void IncrementViews() => ViewCount++;
    public void IncrementShares() => ShareCount++;

    public void IncrementCommentsCount(ISystemClock clock)
    {
        CommentsCount++;
        RaiseDomainEvent(new Events.CommentCountChangedEvent(Id, CommentsCount, clock.UtcNow));
    }

    public void DecrementCommentsCount(ISystemClock clock)
    {
        if (CommentsCount > 0) CommentsCount--;
        RaiseDomainEvent(new Events.CommentCountChangedEvent(Id, CommentsCount, clock.UtcNow));
    }

    public void EditContent(string content, Guid by, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
            throw new DomainException($"Content exceeds {MaxContentLength} chars (got {content.Length}).");
        Content = content;
        MarkAsModified(by, clock);
    }
}
