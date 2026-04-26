using CCE.Domain.Common;

namespace CCE.Domain.Tests.Common;

public class AuditedAttributeTests
{
    [Audited]
    private sealed class SampleAudited { }

    private sealed class SampleNotAudited { }

    [Fact]
    public void Audited_attribute_is_visible_via_reflection()
    {
        var attr = typeof(SampleAudited).GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attr.Should().HaveCount(1);
    }

    [Fact]
    public void Type_without_attribute_returns_no_attribute()
    {
        var attr = typeof(SampleNotAudited).GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attr.Should().BeEmpty();
    }

    [Fact]
    public void Audited_attribute_targets_class_only()
    {
        var usage = typeof(AuditedAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false);
        usage.Should().HaveCount(1);
        var au = (AttributeUsageAttribute)usage[0];
        au.ValidOn.Should().Be(AttributeTargets.Class);
        au.AllowMultiple.Should().BeFalse();
        au.Inherited.Should().BeTrue();
    }
}
