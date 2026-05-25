using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListEvents;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class ListEventsQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();
    private static readonly System.DateTimeOffset BaseTime =
        new(2026, 6, 1, 10, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_empty_paged_result_when_no_events_exist()
    {
        var db = BuildDb(Array.Empty<Event>());
        var sut = new ListEventsQueryHandler(db);

        var result = await sut.Handle(new ListEventsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_events_sorted_by_StartsOn_descending()
    {
        var later = Event.Schedule("ب", "Later Event", "وصف ب", "Description B",
            BaseTime.AddDays(1), BaseTime.AddDays(1).AddHours(2), null, null, null, null, Clock);
        var earlier = Event.Schedule("أ", "Earlier Event", "وصف", "Description A",
            BaseTime, BaseTime.AddHours(2), null, null, null, null, Clock);

        var db = BuildDb([later, earlier]);
        var sut = new ListEventsQueryHandler(db);

        var result = await sut.Handle(new ListEventsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].TitleEn.Should().Be("Later Event");
        result.Items[1].TitleEn.Should().Be("Earlier Event");
    }

    [Fact]
    public async Task Search_filter_matches_title_ar_or_title_en()
    {
        var ev = Event.Schedule("مطابق", "matching-event", "وصف", "Description",
            BaseTime, BaseTime.AddHours(1), null, null, null, null, Clock);

        var db = BuildDb([ev]);
        var sut = new ListEventsQueryHandler(db);

        var result = await sut.Handle(new ListEventsQuery(Search: "matching"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("matching-event");
    }

    [Fact]
    public async Task FromDate_and_ToDate_filters_work()
    {
        var inRange = Event.Schedule("في النطاق", "InRange", "وصف", "Description",
            BaseTime.AddDays(5), BaseTime.AddDays(5).AddHours(1), null, null, null, null, Clock);
        var beforeRange = Event.Schedule("قبل", "Before", "وصف", "Description",
            BaseTime.AddDays(-1), BaseTime.AddDays(-1).AddHours(1), null, null, null, null, Clock);
        var afterRange = Event.Schedule("بعد", "After", "وصف", "Description",
            BaseTime.AddDays(10), BaseTime.AddDays(10).AddHours(1), null, null, null, null, Clock);

        var db = BuildDb([inRange, beforeRange, afterRange]);
        var sut = new ListEventsQueryHandler(db);

        var result = await sut.Handle(new ListEventsQuery(FromDate: BaseTime, ToDate: BaseTime.AddDays(7)), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("InRange");
    }

    private static ICceDbContext BuildDb(IEnumerable<Event> events)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Events.Returns(events.AsQueryable());
        return db;
    }
}
