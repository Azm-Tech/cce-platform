using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicHomepageSections;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicHomepageSectionsQueryHandlerTests
{
    [Fact]
    public async Task Returns_active_sections_sorted_by_order_index()
    {
        var section2 = HomepageSection.Create(HomepageSectionType.FeaturedNews, 1, "محتوى 2", "Content 2");
        var section1 = HomepageSection.Create(HomepageSectionType.Hero, 0, "محتوى 1", "Content 1");

        var db = BuildDb([section2, section1]);
        var sut = new ListPublicHomepageSectionsQueryHandler(db);

        var result = await sut.Handle(new ListPublicHomepageSectionsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].OrderIndex.Should().Be(0);
        result[0].ContentEn.Should().Be("Content 1");
        result[1].OrderIndex.Should().Be(1);
        result[1].ContentEn.Should().Be("Content 2");
    }

    [Fact]
    public async Task Returns_empty_when_no_active_sections_exist()
    {
        var inactive = HomepageSection.Create(HomepageSectionType.Hero, 0, "ar", "en");
        inactive.Deactivate();

        var db = BuildDb([inactive]);
        var sut = new ListPublicHomepageSectionsQueryHandler(db);

        var result = await sut.Handle(new ListPublicHomepageSectionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Excludes_inactive_sections()
    {
        var active = HomepageSection.Create(HomepageSectionType.Hero, 0, "ar-active", "en-active");
        var inactive = HomepageSection.Create(HomepageSectionType.FeaturedNews, 1, "ar-inactive", "en-inactive");
        inactive.Deactivate();

        var db = BuildDb([active, inactive]);
        var sut = new ListPublicHomepageSectionsQueryHandler(db);

        var result = await sut.Handle(new ListPublicHomepageSectionsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].ContentEn.Should().Be("en-active");
    }

    private static ICceDbContext BuildDb(IEnumerable<HomepageSection> sections)
    {
        var db = Substitute.For<ICceDbContext>();
        db.HomepageSections.Returns(sections.AsQueryable());
        return db;
    }
}
