using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class TopicTests
{
    private static Topic NewRoot() => Topic.Create(
        "أساسيات", "Basics", "ا", "Description", "basics", null, null, 0);

    [Fact]
    public void Create_root_topic_is_active()
    {
        var t = NewRoot();
        t.IsActive.Should().BeTrue();
        t.ParentId.Should().BeNull();
    }

    [Fact]
    public void Create_child_topic_has_parent()
    {
        var parent = System.Guid.NewGuid();
        var t = Topic.Create("ا", "x", "ا", "x", "child", parent, null, 0);
        t.ParentId.Should().Be(parent);
    }

    [Fact]
    public void Slug_must_be_kebab_case()
    {
        var act = () => Topic.Create("ا", "x", "ا", "x", "Bad Slug", null, null, 0);
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Fact]
    public void IconUrl_must_be_https()
    {
        var act = () => Topic.Create("ا", "x", "ا", "x", "x", null, "http://insecure", 0);
        act.Should().Throw<DomainException>().WithMessage("*Icon*");
    }

    [Fact]
    public void UpdateContent_replaces_bilingual_fields()
    {
        var t = NewRoot();
        t.UpdateContent("ج", "new", "ج", "new");
        t.NameEn.Should().Be("new");
        t.DescriptionAr.Should().Be("ج");
    }

    [Fact]
    public void Deactivate_then_Activate_toggles()
    {
        var t = NewRoot();
        t.Deactivate();
        t.IsActive.Should().BeFalse();
        t.Activate();
        t.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var t = NewRoot();
        t.SoftDelete(System.Guid.NewGuid(), new FakeSystemClock());
        t.IsDeleted.Should().BeTrue();
    }
}
