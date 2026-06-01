using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.GetPublicEventById;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class GetPublicEventByIdQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();
    private static readonly System.DateTimeOffset BaseTime =
        new(2026, 6, 1, 10, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_dto_when_event_found()
    {
        var ev = Event.Schedule("حدث", "Test Event", "وصف", "Description",
            BaseTime, BaseTime.AddHours(2), "الرياض", "Riyadh", null, null, System.Guid.NewGuid(), Clock);

        var sut = BuildSut([ev]);

        var result = await sut.Handle(new GetPublicEventByIdQuery(ev.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(ev.Id);
        result.Data.TitleEn.Should().Be("Test Event");
        result.Data.StartsOn.Should().Be(BaseTime);
        result.Data.EndsOn.Should().Be(BaseTime.AddHours(2));
        result.Data.LocationAr.Should().Be("الرياض");
        result.Data.LocationEn.Should().Be("Riyadh");
        result.Data.ICalUid.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Returns_not_found_when_event_missing()
    {
        var sut = BuildSut(Array.Empty<Event>());

        var result = await sut.Handle(new GetPublicEventByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    private static GetPublicEventByIdQueryHandler BuildSut(IEnumerable<Event> events)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Events.Returns(events.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new GetPublicEventByIdQueryHandler(db, new MessageFactory(localization));
    }
}
