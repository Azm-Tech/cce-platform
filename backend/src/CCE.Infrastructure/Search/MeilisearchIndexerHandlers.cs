using System.Diagnostics.CodeAnalysis;
using CCE.Application.Common.Interfaces;
using CCE.Application.Search;
using CCE.Domain.Content.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Search;

public sealed class NewsPublishedIndexHandler : INotificationHandler<NewsPublishedEvent>
{
    private readonly ICceDbContext _db;
    private readonly ISearchClient _search;
    private readonly ILogger<NewsPublishedIndexHandler> _logger;

    public NewsPublishedIndexHandler(ICceDbContext db, ISearchClient search, ILogger<NewsPublishedIndexHandler> logger)
    {
        _db = db;
        _search = search;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Search-index failures must not break the publish flow. Any exception is " +
                        "logged and swallowed so the originating command transaction is unaffected.")]
    public async Task Handle(NewsPublishedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var n = await _db.News.FirstOrDefaultAsync(x => x.Id == notification.NewsId, cancellationToken).ConfigureAwait(false);
            if (n is null) return;
            await _search.UpsertAsync(SearchableType.News, new SearchableDocument
            {
                Id = n.Id.ToString(),
                TitleAr = n.TitleAr,
                TitleEn = n.TitleEn,
                ContentAr = n.ContentAr,
                ContentEn = n.ContentEn,
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to index news {NewsId}", notification.NewsId);
        }
    }
}

public sealed class ResourcePublishedIndexHandler : INotificationHandler<ResourcePublishedEvent>
{
    private readonly ICceDbContext _db;
    private readonly ISearchClient _search;
    private readonly ILogger<ResourcePublishedIndexHandler> _logger;

    public ResourcePublishedIndexHandler(ICceDbContext db, ISearchClient search, ILogger<ResourcePublishedIndexHandler> logger)
    {
        _db = db;
        _search = search;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Search-index failures must not break the publish flow. Any exception is " +
                        "logged and swallowed so the originating command transaction is unaffected.")]
    public async Task Handle(ResourcePublishedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var r = await _db.Resources.FirstOrDefaultAsync(x => x.Id == notification.ResourceId, cancellationToken).ConfigureAwait(false);
            if (r is null) return;
            await _search.UpsertAsync(SearchableType.Resources, new SearchableDocument
            {
                Id = r.Id.ToString(),
                TitleAr = r.TitleAr,
                TitleEn = r.TitleEn,
                ContentAr = r.DescriptionAr,
                ContentEn = r.DescriptionEn,
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to index resource {ResourceId}", notification.ResourceId);
        }
    }
}

public sealed class EventScheduledIndexHandler : INotificationHandler<EventScheduledEvent>
{
    private readonly ICceDbContext _db;
    private readonly ISearchClient _search;
    private readonly ILogger<EventScheduledIndexHandler> _logger;

    public EventScheduledIndexHandler(ICceDbContext db, ISearchClient search, ILogger<EventScheduledIndexHandler> logger)
    {
        _db = db;
        _search = search;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Search-index failures must not break the publish flow. Any exception is " +
                        "logged and swallowed so the originating command transaction is unaffected.")]
    public async Task Handle(EventScheduledEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var e = await _db.Events.FirstOrDefaultAsync(x => x.Id == notification.EventId, cancellationToken).ConfigureAwait(false);
            if (e is null) return;
            await _search.UpsertAsync(SearchableType.Events, new SearchableDocument
            {
                Id = e.Id.ToString(),
                TitleAr = e.TitleAr,
                TitleEn = e.TitleEn,
                ContentAr = e.DescriptionAr,
                ContentEn = e.DescriptionEn,
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to index event {EventId}", notification.EventId);
        }
    }
}
