using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.DeleteNews;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class DeleteNewsCommandHandlerTests
{
    [Fact]
    public async Task Returns_not_found_when_news_missing()
    {
        var (sut, _, _, _) = BuildSut();
        // repo returns null for any id
        var result = await sut.Handle(new DeleteNewsCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_not_authenticated_when_actor_unknown()
    {
        var clock = new FakeSystemClock();
        var news = News.Draft("ar", "en", "content-ar", "content-en", System.Guid.NewGuid(), System.Guid.NewGuid(), null, clock);

        var repo = Substitute.For<IRepository<News, System.Guid>>();
        repo.GetByIdAsync(news.Id, Arg.Any<CancellationToken>()).Returns(news);

        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = BuildHandler(repo, db, currentUser, clock);

        var result = await sut.Handle(new DeleteNewsCommand(news.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Soft_deletes_and_saves_via_db_context()
    {
        var clock = new FakeSystemClock();
        var actorId = System.Guid.NewGuid();
        var news = News.Draft("ar", "en", "content-ar", "content-en", System.Guid.NewGuid(), System.Guid.NewGuid(), null, clock);

        var repo = Substitute.For<IRepository<News, System.Guid>>();
        repo.GetByIdAsync(news.Id, Arg.Any<CancellationToken>()).Returns(news);

        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(actorId);

        var sut = BuildHandler(repo, db, currentUser, clock);

        var result = await sut.Handle(new DeleteNewsCommand(news.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        news.IsDeleted.Should().BeTrue();
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static (DeleteNewsCommandHandler sut,
        IRepository<News, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor user) BuildSut()
    {
        var repo = Substitute.For<IRepository<News, System.Guid>>();
        repo.GetByIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((News?)null);
        var db = Substitute.For<ICceDbContext>();
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns(System.Guid.NewGuid());
        return (BuildHandler(repo, db, user, new FakeSystemClock()), repo, db, user);
    }

    private static DeleteNewsCommandHandler BuildHandler(
        IRepository<News, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new DeleteNewsCommandHandler(repo, db, currentUser, clock, new MessageFactory(localization));
    }
}
