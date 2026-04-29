using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListHomepageSections;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Queries;

public class ListHomepageSectionsQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_list_when_no_sections_exist()
    {
        var db = BuildDb(System.Array.Empty<HomepageSection>());
        var sut = new ListHomepageSectionsQueryHandler(db);

        var result = await sut.Handle(new ListHomepageSectionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_sections_sorted_by_OrderIndex_ascending()
    {
        var first = HomepageSection.Create(HomepageSectionType.Hero, 0, "ar-hero", "en-hero");
        var second = HomepageSection.Create(HomepageSectionType.FeaturedNews, 1, "ar-news", "en-news");

        var db = BuildDb(new[] { second, first });
        var sut = new ListHomepageSectionsQueryHandler(db);

        var result = await sut.Handle(new ListHomepageSectionsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].OrderIndex.Should().Be(0);
        result[1].OrderIndex.Should().Be(1);
    }

    private static ICceDbContext BuildDb(IEnumerable<HomepageSection> sections)
    {
        var db = Substitute.For<ICceDbContext>();
        db.HomepageSections.Returns(sections.AsQueryable());
        return db;
    }
}
