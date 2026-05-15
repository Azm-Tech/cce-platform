using CCE.Domain.Common;

namespace CCE.Domain.Community;

[Audited]
public sealed class PostReply : SoftDeletableEntity<System.Guid>
{
    public const int MaxContentLength = 8000;

    private PostReply(
        System.Guid id, System.Guid postId, System.Guid authorId,
        string content, string locale, System.Guid? parentReplyId,
        bool isByExpert) : base(id)
    {
        PostId = postId; AuthorId = authorId;
        Content = content; Locale = locale;
        ParentReplyId = parentReplyId; IsByExpert = isByExpert;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid AuthorId { get; private set; }
    public string Content { get; private set; }
    public string Locale { get; private set; }
    public System.Guid? ParentReplyId { get; private set; }
    public bool IsByExpert { get; private set; }

    public static PostReply Create(
        System.Guid postId, System.Guid authorId,
        string content, string locale, System.Guid? parentReplyId,
        bool isByExpert, ISystemClock clock)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (authorId == System.Guid.Empty) throw new DomainException("AuthorId is required.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
        {
            throw new DomainException($"Content exceeds {MaxContentLength} chars.");
        }
        if (locale != "ar" && locale != "en")
        {
            throw new DomainException("locale must be 'ar' or 'en'.");
        }
        var r = new PostReply(System.Guid.NewGuid(), postId, authorId,
            content, locale, parentReplyId, isByExpert);
        r.MarkAsCreated(authorId, clock);
        return r;
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
