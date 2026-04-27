using CCE.Domain.Common;
using CCE.Domain.Content;

namespace CCE.Domain.Tests.Content;

public class PageTests
{
    [Fact]
    public void Create_builds_a_page()
    {
        var p = Page.Create("about", PageType.AboutPlatform,
            "عن المنصة", "About the platform",
            "محتوى", "content");

        p.Slug.Should().Be("about");
        p.PageType.Should().Be(PageType.AboutPlatform);
        p.TitleAr.Should().Be("عن المنصة");
        p.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public void Slug_must_be_kebab_case()
    {
        var act = () => Page.Create("Bad Slug", PageType.Custom, "ا", "x", "ا", "x");
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Fact]
    public void UpdateContent_replaces_bilingual_content()
    {
        var p = Page.Create("about", PageType.AboutPlatform, "ا", "x", "ا", "x");
        p.UpdateContent("ج", "new", "ج", "new");
        p.TitleAr.Should().Be("ج");
        p.ContentEn.Should().Be("new");
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var p = Page.Create("about", PageType.AboutPlatform, "ا", "x", "ا", "x");
        p.SoftDelete(System.Guid.NewGuid(), new CCE.TestInfrastructure.Time.FakeSystemClock());
        p.IsDeleted.Should().BeTrue();
    }
}
