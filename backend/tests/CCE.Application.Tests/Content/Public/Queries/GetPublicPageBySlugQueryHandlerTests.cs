using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.GetPublicPageBySlug;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Public.Queries;

public class GetPublicPageBySlugQueryHandlerTests
{
    [Fact]
    public async Task Returns_dto_when_page_exists_with_matching_slug()
    {
        var page = Page.Create("about-us", PageType.Custom, "عن الشركة", "About Us", "المحتوى", "Content");

        var db = BuildDb(new[] { page });
        var sut = new GetPublicPageBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicPageBySlugQuery("about-us"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("about-us");
        result.TitleEn.Should().Be("About Us");
        result.TitleAr.Should().Be("عن الشركة");
        result.ContentEn.Should().Be("Content");
    }

    [Fact]
    public async Task Returns_null_when_slug_not_found()
    {
        var db = BuildDb(System.Array.Empty<Page>());
        var sut = new GetPublicPageBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicPageBySlugQuery("no-such-slug"), CancellationToken.None);

        result.Should().BeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<Page> pages)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Pages.Returns(pages.AsQueryable());
        db.Users.Returns(System.Array.Empty<CCE.Domain.Identity.User>().AsQueryable());
        db.Roles.Returns(System.Array.Empty<CCE.Domain.Identity.Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>>().AsQueryable());
        db.Resources.Returns(System.Array.Empty<CCE.Domain.Content.Resource>().AsQueryable());
        return db;
    }
}
