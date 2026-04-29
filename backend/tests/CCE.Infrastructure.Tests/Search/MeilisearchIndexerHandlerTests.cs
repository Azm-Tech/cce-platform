// NOTE: MeilisearchIndexer.StartAsync backfill is deferred to E2E / integration coverage.
// The incremental handlers are unit-tested here using an EF Core InMemory database (which
// provides a real IAsyncQueryProvider that FirstOrDefaultAsync requires) plus an NSubstitute
// ISearchClient to capture the UpsertAsync call.

using CCE.Application.Search;
using CCE.Domain.Content;
using CCE.Domain.Content.Events;
using CCE.Infrastructure.Persistence;
using CCE.Infrastructure.Search;
using CCE.TestInfrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Search;

public class MeilisearchIndexerHandlerTests
{
    private static readonly System.DateTimeOffset BaseTime =
        new(2026, 6, 1, 10, 0, 0, System.TimeSpan.Zero);

    private static CceDbContext BuildDb()
    {
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options;
        return new CceDbContext(options);
    }

    // ── News ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task News_published_handler_upserts_document()
    {
        var clock = new FakeSystemClock();
        var news = News.Draft(
            "عنوان خبر",
            "News Title",
            "محتوى عربي",
            "English content",
            "news-title",
            System.Guid.NewGuid(),
            null,
            clock);
        news.Publish(clock);

        await using var db = BuildDb();
        db.News.Add(news);
        await db.SaveChangesAsync();

        var search = Substitute.For<ISearchClient>();
        var sut = new NewsPublishedIndexHandler(db, search, NullLogger<NewsPublishedIndexHandler>.Instance);

        var evt = new NewsPublishedEvent(news.Id, news.Slug, clock.UtcNow);
        await sut.Handle(evt, CancellationToken.None);

        await search.Received(1).UpsertAsync(
            SearchableType.News,
            Arg.Is<SearchableDocument>(d =>
                d.Id == news.Id.ToString() &&
                d.TitleAr == "عنوان خبر" &&
                d.TitleEn == "News Title" &&
                d.ContentAr == "محتوى عربي" &&
                d.ContentEn == "English content"),
            Arg.Any<CancellationToken>());
    }

    // ── Resource ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resource_published_handler_upserts_document()
    {
        var clock = new FakeSystemClock();
        var resource = Resource.Draft(
            "عنوان مورد",
            "Resource Title",
            "وصف عربي",
            "English description",
            ResourceType.Document,
            categoryId: System.Guid.NewGuid(),
            countryId: null,
            uploadedById: System.Guid.NewGuid(),
            assetFileId: System.Guid.NewGuid(),
            clock);
        resource.Publish(clock);

        await using var db = BuildDb();
        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        var search = Substitute.For<ISearchClient>();
        var sut = new ResourcePublishedIndexHandler(db, search, NullLogger<ResourcePublishedIndexHandler>.Instance);

        var evt = new ResourcePublishedEvent(resource.Id, null, resource.CategoryId, clock.UtcNow);
        await sut.Handle(evt, CancellationToken.None);

        await search.Received(1).UpsertAsync(
            SearchableType.Resources,
            Arg.Is<SearchableDocument>(d =>
                d.Id == resource.Id.ToString() &&
                d.TitleAr == "عنوان مورد" &&
                d.TitleEn == "Resource Title" &&
                d.ContentAr == "وصف عربي" &&
                d.ContentEn == "English description"),
            Arg.Any<CancellationToken>());
    }

    // ── Event ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Event_scheduled_handler_upserts_document()
    {
        var clock = new FakeSystemClock();
        var ev = CCE.Domain.Content.Event.Schedule(
            "عنوان حدث",
            "Event Title",
            "وصف عربي للحدث",
            "English event description",
            BaseTime,
            BaseTime.AddHours(2),
            locationAr: null,
            locationEn: null,
            onlineMeetingUrl: null,
            featuredImageUrl: null,
            clock);

        await using var db = BuildDb();
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var search = Substitute.For<ISearchClient>();
        var sut = new EventScheduledIndexHandler(db, search, NullLogger<EventScheduledIndexHandler>.Instance);

        var evt = new EventScheduledEvent(ev.Id, BaseTime, BaseTime.AddHours(2), clock.UtcNow);
        await sut.Handle(evt, CancellationToken.None);

        await search.Received(1).UpsertAsync(
            SearchableType.Events,
            Arg.Is<SearchableDocument>(d =>
                d.Id == ev.Id.ToString() &&
                d.TitleAr == "عنوان حدث" &&
                d.TitleEn == "Event Title" &&
                d.ContentAr == "وصف عربي للحدث" &&
                d.ContentEn == "English event description"),
            Arg.Any<CancellationToken>());
    }
}
