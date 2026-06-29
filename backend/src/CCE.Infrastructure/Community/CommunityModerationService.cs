using CCE.Application.Community;
using CCE.Application.Search;
using CCE.Domain.Community;
using CCE.Infrastructure.Persistence;
using CCE.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

// Needed for PostCreatedSearchIndexHandler.BuildAuthorName (internal helper in the same assembly).
// ReSharper disable once RedundantUsingDirective

namespace CCE.Infrastructure.Community;

public sealed class CommunityModerationService : ICommunityModerationService
{
    private readonly CceDbContext _db;
    private readonly ISearchClient _search;

    public CommunityModerationService(CceDbContext db, ISearchClient search)
    {
        _db = db;
        _search = search;
    }

    public Task<Post?> FindPostAsync(System.Guid id, CancellationToken ct)
        => _db.Posts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<PostReply?> FindReplyAsync(System.Guid id, CancellationToken ct)
        => _db.PostReplies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task ReIndexPostAsync(System.Guid postId, CancellationToken ct)
    {
        var post = await _db.Posts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == postId, ct).ConfigureAwait(false);
        if (post is null) return;

        var author = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == post.AuthorId, ct).ConfigureAwait(false);
        var authorName = PostCreatedSearchIndexHandler.BuildAuthorName(author?.FirstName, author?.LastName, author?.UserName);

        var doc = new CommunityPostDocument
        {
            Id        = post.Id.ToString(),
            TitleAr   = post.Locale == "ar" ? post.Title : null,
            TitleEn   = post.Locale == "en" ? post.Title : null,
            ContentAr = post.Locale == "ar" ? post.Content : null,
            ContentEn = post.Locale == "en" ? post.Content : null,
            AuthorName = authorName,
        };
        await _search.UpsertAsync(SearchableType.CommunityPosts, doc, ct).ConfigureAwait(false);
    }

    public async Task ReIndexReplyAsync(System.Guid replyId, CancellationToken ct)
    {
        var reply = await _db.PostReplies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == replyId, ct).ConfigureAwait(false);
        if (reply is null) return;

        var author = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == reply.AuthorId, ct).ConfigureAwait(false);
        var authorName = PostCreatedSearchIndexHandler.BuildAuthorName(author?.FirstName, author?.LastName, author?.UserName);

        var doc = new CommunityReplyDocument
        {
            Id        = reply.Id.ToString(),
            PostId    = reply.PostId.ToString(),
            ContentAr = reply.Locale == "ar" ? reply.Content : null,
            ContentEn = reply.Locale == "en" ? reply.Content : null,
            AuthorName = authorName,
        };
        await _search.UpsertAsync(SearchableType.CommunityReplies, doc, ct).ConfigureAwait(false);
    }
}
