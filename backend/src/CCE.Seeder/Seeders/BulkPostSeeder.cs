using CCE.Domain.Community;
using CCE.Domain.Common;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

/// <summary>
/// Seeds 10,000 posts for performance and scale testing.
/// Activated by --bulk CLI flag; idempotent on re-run (checks sentinel post).
/// Author mix: 20% expert, 80% regular — exercises both fan-in and fan-out read paths.
/// </summary>
public sealed class BulkPostSeeder : ISeeder
{
    private const int PostCount = 10_000;
    private const int BatchSize = 200;

    private readonly CceDbContext _ctx;
    private readonly ISystemClock _clock;
    private readonly ILogger<BulkPostSeeder> _logger;

    public BulkPostSeeder(CceDbContext ctx, ISystemClock clock, ILogger<BulkPostSeeder> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public int Order => 105;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var sentinelId = DeterministicGuid.From("post:bulk:0");
        if (await _ctx.Posts.IgnoreQueryFilters()
                .AnyAsync(p => p.Id == sentinelId, cancellationToken)
                .ConfigureAwait(false))
        {
            _logger.LogInformation("BulkPostSeeder: already seeded ({Count} posts) -- skipping.", PostCount);
            return;
        }

        var communityIds = await _ctx.Communities
            .Where(c => c.IsActive && c.Visibility == CommunityVisibility.Public)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var topicIds = await _ctx.Topics
            .Select(t => t.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (communityIds.Count == 0 || topicIds.Count == 0)
        {
            _logger.LogWarning("BulkPostSeeder: no communities or topics found -- run reference seeders first.");
            return;
        }

        var allUserIds = await _ctx.Users
            .OrderBy(u => u.Id)
            .Select(u => u.Id)
            .Take(20)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var expertIds = await _ctx.ExpertProfiles
            .Select(e => e.UserId)
            .Take(5)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (allUserIds.Count == 0)
        {
            _logger.LogWarning("BulkPostSeeder: no users found -- run DemoUsersSeeder first.");
            return;
        }

        var regularIds = allUserIds
            .Where(id => !expertIds.Contains(id))
            .ToList();

        if (regularIds.Count == 0)
            regularIds = allUserIds;

        _logger.LogInformation(
            "BulkPostSeeder: seeding {Count} posts ({Communities} communities, {Topics} topics, {Experts} experts, {Regulars} regular users).",
            PostCount, communityIds.Count, topicIds.Count, expertIds.Count, regularIds.Count);

        var types = new[] { PostType.Info, PostType.Question };
        var saved = 0;
        var communityPostCounts = new Dictionary<Guid, int>();

        for (var i = 0; i < PostCount; i++)
        {
            var postId = DeterministicGuid.From($"post:bulk:{i}");
            var communityId = communityIds[i % communityIds.Count];
            var topicId = topicIds[i % topicIds.Count];
            var postType = types[i % types.Length];

            // 20% expert authors so the fan-in merge path has representative data.
            var isExpert = i % 5 == 0 && expertIds.Count > 0;
            var authorId = isExpert
                ? expertIds[i % expertIds.Count]
                : regularIds[i % regularIds.Count];

            var title = $"Bulk post {i}: scale-test item topic-slot {i % topicIds.Count}";
            var content = $"Auto-generated post #{i} for load testing. Community {i % communityIds.Count}, " +
                          $"topic {i % topicIds.Count}, author-type {(isExpert ? "expert" : "regular")}.";

            var post = Post.CreateDraft(communityId, topicId, authorId, postType,
                title, content, "en", _clock);
            post.Publish(_clock);
            typeof(Post).GetProperty(nameof(post.Id))!.SetValue(post, postId);
            _ctx.Posts.Add(post);
            saved++;
            communityPostCounts[communityId] = communityPostCounts.GetValueOrDefault(communityId) + 1;

            if (saved % BatchSize == 0)
            {
                await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                if (saved % 1000 == 0)
                    _logger.LogInformation("BulkPostSeeder: {Saved}/{Total} posts saved.", saved, PostCount);
            }
        }

        if (saved % BatchSize != 0)
            await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Bulk-update PostCount for all affected communities in one round-trip per community.
        foreach (var (cid, count) in communityPostCounts)
        {
            await _ctx.Communities
                .Where(c => c.Id == cid)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.PostCount, c => c.PostCount + count),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation("BulkPostSeeder: complete -- {Saved} posts seeded.", saved);
    }
}
