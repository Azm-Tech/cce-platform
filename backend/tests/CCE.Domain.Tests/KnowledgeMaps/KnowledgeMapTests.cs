using CCE.Domain.Common;
using CCE.Domain.KnowledgeMaps;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.KnowledgeMaps;

public class KnowledgeMapTests
{
    private static KnowledgeMap NewMap() => KnowledgeMap.Create(
        "خريطة", "Map", "وصف", "Description", "carbon-cycle");

    [Fact]
    public void Create_active_map() {
        var m = NewMap();
        m.IsActive.Should().BeTrue();
        m.Slug.Should().Be("carbon-cycle");
        m.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public void Slug_must_be_kebab_case() {
        var act = () => KnowledgeMap.Create("ا", "x", "ا", "x", "Bad Slug");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateContent_replaces_bilingual_fields() {
        var m = NewMap();
        m.UpdateContent("ج", "new", "ج", "new");
        m.NameEn.Should().Be("new");
    }

    [Fact]
    public void Deactivate_then_Activate_toggles() {
        var m = NewMap();
        m.Deactivate(); m.IsActive.Should().BeFalse();
        m.Activate(); m.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_marks_deleted() {
        var m = NewMap();
        m.SoftDelete(System.Guid.NewGuid(), new FakeSystemClock());
        m.IsDeleted.Should().BeTrue();
    }
}
