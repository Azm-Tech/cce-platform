using CCE.Domain.Common;
using CCE.Domain.Content;

namespace CCE.Domain.Tests.Content;

public class ResourceCategoryTests
{
    [Fact]
    public void Create_root_category_has_no_parent()
    {
        var cat = ResourceCategory.Create("الشمسية", "Solar", "solar", parentId: null, orderIndex: 0);

        cat.NameAr.Should().Be("الشمسية");
        cat.NameEn.Should().Be("Solar");
        cat.Slug.Should().Be("solar");
        cat.ParentId.Should().BeNull();
        cat.OrderIndex.Should().Be(0);
        cat.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_child_category_has_parent()
    {
        var parentId = System.Guid.NewGuid();
        var cat = ResourceCategory.Create("الكهروضوئية", "Photovoltaic", "pv", parentId, orderIndex: 1);

        cat.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void Slug_must_be_kebab_case()
    {
        var act = () => ResourceCategory.Create("ا", "Solar", "Bad Slug", null, 0);
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Fact]
    public void Slug_with_underscores_rejected()
    {
        var act = () => ResourceCategory.Create("ا", "Solar", "wind_power", null, 0);
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Fact]
    public void Empty_NameAr_throws()
    {
        var act = () => ResourceCategory.Create("", "Solar", "solar", null, 0);
        act.Should().Throw<DomainException>().WithMessage("*NameAr*");
    }

    [Fact]
    public void Empty_NameEn_throws()
    {
        var act = () => ResourceCategory.Create("ا", "", "solar", null, 0);
        act.Should().Throw<DomainException>().WithMessage("*NameEn*");
    }

    [Fact]
    public void UpdateNames_replaces_bilingual_names()
    {
        var cat = ResourceCategory.Create("ا", "Old", "x", null, 0);
        cat.UpdateNames("ج", "New");
        cat.NameAr.Should().Be("ج");
        cat.NameEn.Should().Be("New");
    }

    [Fact]
    public void Reorder_updates_OrderIndex()
    {
        var cat = ResourceCategory.Create("ا", "x", "x", null, 0);
        cat.Reorder(7);
        cat.OrderIndex.Should().Be(7);
    }

    [Fact]
    public void Deactivate_sets_IsActive_false()
    {
        var cat = ResourceCategory.Create("ا", "x", "x", null, 0);
        cat.Deactivate();
        cat.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_sets_IsActive_true()
    {
        var cat = ResourceCategory.Create("ا", "x", "x", null, 0);
        cat.Deactivate();
        cat.Activate();
        cat.IsActive.Should().BeTrue();
    }
}
