using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.GetEventById;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class GetEventByIdQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();
    private static readonly System.DateTimeOffset BaseTime =
        new(2026, 6, 1, 10, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_not_found_when_event_missing()
    {
        var sut = BuildSut(Array.Empty<Event>());

        var result = await sut.Handle(new GetEventByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_found()
    {
        var topicId = System.Guid.NewGuid();
        var ev = Event.Schedule("حدث تجريبي", "Test Event Title", "وصف عربي", "English description",
            BaseTime, BaseTime.AddHours(3), "الرياض", "Riyadh",
            "https://example.com/meeting", "https://example.com/image.jpg", topicId, Clock);

        var sut = BuildSut([ev]);

        var result = await sut.Handle(new GetEventByIdQuery(ev.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(ev.Id);
        result.Data.TitleAr.Should().Be("حدث تجريبي");
        result.Data.TitleEn.Should().Be("Test Event Title");
        result.Data.DescriptionAr.Should().Be("وصف عربي");
        result.Data.DescriptionEn.Should().Be("English description");
        result.Data.StartsOn.Should().Be(BaseTime);
        result.Data.EndsOn.Should().Be(BaseTime.AddHours(3));
        result.Data.LocationAr.Should().Be("الرياض");
        result.Data.LocationEn.Should().Be("Riyadh");
        result.Data.OnlineMeetingUrl.Should().Be("https://example.com/meeting");
        result.Data.FeaturedImageUrl.Should().Be("https://example.com/image.jpg");
        result.Data.ICalUid.Should().EndWith("@cce.moenergy.gov.sa");
    }

    private static GetEventByIdQueryHandler BuildSut(IEnumerable<Event> events)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Events.Returns(events.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new GetEventByIdQueryHandler(db, new MessageFactory(localization));
    }
}
