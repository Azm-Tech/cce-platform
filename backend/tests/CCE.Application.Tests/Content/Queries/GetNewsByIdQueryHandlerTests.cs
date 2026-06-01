using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.GetNewsById;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class GetNewsByIdQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_not_found_when_news_missing()
    {
        var sut = BuildSut(Array.Empty<News>());

        var result = await sut.Handle(new GetNewsByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_found()
    {
        var authorId = System.Guid.NewGuid();
        var topicId = System.Guid.NewGuid();
        var news = News.Draft("عنوان", "Test News Title", "المحتوى العربي", "English content body",
            topicId, authorId, "https://example.com/image.jpg", Clock);
        news.Publish(Clock);
        news.MarkFeatured();

        var sut = BuildSut([news]);

        var result = await sut.Handle(new GetNewsByIdQuery(news.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(news.Id);
        result.Data.TitleAr.Should().Be("عنوان");
        result.Data.TitleEn.Should().Be("Test News Title");
        result.Data.ContentAr.Should().Be("المحتوى العربي");
        result.Data.ContentEn.Should().Be("English content body");
        result.Data.TopicId.Should().Be(topicId);
        result.Data.AuthorId.Should().Be(authorId);
        result.Data.FeaturedImageUrl.Should().Be("https://example.com/image.jpg");
        result.Data.IsPublished.Should().BeTrue();
        result.Data.PublishedOn.Should().NotBeNull();
        result.Data.IsFeatured.Should().BeTrue();
    }

    private static GetNewsByIdQueryHandler BuildSut(IEnumerable<News> news)
    {
        var db = Substitute.For<ICceDbContext>();
        db.News.Returns(news.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new GetNewsByIdQueryHandler(db, new MessageFactory(localization));
    }
}
