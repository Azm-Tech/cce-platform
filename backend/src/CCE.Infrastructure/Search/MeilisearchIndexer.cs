using System.Diagnostics.CodeAnalysis;
using CCE.Application.Common.Interfaces;
using CCE.Application.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Search;

/// <summary>
/// Background service that ensures the Meilisearch indexes exist and performs an initial backfill
/// from the database. Incremental updates flow through MediatR domain-event handlers
/// (<see cref="NewsPublishedIndexHandler"/>, <see cref="ResourcePublishedIndexHandler"/>,
/// <see cref="EventScheduledIndexHandler"/>).
/// </summary>
public sealed class MeilisearchIndexer : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MeilisearchIndexer> _logger;

    public MeilisearchIndexer(IServiceProvider services, ILogger<MeilisearchIndexer> logger)
    {
        _services = services;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Meilisearch is an optional auxiliary service. Any failure during startup " +
                        "backfill must not crash the host; the failure is logged and the host continues.")]
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            var search = scope.ServiceProvider.GetRequiredService<ISearchClient>();
            var db = scope.ServiceProvider.GetRequiredService<ICceDbContext>();

            foreach (var type in new[] { SearchableType.News, SearchableType.Events, SearchableType.Resources })
            {
                await search.EnsureIndexAsync(type, cancellationToken).ConfigureAwait(false);
            }

            await BackfillNewsAsync(db, search, cancellationToken).ConfigureAwait(false);
            await BackfillEventsAsync(db, search, cancellationToken).ConfigureAwait(false);
            await BackfillResourcesAsync(db, search, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Meilisearch backfill complete.");
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Meilisearch backfill failed; continuing without full index.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task BackfillNewsAsync(ICceDbContext db, ISearchClient search, CancellationToken ct)
    {
        var rows = await db.News
            .Where(n => n.PublishedOn != null)
            .Select(n => new SearchableDocument
            {
                Id = n.Id.ToString(),
                TitleAr = n.TitleAr,
                TitleEn = n.TitleEn,
                ContentAr = n.ContentAr,
                ContentEn = n.ContentEn,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var doc in rows)
        {
            await search.UpsertAsync(SearchableType.News, doc, ct).ConfigureAwait(false);
        }
    }

    private static async Task BackfillEventsAsync(ICceDbContext db, ISearchClient search, CancellationToken ct)
    {
        var rows = await db.Events
            .Select(e => new SearchableDocument
            {
                Id = e.Id.ToString(),
                TitleAr = e.TitleAr,
                TitleEn = e.TitleEn,
                ContentAr = e.DescriptionAr,
                ContentEn = e.DescriptionEn,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var doc in rows)
        {
            await search.UpsertAsync(SearchableType.Events, doc, ct).ConfigureAwait(false);
        }
    }

    private static async Task BackfillResourcesAsync(ICceDbContext db, ISearchClient search, CancellationToken ct)
    {
        var rows = await db.Resources
            .Where(r => r.PublishedOn != null)
            .Select(r => new SearchableDocument
            {
                Id = r.Id.ToString(),
                TitleAr = r.TitleAr,
                TitleEn = r.TitleEn,
                ContentAr = r.DescriptionAr,
                ContentEn = r.DescriptionEn,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var doc in rows)
        {
            await search.UpsertAsync(SearchableType.Resources, doc, ct).ConfigureAwait(false);
        }
    }
}
