using CCE.Domain.Common;
using CCE.Domain.Country;

namespace CCE.Domain.Tests.Country;

public class AuditedCoverageTests
{
    [Theory]
    [InlineData(typeof(CCE.Domain.Country.Country))]
    [InlineData(typeof(CountryProfile))]
    [InlineData(typeof(CountryResourceRequest))]
    public void Country_aggregate_or_profile_carries_AuditedAttribute(System.Type type)
    {
        var attrs = type.GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().HaveCount(1, because: $"{type.Name} must be marked [Audited] (spec §4.11)");
    }
}
