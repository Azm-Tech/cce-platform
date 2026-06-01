using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.CreateNews;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class CreateNewsCommandHandlerTests
{
    private static readonly System.Guid TopicId = System.Guid.NewGuid();

    [Fact]
    public async Task Returns_not_authenticated_when_actor_unknown()
    {
        var (sut, _, _) = BuildSut(TopicId, noUser: true);

        var result = await sut.Handle(BuildCmd(TopicId), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Persists_news_when_inputs_valid()
    {
        var (sut, repo, db) = BuildSut(TopicId);

        var result = await sut.Handle(BuildCmd(TopicId), CancellationToken.None);

        result.Success.Should().BeTrue();
        await repo.Received(1).AddAsync(Arg.Any<News>(), Arg.Any<CancellationToken>());
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_dto_with_correct_fields()
    {
        var (sut, _, _) = BuildSut(TopicId);

        var result = await sut.Handle(BuildCmd(TopicId), CancellationToken.None);

        result.Data!.TitleAr.Should().Be("خبر");
        result.Data.TitleEn.Should().Be("News");
        result.Data.TopicId.Should().Be(TopicId);
        result.Data.IsPublished.Should().BeFalse();
    }

    private static CreateNewsCommand BuildCmd(System.Guid topicId) =>
        new("خبر", "News", "محتوى", "Content", topicId, null);

    private static (CreateNewsCommandHandler sut,
        IRepository<News, System.Guid> repo,
        ICceDbContext db) BuildSut(System.Guid topicId, bool noUser = false)
    {
        var repo = Substitute.For<IRepository<News, System.Guid>>();
        var db = Substitute.For<ICceDbContext>();
        var topic = CCE.Domain.Community.Topic.Create(
            "name-ar", "name-en", "desc-ar", "desc-en", "slug", null, null, 0);
        typeof(CCE.Domain.Community.Topic).GetProperty(nameof(CCE.Domain.Community.Topic.Id))!
            .SetValue(topic, topicId);
        db.Topics.Returns(new[] { topic }.AsQueryable());
        var user = Substitute.For<ICurrentUserAccessor>();
        if (noUser)
            user.GetUserId().Returns((System.Guid?)null);
        else
            user.GetUserId().Returns(System.Guid.NewGuid());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        var sut = new CreateNewsCommandHandler(repo, db, user, new FakeSystemClock(), new MessageFactory(localization));
        return (sut, repo, db);
    }
}
