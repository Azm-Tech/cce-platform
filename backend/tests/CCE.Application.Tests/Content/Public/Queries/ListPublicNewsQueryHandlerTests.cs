using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicNews;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicNewsQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_news_exist()
    {
        var db = BuildDb(System.Array.Empty<News>());
        var sut = new ListPublicNewsQueryHandler(db);

        var result = await sut.Handle(new ListPublicNewsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Only_published_news_are_returned()
    {
        var clock = new FakeSystemClock();
        var authorId = System.Guid.NewGuid();

        var published = News.Draft("منشور", "Published", "محتوى", "Content", "published-slug", authorId, null, clock);
        var draft = News.Draft("مسودة", "Draft", "محتوى", "Content", "draft-slug", authorId, null, clock);
        published.Publish(clock);

        var db = BuildDb(new[] { published, draft });
        var sut = new ListPublicNewsQueryHandler(db);

        var result = await sut.Handle(new ListPublicNewsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("Published");
    }

    [Fact]
    public async Task IsFeatured_filter_returns_only_featured_published_news()
    {
        var clock = new FakeSystemClock();
        var authorId = System.Guid.NewGuid();

        var featured = News.Draft("مميز", "Featured", "محتوى", "Content", "featured-slug", authorId, null, clock);
        var regular = News.Draft("عادي", "Regular", "محتوى", "Content", "regular-slug", authorId, null, clock);
        featured.Publish(clock);
        featured.MarkFeatured();
        regular.Publish(clock);

        var db = BuildDb(new[] { featured, regular });
        var sut = new ListPublicNewsQueryHandler(db);

        var result = await sut.Handle(new ListPublicNewsQuery(Page: 1, PageSize: 20, IsFeatured: true), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("Featured");
        result.Items.Single().IsFeatured.Should().BeTrue();
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
