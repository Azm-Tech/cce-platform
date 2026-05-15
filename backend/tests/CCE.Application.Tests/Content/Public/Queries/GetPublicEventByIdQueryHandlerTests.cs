using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.GetPublicEventById;
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
            BaseTime, BaseTime.AddHours(2), "الرياض", "Riyadh", null, null, Clock);

        var db = BuildDb([ev]);
        var sut = new GetPublicEventByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicEventByIdQuery(ev.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(ev.Id);
        result.TitleEn.Should().Be("Test Event");
        result.StartsOn.Should().Be(BaseTime);
        result.EndsOn.Should().Be(BaseTime.AddHours(2));
        result.LocationAr.Should().Be("الرياض");
        result.LocationEn.Should().Be("Riyadh");
        result.ICalUid.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Returns_null_when_event_not_found()
    {
        var db = BuildDb(Array.Empty<Event>());
        var sut = new GetPublicEventByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicEventByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<Event> events)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Events.Returns(events.AsQueryable());
        return db;
    }
}
