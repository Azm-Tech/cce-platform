using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class HomepageSectionTests
{
    [Fact]
    public void Create_builds_active_section()
    {
        var s = HomepageSection.Create(HomepageSectionType.Hero, 0, "{}", "{}");
        s.SectionType.Should().Be(HomepageSectionType.Hero);
        s.OrderIndex.Should().Be(0);
        s.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reorder_updates_OrderIndex()
    {
        var s = HomepageSection.Create(HomepageSectionType.Hero, 0, "{}", "{}");
        s.Reorder(5);
        s.OrderIndex.Should().Be(5);
    }

    [Fact]
    public void UpdateContent_replaces_bilingual_content()
    {
        var s = HomepageSection.Create(HomepageSectionType.Hero, 0, "{}", "{}");
        s.UpdateContent("{ \"a\": 1 }", "{ \"b\": 2 }");
        s.ContentAr.Should().Be("{ \"a\": 1 }");
        s.ContentEn.Should().Be("{ \"b\": 2 }");
    }

    [Fact]
    public void Deactivate_and_Activate_toggle_IsActive()
    {
        var s = HomepageSection.Create(HomepageSectionType.Hero, 0, "{}", "{}");
        s.Deactivate();
        s.IsActive.Should().BeFalse();
        s.Activate();
        s.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var s = HomepageSection.Create(HomepageSectionType.Hero, 0, "{}", "{}");
        s.SoftDelete(System.Guid.NewGuid(), new FakeSystemClock());
        s.IsDeleted.Should().BeTrue();
    }
}
