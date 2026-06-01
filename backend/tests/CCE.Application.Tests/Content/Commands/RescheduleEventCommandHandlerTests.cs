using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.RescheduleEvent;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class RescheduleEventCommandHandlerTests
{
    private static readonly System.DateTimeOffset StartsOn =
        new(2026, 9, 1, 9, 0, 0, System.TimeSpan.Zero);

    private static readonly System.DateTimeOffset EndsOn =
        new(2026, 9, 1, 17, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_not_found_when_event_missing()
    {
        var (sut, _, _) = BuildSut(null);

        var result = await sut.Handle(
            new RescheduleEventCommand(System.Guid.NewGuid(), StartsOn, EndsOn),
            CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Reschedules_and_saves()
    {
        var clock = new FakeSystemClock();
        var ev = Event.Schedule(
            "ar", "en", "desc-ar", "desc-en",
            StartsOn, EndsOn, null, null, null, null, System.Guid.NewGuid(), clock);

        var (sut, db, _) = BuildSut(ev);
        var newStart = new System.DateTimeOffset(2026, 10, 1, 9, 0, 0, System.TimeSpan.Zero);
        var newEnd = new System.DateTimeOffset(2026, 10, 1, 17, 0, 0, System.TimeSpan.Zero);

        var result = await sut.Handle(
            new RescheduleEventCommand(ev.Id, newStart, newEnd),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.StartsOn.Should().Be(newStart);
        result.Data.EndsOn.Should().Be(newEnd);
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static (RescheduleEventCommandHandler sut, ICceDbContext db, IRepository<Event, System.Guid> repo) BuildSut(Event? evToReturn)
    {
        var repo = Substitute.For<IRepository<Event, System.Guid>>();
        repo.GetByIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns(evToReturn);
        var db = Substitute.For<ICceDbContext>();
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return (new RescheduleEventCommandHandler(repo, db, new MessageFactory(localization)), db, repo);
    }
}
