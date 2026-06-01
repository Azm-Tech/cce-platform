using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.UpdateNews;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateNewsCommandHandlerTests
{
    [Fact]
    public async Task Returns_not_found_when_news_missing()
    {
        var (sut, _, _) = BuildSut(null, System.Guid.NewGuid());

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Updates_content_and_saves()
    {
        var clock = new FakeSystemClock();
        var topicId = System.Guid.NewGuid();
        var news = News.Draft("old-ar", "old-en", "old-content-ar", "old-content-en",
            topicId, System.Guid.NewGuid(), null, clock);

        var (sut, db, _) = BuildSut(news, topicId);

        var cmd = new UpdateNewsCommand(
            news.Id,
            "new-ar", "new-en", "new-content-ar", "new-content-en",
            topicId, null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.TitleEn.Should().Be("new-en");
        result.Data.TopicId.Should().Be(topicId);
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static UpdateNewsCommand BuildCommand(System.Guid id) =>
        new(id, "ar", "en", "content-ar", "content-en", System.Guid.NewGuid(), null);

    private static (UpdateNewsCommandHandler sut, ICceDbContext db, IRepository<News, System.Guid> repo) BuildSut(News? newsToReturn, System.Guid topicId)
    {
        var repo = Substitute.For<IRepository<News, System.Guid>>();
        repo.GetByIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns(newsToReturn);
        var db = Substitute.For<ICceDbContext>();
        var topic = CCE.Domain.Community.Topic.Create(
            "name-ar", "name-en", "desc-ar", "desc-en", "slug", null, null, 0);
        typeof(CCE.Domain.Community.Topic).GetProperty(nameof(CCE.Domain.Community.Topic.Id))!
            .SetValue(topic, topicId);
        db.Topics.Returns(new[] { topic }.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return (new UpdateNewsCommandHandler(repo, db, new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance)), db, repo);
    }
}
