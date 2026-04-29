using CCE.Application.Search;
using CCE.Infrastructure;
using CCE.Infrastructure.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Tests.Search;

public class MeilisearchClientTests
{
    private const string MeiliUrl = "http://localhost:7700";
    private const string MeiliKey = "dev-meili-master-key-change-me";

    [Fact]
    public async Task Round_trips_a_document_via_index_and_search()
    {
        var sut = NewClient();
        await sut.EnsureIndexAsync(SearchableType.News, CancellationToken.None);

        var doc = new SearchableDocument
        {
            Id = System.Guid.NewGuid().ToString(),
            TitleEn = "Carbon capture breakthrough",
            TitleAr = "اختراق احتجاز الكربون",
            ContentEn = "A new method for carbon sequestration was announced today.",
            ContentAr = "أُعلن اليوم عن طريقة جديدة لاحتجاز الكربون.",
        };
        await sut.UpsertAsync(SearchableType.News, doc, CancellationToken.None);

        // Meilisearch indexing is async; allow up to 5 seconds for the document to appear.
        var deadline = System.DateTimeOffset.UtcNow.AddSeconds(5);
        CCE.Application.Common.Pagination.PagedResult<SearchHitDto>? result = null;
        while (System.DateTimeOffset.UtcNow < deadline)
        {
            result = await sut.SearchAsync("carbon", SearchableType.News, 1, 10, CancellationToken.None);
            if (result.Items.Any(h => h.Id.ToString() == doc.Id)) break;
            await Task.Delay(200);
        }
        result.Should().NotBeNull();
        result!.Items.Should().Contain(h => h.Id.ToString() == doc.Id);
    }

    [Fact]
    public async Task Searching_a_nonexistent_index_returns_empty()
    {
        var sut = NewClient();
        // Don't call EnsureIndexAsync — search a fresh type that has no docs.
        // Use Pages which is unlikely to have data this run.
        var result = await sut.SearchAsync("zzz-no-such-content-xyz", SearchableType.Pages, 1, 10, CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_removes_the_document()
    {
        var sut = NewClient();
        await sut.EnsureIndexAsync(SearchableType.Events, CancellationToken.None);

        var docId = System.Guid.NewGuid();
        var doc = new SearchableDocument
        {
            Id = docId.ToString(),
            TitleEn = "Decarbonization conference 2026",
            TitleAr = "مؤتمر إزالة الكربون 2026",
            ContentEn = "Annual gathering on decarbonization strategies.",
            ContentAr = "تجمع سنوي حول استراتيجيات إزالة الكربون.",
        };
        await sut.UpsertAsync(SearchableType.Events, doc, CancellationToken.None);

        // Wait for indexing
        await Task.Delay(1500);

        await sut.DeleteAsync(SearchableType.Events, docId, CancellationToken.None);
        await Task.Delay(1500);

        var result = await sut.SearchAsync("Decarbonization", SearchableType.Events, 1, 10, CancellationToken.None);
        result.Items.Should().NotContain(h => h.Id == docId);
    }

    private static MeilisearchClient NewClient()
    {
        var opts = Options.Create(new CceInfrastructureOptions
        {
            SqlConnectionString = "x", RedisConnectionString = "x",
            MeilisearchUrl = MeiliUrl, MeilisearchMasterKey = MeiliKey,
        });
        return new MeilisearchClient(opts, NullLogger<MeilisearchClient>.Instance);
    }
}
