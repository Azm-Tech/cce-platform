using CCE.Domain.Common;
using CCE.Domain.Content.Events;

namespace CCE.Domain.Content;

/// <summary>
/// Calendar event. Bilingual title/description/location, optional online meeting URL.
/// <see cref="ICalUid"/> is generated once at creation and never changes — keeping it
/// stable lets external calendar clients (.ics consumers) deduplicate updates by UID.
/// </summary>
[Audited]
public sealed class Event : AggregateRoot<System.Guid>
{
    private Event(
        System.Guid id,
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        System.DateTimeOffset startsOn,
        System.DateTimeOffset endsOn,
        string? locationAr,
        string? locationEn,
        string? onlineMeetingUrl,
        string? featuredImageUrl,
        string iCalUid) : base(id)
    {
        TitleAr = titleAr;
        TitleEn = titleEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        StartsOn = startsOn;
        EndsOn = endsOn;
        LocationAr = locationAr;
        LocationEn = locationEn;
        OnlineMeetingUrl = onlineMeetingUrl;
        FeaturedImageUrl = featuredImageUrl;
        ICalUid = iCalUid;
    }

    public string TitleAr { get; private set; }
    public string TitleEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public System.DateTimeOffset StartsOn { get; private set; }
    public System.DateTimeOffset EndsOn { get; private set; }
    public string? LocationAr { get; private set; }
    public string? LocationEn { get; private set; }
    public string? OnlineMeetingUrl { get; private set; }
    public string? FeaturedImageUrl { get; private set; }

    /// <summary>Stable iCalendar UID (set at creation). Never changes.</summary>
    public string ICalUid { get; private set; }

    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public static Event Schedule(
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        System.DateTimeOffset startsOn,
        System.DateTimeOffset endsOn,
        string? locationAr,
        string? locationEn,
        string? onlineMeetingUrl,
        string? featuredImageUrl,
        ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (endsOn <= startsOn)
        {
            throw new DomainException("EndsOn must be strictly after StartsOn.");
        }
        if (onlineMeetingUrl is not null
            && !onlineMeetingUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("OnlineMeetingUrl must use https://.");
        }
        if (featuredImageUrl is not null
            && !featuredImageUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("FeaturedImageUrl must use https://.");
        }
        var id = System.Guid.NewGuid();
        var iCalUid = $"{id:N}@cce.moenergy.gov.sa";
        var ev = new Event(id, titleAr, titleEn, descriptionAr, descriptionEn,
            startsOn, endsOn, locationAr, locationEn, onlineMeetingUrl, featuredImageUrl, iCalUid);
        ev.RaiseDomainEvent(new EventScheduledEvent(id, startsOn, endsOn, clock.UtcNow));
        return ev;
    }

    public void UpdateContent(
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        string? locationAr,
        string? locationEn,
        string? onlineMeetingUrl,
        string? featuredImageUrl)
    {
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (onlineMeetingUrl is not null
            && !onlineMeetingUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("OnlineMeetingUrl must use https://.");
        }
        if (featuredImageUrl is not null
            && !featuredImageUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("FeaturedImageUrl must use https://.");
        }
        TitleAr = titleAr;
        TitleEn = titleEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        LocationAr = locationAr;
        LocationEn = locationEn;
        OnlineMeetingUrl = onlineMeetingUrl;
        FeaturedImageUrl = featuredImageUrl;
    }

    public void Reschedule(System.DateTimeOffset startsOn, System.DateTimeOffset endsOn)
    {
        if (endsOn <= startsOn)
        {
            throw new DomainException("EndsOn must be strictly after StartsOn.");
        }
        StartsOn = startsOn;
        EndsOn = endsOn;
    }
}
