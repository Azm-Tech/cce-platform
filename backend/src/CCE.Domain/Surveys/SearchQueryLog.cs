using CCE.Domain.Common;

namespace CCE.Domain.Surveys;

public sealed class SearchQueryLog : Entity<System.Guid>
{
    private SearchQueryLog(System.Guid id, System.Guid? userId, string queryText,
        int resultsCount, int responseTimeMs, string locale,
        System.DateTimeOffset submittedOn) : base(id)
    {
        UserId = userId; QueryText = queryText;
        ResultsCount = resultsCount; ResponseTimeMs = responseTimeMs;
        Locale = locale; SubmittedOn = submittedOn;
    }

    public System.Guid? UserId { get; private set; }
    public string QueryText { get; private set; }
    public int ResultsCount { get; private set; }
    public int ResponseTimeMs { get; private set; }
    public string Locale { get; private set; }
    public System.DateTimeOffset SubmittedOn { get; private set; }

    public static SearchQueryLog Record(System.Guid? userId, string queryText,
        int resultsCount, int responseTimeMs, string locale, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(queryText)) throw new DomainException("QueryText is required.");
        if (resultsCount < 0) throw new DomainException("ResultsCount cannot be negative.");
        if (responseTimeMs < 0) throw new DomainException("ResponseTimeMs cannot be negative.");
        if (locale != "ar" && locale != "en")
            throw new DomainException("locale must be 'ar' or 'en'.");
        return new SearchQueryLog(System.Guid.NewGuid(), userId, queryText,
            resultsCount, responseTimeMs, locale, clock.UtcNow);
    }
}
