using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.GetPublicNewsBySlug;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class GetPublicNewsBySlugQueryHandlerTests
{
    [Fact]
    public async Task Returns_dto_when_news_is_published_and_slug_matches()
    {
        var clock = new FakeSystemClock();
        var authorId = System.Guid.NewGuid();
        var news = News.Draft("عنوان", "Published News", "محتوى", "Content", "published-slug", authorId, null, clock);
        news.Publish(clock);

        var db = BuildDb(new[] { news });
        var sut = new GetPublicNewsBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicNewsBySlugQuery("published-slug"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("published-slug");
        result.TitleEn.Should().Be("Published News");
        result.PublishedOn.Should().NotBe(default);
    }

    [Fact]
    public async Task Returns_null_when_slug_not_found()
    {
        var db = BuildDb(System.Array.Empty<News>());
        var sut = new GetPublicNewsBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicNewsBySlugQuery("no-such-slug"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_news_found_but_not_published()
    {
        var clock = new FakeSystemClock();
        var authorId = System.Guid.NewGuid();
        var draft = News.Draft("مسودة", "Draft News", "محتوى", "Content", "draft-slug", authorId, null, clock);
        // Not published — PublishedOn is null

        var db = BuildDb(new[] { draft });
        var sut = new GetPublicNewsBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicNewsBySlugQuery("draft-slug"), CancellationToken.None);

        result.Should().BeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<News> news)
    {
        var db = Substitute.For<ICceDbContext>();
        db.News.Returns(news.AsQueryable());
        db.Users.Returns(System.Array.Empty<CCE.Domain.Identity.User>().AsQueryable());
        db.Roles.Returns(System.Array.Empty<CCE.Domain.Identity.Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>>().AsQueryable());
        db.Resources.Returns(System.Array.Empty<CCE.Domain.Content.Resource>().AsQueryable());
        return db;
    }
}
