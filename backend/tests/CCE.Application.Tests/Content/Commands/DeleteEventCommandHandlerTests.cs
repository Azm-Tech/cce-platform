using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Commands.DeleteEvent;
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
    public async Task Throws_KeyNotFound_when_event_missing()
    {
        var service = Substitute.For<IEventService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Event?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var sut = new DeleteEventCommandHandler(service, currentUser, new FakeSystemClock());

        var act = async () => await sut.Handle(new DeleteEventCommand(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var clock = new FakeSystemClock();
        var ev = Event.Schedule(
            "ar", "en", "desc-ar", "desc-en",
            StartsOn, EndsOn, null, null, null, null, clock);

        var service = Substitute.For<IEventService>();
        service.FindAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = new DeleteEventCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(new DeleteEventCommand(ev.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Soft_deletes_and_calls_UpdateAsync()
    {
        var clock = new FakeSystemClock();
        var actorId = System.Guid.NewGuid();
        var ev = Event.Schedule(
            "ar", "en", "desc-ar", "desc-en",
            StartsOn, EndsOn, null, null, null, null, clock);

        var service = Substitute.For<IEventService>();
        service.FindAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(actorId);

        var sut = new DeleteEventCommandHandler(service, currentUser, clock);

        await sut.Handle(new DeleteEventCommand(ev.Id), CancellationToken.None);

        ev.IsDeleted.Should().BeTrue();
        await service.Received(1).UpdateAsync(ev, Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }
}
