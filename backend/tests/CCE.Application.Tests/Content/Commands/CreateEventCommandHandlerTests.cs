using CCE.Application.Content;
using CCE.Application.Content.Commands.CreateEvent;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class CreateEventCommandHandlerTests
{
    private static readonly System.DateTimeOffset StartsOn =
        new(2026, 9, 1, 9, 0, 0, System.TimeSpan.Zero);

    private static readonly System.DateTimeOffset EndsOn =
        new(2026, 9, 1, 17, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Persists_event_when_inputs_valid()
    {
        var (sut, service) = BuildSut();

        await sut.Handle(BuildCmd(), CancellationToken.None);

        await service.Received(1).SaveAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_dto_with_correct_fields()
    {
        var (sut, _) = BuildSut();

        var dto = await sut.Handle(BuildCmd(), CancellationToken.None);

        dto.TitleAr.Should().Be("حدث");
        dto.TitleEn.Should().Be("Event");
        dto.StartsOn.Should().Be(StartsOn);
        dto.EndsOn.Should().Be(EndsOn);
        dto.ICalUid.Should().EndWith("@cce.moenergy.gov.sa");
    }

    private static CreateEventCommand BuildCmd() =>
        new("حدث", "Event", "وصف", "Description", StartsOn, EndsOn,
            null, null, null, null);

    private static (CreateEventCommandHandler sut, IEventRepository service) BuildSut()
    {
        var service = Substitute.For<IEventRepository>();
        var sut = new CreateEventCommandHandler(service, new FakeSystemClock());
        return (sut, service);
    }
}
