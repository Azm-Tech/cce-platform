using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.GetEventById;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class GetEventByIdQueryHandlerTests
{
    private static readonly System.DateTimeOffset BaseTime =
        new(2026, 6, 1, 10, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_null_when_event_not_found()
    {
        var db = BuildDb(System.Array.Empty<CCE.Domain.Content.Event>());
        var sut = new GetEventByIdQueryHandler(db);

        var result = await sut.Handle(new GetEventByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_found()
    {
        var clock = new FakeSystemClock();
        var ev = CCE.Domain.Content.Event.Schedule(
            "حدث تجريبي",
            "Test Event Title",
            "وصف عربي",
            "English description",
            BaseTime,
            BaseTime.AddHours(3),
            "الرياض", "Riyadh",
            "https://example.com/meeting",
            "https://example.com/image.jpg",
            clock);

        var db = BuildDb(new[] { ev });
        var sut = new GetEventByIdQueryHandler(db);

        var result = await sut.Handle(new GetEventByIdQuery(ev.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(ev.Id);
        result.TitleAr.Should().Be("حدث تجريبي");
        result.TitleEn.Should().Be("Test Event Title");
        result.DescriptionAr.Should().Be("وصف عربي");
        result.DescriptionEn.Should().Be("English description");
        result.StartsOn.Should().Be(BaseTime);
        result.EndsOn.Should().Be(BaseTime.AddHours(3));
        result.LocationAr.Should().Be("الرياض");
        result.LocationEn.Should().Be("Riyadh");
        result.OnlineMeetingUrl.Should().Be("https://example.com/meeting");
        result.FeaturedImageUrl.Should().Be("https://example.com/image.jpg");
        result.ICalUid.Should().EndWith("@cce.moenergy.gov.sa");
        result.RowVersion.Should().NotBeNull();
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
