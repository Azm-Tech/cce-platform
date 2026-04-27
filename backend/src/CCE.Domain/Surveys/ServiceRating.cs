using CCE.Domain.Common;

namespace CCE.Domain.Surveys;

public sealed class ServiceRating : Entity<System.Guid>
{
    private ServiceRating(System.Guid id, System.Guid? userId, int rating,
        string? commentAr, string? commentEn, string page, string locale,
        System.DateTimeOffset submittedOn) : base(id)
    {
        UserId = userId; Rating = rating;
        CommentAr = commentAr; CommentEn = commentEn;
        Page = page; Locale = locale; SubmittedOn = submittedOn;
    }

    public System.Guid? UserId { get; private set; }
    public int Rating { get; private set; }
    public string? CommentAr { get; private set; }
    public string? CommentEn { get; private set; }
    public string Page { get; private set; }
    public string Locale { get; private set; }
    public System.DateTimeOffset SubmittedOn { get; private set; }

    public static ServiceRating Submit(System.Guid? userId, int rating,
        string? commentAr, string? commentEn, string page, string locale, ISystemClock clock)
    {
        if (rating < 1 || rating > 5)
            throw new DomainException($"Rating must be 1-5 (got {rating}).");
        if (string.IsNullOrWhiteSpace(page)) throw new DomainException("Page is required.");
        if (locale != "ar" && locale != "en")
            throw new DomainException("locale must be 'ar' or 'en'.");
        return new ServiceRating(System.Guid.NewGuid(), userId, rating,
            commentAr, commentEn, page, locale, clock.UtcNow);
    }
}
