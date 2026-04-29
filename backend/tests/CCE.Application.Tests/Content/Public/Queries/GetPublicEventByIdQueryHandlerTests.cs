using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.GetPublicEventById;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class GetPublicEventByIdQueryHandlerTests
{
    private static readonly System.DateTimeOffset BaseTime =
        new(2026, 6, 1, 10, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_dto_when_event_found()
    {
        var clock = new FakeSystemClock();
        var ev = CCE.Domain.Content.Event.Schedule(
            "حدث", "Test Event", "وصف", "Description",
            BaseTime, BaseTime.AddHours(2),
            "الرياض", "Riyadh", null, null, clock);

        var db = BuildDb(new[] { ev });
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
        var db = BuildDb(System.Array.Empty<CCE.Domain.Content.Event>());
        var sut = new GetPublicEventByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicEventByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<CCE.Domain.Content.Event> events)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Events.Returns(events.AsQueryable());
        db.Users.Returns(System.Array.Empty<CCE.Domain.Identity.User>().AsQueryable());
        db.Roles.Returns(System.Array.Empty<CCE.Domain.Identity.Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>>().AsQueryable());
        db.Resources.Returns(System.Array.Empty<CCE.Domain.Content.Resource>().AsQueryable());
        return db;
    }
}
