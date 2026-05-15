using CCE.Domain.Common;
using CCE.Domain.Community.Events;

namespace CCE.Domain.Community;

/// <summary>
/// Community post (question or discussion). Single-language: the author writes in their
/// own language and the entity records that locale. Question posts (<see cref="IsAnswerable"/>=true)
/// can have a <see cref="AnsweredReplyId"/> — set by the asker when they accept a reply as the answer.
/// Content max 8000 chars to keep the read-side cheap.
/// </summary>
[Audited]
public sealed class Post : SoftDeletableAggregateRoot<System.Guid>
{
    public const int MaxContentLength = 8000;

    private Post(
        System.Guid id,
        System.Guid topicId,
        System.Guid authorId,
        string content,
        string locale,
        bool isAnswerable) : base(id)
    {
        TopicId = topicId;
        AuthorId = authorId;
        Content = content;
        Locale = locale;
        IsAnswerable = isAnswerable;
    }

    public System.Guid TopicId { get; private set; }
    public System.Guid AuthorId { get; private set; }
    public string Content { get; private set; }
    public string Locale { get; private set; }
    public bool IsAnswerable { get; private set; }
    public System.Guid? AnsweredReplyId { get; private set; }

    public static Post Create(
        System.Guid topicId,
        System.Guid authorId,
        string content,
        string locale,
        bool isAnswerable,
        ISystemClock clock)
    {
        if (topicId == System.Guid.Empty) throw new DomainException("TopicId is required.");
        if (authorId == System.Guid.Empty) throw new DomainException("AuthorId is required.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
        {
            throw new DomainException($"Content exceeds {MaxContentLength} chars (got {content.Length}).");
        }
        if (locale != "ar" && locale != "en")
        {
            throw new DomainException("locale must be 'ar' or 'en'.");
        }
        var p = new Post(System.Guid.NewGuid(), topicId, authorId, content, locale, isAnswerable);
        p.MarkAsCreated(authorId, clock);
        p.RaiseDomainEvent(new PostCreatedEvent(p.Id, topicId, authorId, locale, p.CreatedOn));
        return p;
    }

    public void MarkAnswered(System.Guid replyId)
    {
        if (!IsAnswerable)
        {
            throw new DomainException("Only answerable (question) posts can be marked answered.");
        }
        if (replyId == System.Guid.Empty) throw new DomainException("ReplyId is required.");
        AnsweredReplyId = replyId;
    }

    public void ClearAnswer() => AnsweredReplyId = null;

    public void EditContent(string content, Guid by, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
        {
            throw new DomainException($"Content exceeds {MaxContentLength} chars (got {content.Length}).");
        }
        Content = content;
        MarkAsModified(by, clock);
    }
}
