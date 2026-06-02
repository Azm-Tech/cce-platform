using CCE.Domain.Content;

namespace CCE.Application.Content.Public.Queries.ListHomepageFeed;

internal sealed class FeedRow
{
    public System.Guid Id { get; init; }
    public HomepageFeedContentType ContentType { get; init; }
    public string NameEn { get; init; } = string.Empty;
    public string NameAr { get; init; } = string.Empty;
    public System.Guid? AuthorId { get; init; }
    public System.Guid TopicId { get; init; }
    public System.DateTimeOffset PublishedOn { get; init; }
    public string? FeaturedImageUrl { get; init; }
    public string? LocationEn { get; init; }
    public string? LocationAr { get; init; }
}
