using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicEvents;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicEventsQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();
    private static readonly System.DateTimeOffset BaseTime =
        new(2026, 6, 1, 10, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_empty_paged_result_when_no_events_exist()
    {
        var db = BuildDb(Array.Empty<Event>());
        var sut = new ListPublicEventsQueryHandler(db);

        var result = await sut.Handle(new ListPublicEventsQuery(Page: 1, PageSize: 20,
            From: BaseTime, To: BaseTime.AddDays(30)), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_events_sorted_by_StartsOn_ascending()
    {
        var earlier = Event.Schedule("أ", "Earlier Event", "وصف", "Description A",
            BaseTime, BaseTime.AddHours(2), null, null, null, null, Clock);
        var later = Event.Schedule("ب", "Later Event", "وصف ب", "Description B",
            BaseTime.AddDays(1), BaseTime.AddDays(1).AddHours(2), null, null, null, null, Clock);

        var db = BuildDb([earlier, later]);
        var sut = new ListPublicEventsQueryHandler(db);

        var result = await sut.Handle(new ListPublicEventsQuery(Page: 1, PageSize: 20,
            From: BaseTime.AddMinutes(-1), To: BaseTime.AddDays(2)), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].TitleEn.Should().Be("Earlier Event");
        result.Items[1].TitleEn.Should().Be("Later Event");
    }

    [Fact]
    public async Task From_to_range_filter_returns_only_events_in_range()
    {
        var inRange = Event.Schedule("داخل النطاق", "In Range", "وصف", "Description",
            BaseTime.AddDays(5), BaseTime.AddDays(5).AddHours(1), null, null, null, null, Clock);
        var tooEarly = Event.Schedule("مبكر", "Too Early", "وصف", "Description",
            BaseTime.AddDays(-1), BaseTime.AddDays(-1).AddHours(1), null, null, null, null, Clock);
        var tooLate = Event.Schedule("متأخر", "Too Late", "وصف", "Description",
            BaseTime.AddDays(12), BaseTime.AddDays(12).AddHours(1), null, null, null, null, Clock);

        var db = BuildDb([inRange, tooEarly, tooLate]);
        var sut = new ListPublicEventsQueryHandler(db);

        var result = await sut.Handle(new ListPublicEventsQuery(Page: 1, PageSize: 20,
            From: BaseTime, To: BaseTime.AddDays(10)), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("In Range");
    }

    private static ICceDbContext BuildDb(IEnumerable<Event> events)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Events.Returns(events.AsQueryable());
        return db;
    }
}
