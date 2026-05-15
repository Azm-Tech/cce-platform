using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.GetNewsById;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class GetNewsByIdQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_null_when_news_not_found()
    {
        var db = BuildDb(Array.Empty<News>());
        var sut = new GetNewsByIdQueryHandler(db);

        var result = await sut.Handle(new GetNewsByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_found()
    {
        var authorId = System.Guid.NewGuid();
        var news = News.Draft("عنوان", "Test News Title", "المحتوى العربي", "English content body",
            "test-news-title", authorId, "https://example.com/image.jpg", Clock);
        news.Publish(Clock);
        news.MarkFeatured();

        var db = BuildDb([news]);
        var sut = new GetNewsByIdQueryHandler(db);

        var result = await sut.Handle(new GetNewsByIdQuery(news.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(news.Id);
        result.TitleAr.Should().Be("عنوان");
        result.TitleEn.Should().Be("Test News Title");
        result.ContentAr.Should().Be("المحتوى العربي");
        result.ContentEn.Should().Be("English content body");
        result.Slug.Should().Be("test-news-title");
        result.AuthorId.Should().Be(authorId);
        result.FeaturedImageUrl.Should().Be("https://example.com/image.jpg");
        result.IsPublished.Should().BeTrue();
        result.PublishedOn.Should().NotBeNull();
        result.IsFeatured.Should().BeTrue();
        result.RowVersion.Should().NotBeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<News> news)
    {
        var db = Substitute.For<ICceDbContext>();
        db.News.Returns(news.AsQueryable());
        return db;
    }
}
