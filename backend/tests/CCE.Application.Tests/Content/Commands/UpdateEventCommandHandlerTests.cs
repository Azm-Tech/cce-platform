using CCE.Application.Content;
using CCE.Application.Content.Commands.UpdateEvent;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateEventCommandHandlerTests
{
    private static readonly System.DateTimeOffset StartsOn =
        new(2026, 9, 1, 9, 0, 0, System.TimeSpan.Zero);

    private static readonly System.DateTimeOffset EndsOn =
        new(2026, 9, 1, 17, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_null_when_event_not_found()
    {
        var service = Substitute.For<IEventRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Event?)null);
        var sut = new UpdateEventCommandHandler(service);

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Updates_content_and_calls_UpdateAsync_with_expected_rowversion()
    {
        var clock = new FakeSystemClock();
        var ev = Event.Schedule(
            "old-ar", "old-en", "old-desc-ar", "old-desc-en",
            StartsOn, EndsOn, null, null, null, null, clock);

        var service = Substitute.For<IEventRepository>();
        service.FindAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        var sut = new UpdateEventCommandHandler(service);
        var rowVersion = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var cmd = new UpdateEventCommand(
            ev.Id,
            "new-ar", "new-en", "new-desc-ar", "new-desc-en",
            "الرياض", "Riyadh",
            null, null,
            rowVersion);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TitleEn.Should().Be("new-en");
        result.DescriptionAr.Should().Be("new-desc-ar");
        await service.Received(1).UpdateAsync(ev, rowVersion, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_ConcurrencyException_from_UpdateAsync()
    {
        var clock = new FakeSystemClock();
        var ev = Event.Schedule(
            "ar", "en", "desc-ar", "desc-en",
            StartsOn, EndsOn, null, null, null, null, clock);

        var service = Substitute.For<IEventRepository>();
        service.FindAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        service.UpdateAsync(default!, default!, default).ReturnsForAnyArgs<Task>(_ =>
            throw new ConcurrencyException("conflict"));

        var sut = new UpdateEventCommandHandler(service);
        var cmd = BuildCommand(ev.Id);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    private static UpdateEventCommand BuildCommand(System.Guid id) =>
        new(id, "ar", "en", "desc-ar", "desc-en", null, null, null, null, new byte[8]);
}
