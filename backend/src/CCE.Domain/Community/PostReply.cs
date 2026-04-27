using CCE.Domain.Common;

namespace CCE.Domain.Community;

[Audited]
public sealed class PostReply : Entity<System.Guid>, ISoftDeletable
{
    public const int MaxContentLength = 8000;

    private PostReply(
        System.Guid id, System.Guid postId, System.Guid authorId,
        string content, string locale, System.Guid? parentReplyId,
        bool isByExpert, System.DateTimeOffset createdOn) : base(id)
    {
        PostId = postId; AuthorId = authorId;
        Content = content; Locale = locale;
        ParentReplyId = parentReplyId; IsByExpert = isByExpert;
        CreatedOn = createdOn;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid AuthorId { get; private set; }
    public string Content { get; private set; }
    public string Locale { get; private set; }
    public System.Guid? ParentReplyId { get; private set; }
    public bool IsByExpert { get; private set; }
    public System.DateTimeOffset CreatedOn { get; private set; }
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

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
        return new PostReply(System.Guid.NewGuid(), postId, authorId,
            content, locale, parentReplyId, isByExpert, clock.UtcNow);
    }

    public void EditContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
        {
            throw new DomainException($"Content exceeds {MaxContentLength} chars.");
        }
        Content = content;
    }

    public void SoftDelete(System.Guid deletedById, ISystemClock clock)
    {
        if (deletedById == System.Guid.Empty) throw new DomainException("DeletedById is required.");
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedById = deletedById;
        DeletedOn = clock.UtcNow;
    }
}
