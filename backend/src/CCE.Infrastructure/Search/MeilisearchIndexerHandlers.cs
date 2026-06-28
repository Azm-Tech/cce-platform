using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CCE.Application.Common.Interfaces;
using CCE.Application.Search;
using CCE.Domain.Community.Events;
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

/// <summary>
/// Indexes a newly published community post into the community_posts Meilisearch index.
/// Triggered by <see cref="PostCreatedEvent"/> raised from <see cref="CCE.Domain.Community.Post.Publish"/>.
/// Failures are swallowed so search-index errors never break the publish transaction.
/// SEARCH-INDEX-NOTE: When a published-post edit feature is added, raise a PostEditedEvent and add a
/// PostEditedSearchIndexHandler that re-upserts the post document with updated title/content/tags.
/// </summary>
public sealed class PostCreatedSearchIndexHandler : INotificationHandler<PostCreatedEvent>
{
    private readonly ICceDbContext _db;
    private readonly ISearchClient _search;
    private readonly ILogger<PostCreatedSearchIndexHandler> _logger;

    public PostCreatedSearchIndexHandler(ICceDbContext db, ISearchClient search, ILogger<PostCreatedSearchIndexHandler> logger)
    {
        _db = db;
        _search = search;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Search-index failures must not break the publish flow. Any exception is " +
                        "logged and swallowed so the originating command transaction is unaffected.")]
    public async Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var post = await _db.Posts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == notification.PostId, cancellationToken)
                .ConfigureAwait(false);
            if (post is null) return;

            var author = await _db.Users
                .Where(u => u.Id == notification.AuthorId)
                .Select(u => new { u.FirstName, u.LastName, u.UserName })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var authorName = BuildAuthorName(author?.FirstName, author?.LastName, author?.UserName);
            var tagNamesAr = string.Join(' ', post.Tags.Select(t => t.NameAr).Where(n => !string.IsNullOrEmpty(n)));
            var tagNamesEn = string.Join(' ', post.Tags.Select(t => t.NameEn).Where(n => !string.IsNullOrEmpty(n)));

            var doc = new CommunityPostDocument
            {
                Id         = post.Id.ToString(),
                TitleAr    = post.Locale == "ar" ? post.Title : null,
                TitleEn    = post.Locale == "en" ? post.Title : null,
                ContentAr  = post.Locale == "ar" ? post.Content : null,
                ContentEn  = post.Locale == "en" ? post.Content : null,
                AuthorName = authorName,
                TagNamesAr = string.IsNullOrEmpty(tagNamesAr) ? null : tagNamesAr,
                TagNamesEn = string.IsNullOrEmpty(tagNamesEn) ? null : tagNamesEn,
            };

            await _search.UpsertAsync(SearchableType.CommunityPosts, doc, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to index post {PostId} to community_posts", notification.PostId);
        }
    }

    internal static string? BuildAuthorName(string? firstName, string? lastName, string? userName)
    {
        var full = $"{firstName} {lastName}".Trim();
        return string.IsNullOrEmpty(full) ? userName : full;
    }
}

/// <summary>
/// Indexes a new community reply into the community_replies Meilisearch index.
/// Triggered by <see cref="ReplyCreatedEvent"/> raised from <see cref="CCE.Domain.Community.Post.RegisterReply"/>.
/// Fetches the full reply from the database (not the 200-char snippet in the event) so that all
/// reply content is searchable regardless of length.
/// Failures are swallowed so search-index errors never break the reply creation flow.
/// SEARCH-INDEX-NOTE: When reply soft-delete is implemented, call DeleteAsync(CommunityReplies, replyId, ct).
/// SEARCH-INDEX-NOTE: When reply edit is implemented, call UpsertAsync with the updated CommunityReplyDocument.
/// </summary>
public sealed class ReplyCreatedSearchIndexHandler : INotificationHandler<ReplyCreatedEvent>
{
    private readonly ICceDbContext _db;
    private readonly ISearchClient _search;
    private readonly ILogger<ReplyCreatedSearchIndexHandler> _logger;

    public ReplyCreatedSearchIndexHandler(ICceDbContext db, ISearchClient search, ILogger<ReplyCreatedSearchIndexHandler> logger)
    {
        _db = db;
        _search = search;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Search-index failures must not break the reply creation flow. Any exception is " +
                        "logged and swallowed so the originating command transaction is unaffected.")]
    public async Task Handle(ReplyCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var reply = await _db.PostReplies
                .FirstOrDefaultAsync(r => r.Id == notification.ReplyId, cancellationToken)
                .ConfigureAwait(false);
            if (reply is null) return;

            var author = await _db.Users
                .Where(u => u.Id == notification.AuthorId)
                .Select(u => new { u.FirstName, u.LastName, u.UserName })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var doc = new CommunityReplyDocument
            {
                Id        = reply.Id.ToString(),
                PostId    = reply.PostId.ToString(),
                ContentAr = reply.Locale == "ar" ? reply.Content : null,
                ContentEn = reply.Locale == "en" ? reply.Content : null,
                AuthorName = PostCreatedSearchIndexHandler.BuildAuthorName(
                    author?.FirstName, author?.LastName, author?.UserName),
            };

            await _search.UpsertAsync(SearchableType.CommunityReplies, doc, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to index reply {ReplyId} to community_replies", notification.ReplyId);
        }
    }
}
