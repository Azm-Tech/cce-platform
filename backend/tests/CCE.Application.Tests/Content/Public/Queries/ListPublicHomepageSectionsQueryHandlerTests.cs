using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicHomepageSections;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicHomepageSectionsQueryHandlerTests
{
    [Fact]
    public async Task Returns_active_sections_sorted_by_order_index()
    {
        var section1 = HomepageSection.Create(HomepageSectionType.Hero, 2, "محتوى 1", "Content 1");
        var section2 = HomepageSection.Create(HomepageSectionType.FeaturedNews, 1, "محتوى 2", "Content 2");
        var inactive = HomepageSection.Create(HomepageSectionType.UpcomingEvents, 0, "محتوى غير نشط", "Inactive Content");
        inactive.Deactivate();

        var db = BuildDb(new[] { section1, section2, inactive });
        var sut = new ListPublicHomepageSectionsQueryHandler(db);

        var result = await sut.Handle(new ListPublicHomepageSectionsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].OrderIndex.Should().Be(1);
        result[0].ContentEn.Should().Be("Content 2");
        result[1].OrderIndex.Should().Be(2);
        result[1].ContentEn.Should().Be("Content 1");
    }

    [Fact]
    public async Task Returns_empty_when_no_active_sections_exist()
    {
        var inactive = HomepageSection.Create(HomepageSectionType.Hero, 1, "محتوى", "Content");
        inactive.Deactivate();

        var db = BuildDb(new[] { inactive });
        var sut = new ListPublicHomepageSectionsQueryHandler(db);

        var result = await sut.Handle(new ListPublicHomepageSectionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static ICceDbContext BuildDb(IEnumerable<HomepageSection> sections)
    {
        var db = Substitute.For<ICceDbContext>();
        db.HomepageSections.Returns(sections.AsQueryable());
        db.Users.Returns(System.Array.Empty<CCE.Domain.Identity.User>().AsQueryable());
        db.Roles.Returns(System.Array.Empty<CCE.Domain.Identity.Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>>().AsQueryable());
        db.Resources.Returns(System.Array.Empty<CCE.Domain.Content.Resource>().AsQueryable());
        return db;
    }
}
