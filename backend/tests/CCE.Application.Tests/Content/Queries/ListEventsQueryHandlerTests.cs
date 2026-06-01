using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListEvents;
using CCE.Application.Localization;
using CCE.Application.Messages;
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
        var sut = BuildSut(Array.Empty<Event>());

        var result = await sut.Handle(new ListEventsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_events_sorted_by_StartsOn_descending()
    {
        var topicId = System.Guid.NewGuid();
        var later = Event.Schedule("ب", "Later Event", "وصف ب", "Description B",
            BaseTime.AddDays(1), BaseTime.AddDays(1).AddHours(2), null, null, null, null, topicId, Clock);
        var earlier = Event.Schedule("أ", "Earlier Event", "وصف", "Description A",
            BaseTime, BaseTime.AddHours(2), null, null, null, null, topicId, Clock);

        var sut = BuildSut([later, earlier]);

        var result = await sut.Handle(new ListEventsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Data!.Total.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items[0].TitleEn.Should().Be("Later Event");
        result.Data.Items[1].TitleEn.Should().Be("Earlier Event");
    }

    [Fact]
    public async Task Search_filter_matches_title_ar_or_title_en()
    {
        var topicId = System.Guid.NewGuid();
        var ev = Event.Schedule("مطابق", "matching-event", "وصف", "Description",
            BaseTime, BaseTime.AddHours(1), null, null, null, null, topicId, Clock);

        var sut = BuildSut([ev]);

        var result = await sut.Handle(new ListEventsQuery(Search: "matching"), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("matching-event");
    }

    [Fact]
    public async Task FromDate_and_ToDate_filters_work()
    {
        var topicId = System.Guid.NewGuid();
        var inRange = Event.Schedule("في النطاق", "InRange", "وصف", "Description",
            BaseTime.AddDays(5), BaseTime.AddDays(5).AddHours(1), null, null, null, null, topicId, Clock);
        var beforeRange = Event.Schedule("قبل", "Before", "وصف", "Description",
            BaseTime.AddDays(-1), BaseTime.AddDays(-1).AddHours(1), null, null, null, null, topicId, Clock);
        var afterRange = Event.Schedule("بعد", "After", "وصف", "Description",
            BaseTime.AddDays(10), BaseTime.AddDays(10).AddHours(1), null, null, null, null, topicId, Clock);

        var sut = BuildSut([inRange, beforeRange, afterRange]);

        var result = await sut.Handle(new ListEventsQuery(FromDate: BaseTime, ToDate: BaseTime.AddDays(7)), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("InRange");
    }

    private static ListEventsQueryHandler BuildSut(IEnumerable<Event> events)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Events.Returns(events.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new ListEventsQueryHandler(db, new MessageFactory(localization));
    }
}
