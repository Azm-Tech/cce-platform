using CCE.Application.Content;
using CCE.Application.Content.Commands.RescheduleEvent;
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
    public async Task Returns_null_when_event_not_found()
    {
        var service = Substitute.For<IEventService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Event?)null);
        var sut = new RescheduleEventCommandHandler(service);

        var result = await sut.Handle(
            new RescheduleEventCommand(System.Guid.NewGuid(), StartsOn, EndsOn, new byte[8]),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Reschedules_and_calls_UpdateAsync()
    {
        var clock = new FakeSystemClock();
        var ev = Event.Schedule(
            "ar", "en", "desc-ar", "desc-en",
            StartsOn, EndsOn, null, null, null, null, clock);

        var service = Substitute.For<IEventService>();
        service.FindAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        var sut = new RescheduleEventCommandHandler(service);
        var newStart = new System.DateTimeOffset(2026, 10, 1, 9, 0, 0, System.TimeSpan.Zero);
        var newEnd = new System.DateTimeOffset(2026, 10, 1, 17, 0, 0, System.TimeSpan.Zero);
        var rowVersion = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var result = await sut.Handle(
            new RescheduleEventCommand(ev.Id, newStart, newEnd, rowVersion),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.StartsOn.Should().Be(newStart);
        result.EndsOn.Should().Be(newEnd);
        await service.Received(1).UpdateAsync(ev, rowVersion, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_ConcurrencyException_from_UpdateAsync()
    {
        var clock = new FakeSystemClock();
        var ev = Event.Schedule(
            "ar", "en", "desc-ar", "desc-en",
            StartsOn, EndsOn, null, null, null, null, clock);

        var service = Substitute.For<IEventService>();
        service.FindAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        service.UpdateAsync(default!, default!, default).ReturnsForAnyArgs<Task>(_ =>
            throw new ConcurrencyException("conflict"));

        var sut = new RescheduleEventCommandHandler(service);

        var act = async () => await sut.Handle(
            new RescheduleEventCommand(ev.Id, StartsOn, EndsOn, new byte[8]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
    }
}
