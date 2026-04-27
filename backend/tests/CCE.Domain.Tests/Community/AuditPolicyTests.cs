using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Domain.Tests.Community;

public class AuditPolicyTests
{
    [Theory]
    [InlineData(typeof(Topic))]
    [InlineData(typeof(Post))]
    [InlineData(typeof(PostReply))]
    public void Audited_entity_carries_attribute(System.Type type)
    {
        var attrs = type.GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().HaveCount(1, because: $"{type.Name} is audited per spec §4.11");
    }

    [Theory]
    [InlineData(typeof(PostRating))]
    [InlineData(typeof(TopicFollow))]
    [InlineData(typeof(UserFollow))]
    [InlineData(typeof(PostFollow))]
    public void Non_audited_high_volume_association_lacks_attribute(System.Type type)
    {
        var attrs = type.GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().BeEmpty(because: $"{type.Name} is intentionally NOT audited (high volume — spec §4.11)");
    }
}
