using CCE.Application.Community;
using CCE.Domain.Community;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Community;

/// <summary>EF implementation of <see cref="IPostRepository"/>. Returns tracked entities for the UoW.</summary>
public sealed class PostRepository : IPostRepository
{
    private readonly CceDbContext _db;

    public PostRepository(CceDbContext db) => _db = db;

    public Task<Post?> GetAsync(Guid id, CancellationToken ct)
        => _db.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Guid?> GetCommunityIdAsync(Guid id, CancellationToken ct)
        => _db.Posts.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => (Guid?)p.CommunityId)
            .FirstOrDefaultAsync(ct);

    public Task<bool> TopicExistsAsync(Guid topicId, CancellationToken ct)
        => _db.Topics.AnyAsync(t => t.Id == topicId && t.IsActive, ct);

    public async Task<IReadOnlyList<Tag>> GetTagsAsync(IReadOnlyList<Guid> tagIds, CancellationToken ct)
    {
        if (tagIds.Count == 0) return System.Array.Empty<Tag>();
        return await _db.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AssetFile>> GetAssetsAsync(IReadOnlyList<Guid> assetIds, CancellationToken ct)
    {
        if (assetIds.Count == 0) return System.Array.Empty<AssetFile>();
        return await _db.AssetFiles.Where(a => assetIds.Contains(a.Id)).ToListAsync(ct).ConfigureAwait(false);
    }

    public void Add(Post post) => _db.Posts.Add(post);

    public void Remove(Post post) => _db.Posts.Remove(post);
}
