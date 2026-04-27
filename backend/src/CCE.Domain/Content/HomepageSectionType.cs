namespace CCE.Domain.Content;

/// <summary>
/// Logical block on the public homepage. Order is set per-section by
/// <c>HomepageSection.OrderIndex</c>; the same SectionType can appear multiple times
/// (e.g., two Hero rows) but each row is a distinct entity.
/// </summary>
public enum HomepageSectionType
{
    Hero = 0,
    FeaturedNews = 1,
    FeaturedResources = 2,
    UpcomingEvents = 3,
    KnowledgeMapTeaser = 4,
    InteractiveCityTeaser = 5,
    NewsletterSignup = 6,
    Custom = 99,
}
