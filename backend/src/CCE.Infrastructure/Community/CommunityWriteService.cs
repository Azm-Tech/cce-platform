using CCE.Application.Community;
using CCE.Domain.Community;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Community;

public sealed class CommunityWriteService : ICommunityWriteService
{
    private readonly CceDbContext _db;

    public CommunityWriteService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SavePostAsync(Post post, CancellationToken ct)
    {
        _db.Posts.Add(post);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveReplyAsync(PostReply reply, CancellationToken ct)
    {
        _db.PostReplies.Add(reply);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SaveRatingAsync(PostRating rating, CancellationToken ct)
    {
        // Upsert: remove existing rating for same (PostId, UserId), then add the new one.
        var existing = await _db.PostRatings
            .FirstOrDefaultAsync(r => r.PostId == rating.PostId && r.UserId == rating.UserId, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            _db.PostRatings.Remove(existing);
        }
        _db.PostRatings.Add(rating);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<Post?> FindPostAsync(Guid id, CancellationToken ct)
        => await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);

    public async Task<PostReply?> FindReplyAsync(Guid id, CancellationToken ct)
        => await _db.PostReplies.FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);

    public async Task UpdatePostAsync(Post post, CancellationToken ct)
        => await _db.SaveChangesAsync(ct).ConfigureAwait(false);

    public async Task UpdateReplyAsync(PostReply reply, CancellationToken ct)
        => await _db.SaveChangesAsync(ct).ConfigureAwait(false);

    public async Task SaveFollowAsync<T>(T follow, CancellationToken ct) where T : class
    {
        _db.Set<T>().Add(follow);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<TopicFollow?> FindTopicFollowAsync(Guid topicId, Guid userId, CancellationToken ct)
        => await _db.TopicFollows
            .FirstOrDefaultAsync(f => f.TopicId == topicId && f.UserId == userId, ct)
            .ConfigureAwait(false);

    public async Task<UserFollow?> FindUserFollowAsync(Guid followerId, Guid followedId, CancellationToken ct)
        => await _db.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId, ct)
            .ConfigureAwait(false);

    public async Task<PostFollow?> FindPostFollowAsync(Guid postId, Guid userId, CancellationToken ct)
        => await _db.PostFollows
            .FirstOrDefaultAsync(f => f.PostId == postId && f.UserId == userId, ct)
            .ConfigureAwait(false);

    public async Task<bool> RemoveTopicFollowAsync(Guid topicId, Guid userId, CancellationToken ct)
    {
        var row = await _db.TopicFollows
            .FirstOrDefaultAsync(f => f.TopicId == topicId && f.UserId == userId, ct)
            .ConfigureAwait(false);
        if (row is null) return false;
        _db.TopicFollows.Remove(row);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RemoveUserFollowAsync(Guid followerId, Guid followedId, CancellationToken ct)
    {
        var row = await _db.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId, ct)
            .ConfigureAwait(false);
        if (row is null) return false;
        _db.UserFollows.Remove(row);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RemovePostFollowAsync(Guid postId, Guid userId, CancellationToken ct)
    {
        var row = await _db.PostFollows
            .FirstOrDefaultAsync(f => f.PostId == postId && f.UserId == userId, ct)
            .ConfigureAwait(false);
        if (row is null) return false;
        _db.PostFollows.Remove(row);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
