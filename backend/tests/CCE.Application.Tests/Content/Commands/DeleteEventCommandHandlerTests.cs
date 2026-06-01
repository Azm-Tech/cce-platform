using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.DeleteEvent;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class DeleteEventCommandHandlerTests
{
    private static readonly System.DateTimeOffset StartsOn =
        new(2026, 9, 1, 9, 0, 0, System.TimeSpan.Zero);

    private static readonly System.DateTimeOffset EndsOn =
        new(2026, 9, 1, 17, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_not_found_when_event_missing()
    {
        var (sut, _, _, _) = BuildSut();

        var result = await sut.Handle(new DeleteEventCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_not_authenticated_when_actor_unknown()
    {
        var clock = new FakeSystemClock();
        var ev = Event.Schedule(
            "ar", "en", "desc-ar", "desc-en",
            StartsOn, EndsOn, null, null, null, null, System.Guid.NewGuid(), clock);

        var repo = Substitute.For<IRepository<Event, System.Guid>>();
        repo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = BuildHandler(repo, db, currentUser, clock);

        var result = await sut.Handle(new DeleteEventCommand(ev.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Soft_deletes_and_saves_via_db_context()
    {
        var clock = new FakeSystemClock();
        var actorId = System.Guid.NewGuid();
        var ev = Event.Schedule(
            "ar", "en", "desc-ar", "desc-en",
            StartsOn, EndsOn, null, null, null, null, System.Guid.NewGuid(), clock);

        var repo = Substitute.For<IRepository<Event, System.Guid>>();
        repo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(actorId);

        var sut = BuildHandler(repo, db, currentUser, clock);

        var result = await sut.Handle(new DeleteEventCommand(ev.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        ev.IsDeleted.Should().BeTrue();
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static (DeleteEventCommandHandler sut,
        IRepository<Event, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor user) BuildSut()
    {
        var repo = Substitute.For<IRepository<Event, System.Guid>>();
        repo.GetByIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Event?)null);
        var db = Substitute.For<ICceDbContext>();
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns(System.Guid.NewGuid());
        return (BuildHandler(repo, db, user, new FakeSystemClock()), repo, db, user);
    }

    private static DeleteEventCommandHandler BuildHandler(
        IRepository<Event, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new DeleteEventCommandHandler(repo, db, currentUser, clock, new MessageFactory(localization));
    }
}
