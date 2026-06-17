using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Content;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

public sealed class DemoTopicDataSeeder : ISeeder
{
    private const string TargetEmail = "ahmed.elbatal@azm.com";

    private readonly CceDbContext _ctx;
    private readonly ISystemClock _clock;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DemoTopicDataSeeder> _logger;

    public DemoTopicDataSeeder(
        CceDbContext ctx,
        ISystemClock clock,
        UserManager<User> userManager,
        ILogger<DemoTopicDataSeeder> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _userManager = userManager;
        _logger = logger;
    }

    public int Order => 101;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(TargetEmail).ConfigureAwait(false);
        if (user is null)
        {
            _logger.LogWarning("Demo user {Email} not found — skipping DemoTopicDataSeeder.", TargetEmail);
            return;
        }

        var userId = user.Id;

        // Map topic slugs to IDs.
        var topicMap = await _ctx.Topics
            .ToDictionaryAsync(t => t.Slug, t => t.Id, cancellationToken)
            .ConfigureAwait(false);

        var topicSlugs = new[] { "general", "solar-power", "policy", "research" };

        // Create TopicFollows for all four topics.
        foreach (var slug in topicSlugs)
        {
            if (!topicMap.TryGetValue(slug, out var topicId))
            {
                _logger.LogWarning("Topic '{Slug}' not found — skipping follow.", slug);
                continue;
            }

            var exists = await _ctx.TopicFollows.IgnoreQueryFilters()
                .AnyAsync(f => f.TopicId == topicId && f.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (exists) continue;

            var follow = TopicFollow.Follow(topicId, userId, _clock);
            _ctx.TopicFollows.Add(follow);
        }

        // Ensure the general community exists.
        var generalId = CommunitySeedIds.GeneralCommunityId;
        var communityExists = await _ctx.Communities.IgnoreQueryFilters()
            .AnyAsync(c => c.Id == generalId, cancellationToken)
            .ConfigureAwait(false);
        if (!communityExists)
        {
            var general = CCE.Domain.Community.Community.Create(
                "عام", "General", "المجتمع العام", "The general community",
                "general", CommunityVisibility.Public);
            typeof(CCE.Domain.Community.Community).GetProperty(nameof(general.Id))!.SetValue(general, generalId);
            _ctx.Communities.Add(general);
            await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Create published posts by Ahmed in every topic.
        var posts = new[]
        {
            (Slug: "my-carbon-footprint", TopicSlug: "general", Locale: "en", Type: PostType.Info,
             Content: "I calculated my personal carbon footprint this weekend using the new CCE calculator tool. " +
                      "It was eye-opening — my household produces about 8.5 tonnes CO₂e per year. " +
                      "I'm now looking into offsets and reduction strategies. Anyone else tried it?"),
            (Slug: "solar-panel-cleaning", TopicSlug: "solar-power", Locale: "en", Type: PostType.Info,
             Content: "Just had my quarterly solar panel cleaning. The efficiency jumped from 78% to 94% overnight. " +
                      "In this region, dust accumulation really hits performance hard. " +
                      "Highly recommend automated cleaning drones."),
            (Slug: "carbon-credit-question", TopicSlug: "policy", Locale: "en", Type: PostType.Question,
             Content: "Can someone explain how voluntary carbon credits differ from compliance credits in the " +
                      "current regulatory landscape? I'm especially curious about the MENA region approach."),
            (Slug: "ccs-breakthrough", TopicSlug: "research", Locale: "en", Type: PostType.Info,
             Content: "Interesting paper published last week on a new solvent-based carbon capture method that " +
                      "reduces energy penalty by 40% compared to amine scrubbing. The lab results look promising " +
                      "— hoping to see a pilot plant within 18 months."),
            (Slug: "battery-storage-tips", TopicSlug: "solar-power", Locale: "en", Type: PostType.Question,
             Content: "I'm designing a solar-plus-storage system for a small commercial building. Any recommendations " +
                      "on lithium iron phosphate vs. flow batteries for a 200kWh daily draw in hot climate?"),
        };

        foreach (var p in posts)
        {
            if (!topicMap.TryGetValue(p.TopicSlug, out var topicId))
            {
                _logger.LogWarning("Topic '{Slug}' not found — skipping post '{PostSlug}'.", p.TopicSlug, p.Slug);
                continue;
            }

            var postId = DeterministicGuid.From($"post:demo:{p.Slug}");
            var postExists = await _ctx.Posts.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == postId, cancellationToken)
                .ConfigureAwait(false);

            if (postExists) continue;

            var title = p.Content.Length > Post.MaxTitleLength
                ? p.Content[..Post.MaxTitleLength]
                : p.Content;

            var post = Post.CreateDraft(generalId, topicId, userId, p.Type, title, p.Content, p.Locale, _clock);
            post.Publish(_clock);
            typeof(Post).GetProperty(nameof(post.Id))!.SetValue(post, postId);
            _ctx.Posts.Add(post);
        }

        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Seeded topic follows and demo posts for {Email}.", TargetEmail);
    }
}
