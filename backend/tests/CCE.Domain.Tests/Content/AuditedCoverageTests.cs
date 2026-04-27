using CCE.Domain.Common;
using CCE.Domain.Content;

namespace CCE.Domain.Tests.Content;

public class AuditedCoverageTests
{
    [Theory]
    [InlineData(typeof(Resource))]
    [InlineData(typeof(News))]
    [InlineData(typeof(Event))]
    [InlineData(typeof(Page))]
    [InlineData(typeof(AssetFile))]
    [InlineData(typeof(ResourceCategory))]
    [InlineData(typeof(HomepageSection))]
    [InlineData(typeof(NewsletterSubscription))]
    public void Content_entity_carries_AuditedAttribute(System.Type type)
    {
        var attrs = type.GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().HaveCount(1, because: $"{type.Name} must be marked [Audited] (spec §4.11)");
    }
}
