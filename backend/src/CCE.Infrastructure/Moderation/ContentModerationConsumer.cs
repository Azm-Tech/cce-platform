using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Community;
using CCE.Application.Community.Moderation;
using CCE.Application.Search;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Moderation;

/// <summary>
/// Async AI moderation consumer. Processes <see cref="ContentModerationRequestedIntegrationEvent"/>
/// in two phases:
/// <list type="number">
///   <item>Rule-based pre-filter (no API call) — catches obvious violations instantly.</item>
///   <item>AI provider call (Ollama / Groq / OpenRouter) — for content that passes the rule filter.</item>
/// </list>
/// On Rejected: soft-deletes content, removes from Meilisearch and Redis community feed.
/// On Flagged: leaves content visible, notifies admins via <see cref="ContentFlaggedIntegrationEvent"/>.
/// </summary>
public sealed class ContentModerationConsumer : IConsumer<ContentModerationRequestedIntegrationEvent>
{
    // Sentinel actor for automated actions; domain rejects Guid.Empty so we use a fixed non-zero value.
    private static readonly System.Guid SystemActorId =
        System.Guid.Parse("00000000-0000-0000-0000-000000000001");

    private const float SafeConfidenceThreshold   = 0.6f;
    private const float RejectConfidenceThreshold = 0.7f;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAiModerationProvider _aiProvider;
    private readonly IRuleBasedPreFilter _preFilter;
    private readonly ModerationOptions _opts;
    private readonly ILogger<ContentModerationConsumer> _logger;

    public ContentModerationConsumer(
        IServiceScopeFactory scopeFactory,
        IAiModerationProvider aiProvider,
        IRuleBasedPreFilter preFilter,
        IOptions<ModerationOptions> opts,
        ILogger<ContentModerationConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _aiProvider   = aiProvider;
        _preFilter    = preFilter;
        _opts         = opts.Value;
        _logger       = logger;
    }

    // ── Scoped services bundle (one per Consume invocation) ────────────────
    private readonly record struct ModerationContext(
        ICceDbContext Db,
        ICommunityModerationService ModerationService,
        IPostRepository PostRepo,
        ISearchClient SearchClient,
        IRedisFeedStore FeedStore,
        IIntegrationEventPublisher Publisher,
        ISystemClock Clock);

    // ───────────────────────────────────────────────────────────────────────

    public async Task Consume(ConsumeContext<ContentModerationRequestedIntegrationEvent> context)
    {
        var evt    = context.Message;
        var ct     = context.CancellationToken;
        var isPost = string.Equals(
            evt.ContentType,
            ContentModerationRequestedIntegrationEvent.ContentTypes.Post,
            System.StringComparison.Ordinal);

        _logger.LogInformation(
            "ContentModerationConsumer: analysing {ContentType} {ContentId}",
            evt.ContentType, evt.ContentId);

        var (score, phase, provider) = await AnalyseContentAsync(evt.ContentId, evt.Content, ct)
            .ConfigureAwait(false);
        var status = ResolveStatus(score, phase);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var ctx = new ModerationContext(
            Db:                scope.ServiceProvider.GetRequiredService<ICceDbContext>(),
            ModerationService: scope.ServiceProvider.GetRequiredService<ICommunityModerationService>(),
            PostRepo:          scope.ServiceProvider.GetRequiredService<IPostRepository>(),
            SearchClient:      scope.ServiceProvider.GetRequiredService<ISearchClient>(),
            FeedStore:         scope.ServiceProvider.GetRequiredService<IRedisFeedStore>(),
            Publisher:         scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>(),
            Clock:             scope.ServiceProvider.GetRequiredService<ISystemClock>());

        var record = ModerationRecord.CreateAutomated(
            isPost ? ModerationContentType.Post : ModerationContentType.Reply,
            evt.ContentId, status, phase, provider,
            score.Confidence, score.Category, score.Reason);
        ctx.Db.Add(record);

        // Process content mutations + counter decrements; capture communityId for feed removal.
        var rejectedCommunityId = isPost
            ? await HandlePostAsync(evt.ContentId, status, ctx, ct).ConfigureAwait(false)
            : await HandleReplyAsync(evt.ContentId, status, ctx, ct).ConfigureAwait(false);

        // ── Single transactional save ──────────────────────────────────────
        await ctx.Db.SaveChangesAsync(ct).ConfigureAwait(false);

        // ── Side effects after commit (search, feed, notifications) ────────
        await RunSideEffectsAsync(isPost, status, evt, rejectedCommunityId, score, ctx, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "ContentModerationConsumer: {ContentType} {ContentId} → {Status}",
            evt.ContentType, evt.ContentId, status);
    }

    // ── Phase 1 + 2 ────────────────────────────────────────────────────────

    private async Task<(ModerationScore Score, string Phase, string Provider)> AnalyseContentAsync(
        System.Guid contentId, string content, CancellationToken ct)
    {
        if (_preFilter.ShouldFlag(content, out var ruleReason))
        {
            _logger.LogInformation(
                "ContentModerationConsumer: {ContentId} flagged by rule filter ({Reason})",
                contentId, ruleReason);
            return (
                new ModerationScore(false, 1f, ModerationPhase.Rule, ruleReason),
                ModerationPhase.Rule,
                ModerationPhase.Rule);
        }

        var score = await _aiProvider.ModerateAsync(content, ct).ConfigureAwait(false);
        _logger.LogInformation(
            "ContentModerationConsumer: {ContentId} AI result safe={Safe} confidence={Confidence:F2} category={Category}",
            contentId, score.IsSafe, score.Confidence, score.Category);
        return (score, ModerationPhase.Ai, _aiProvider.ProviderName);
    }

    // ── Post mutation + counter decrements ─────────────────────────────────
    // Returns the post's CommunityId when auto-rejected (needed for feed removal), null otherwise.

    private async Task<System.Guid?> HandlePostAsync(
        System.Guid contentId, ModerationStatus status, ModerationContext ctx, CancellationToken ct)
    {
        var post = await ctx.ModerationService.FindPostAsync(contentId, ct).ConfigureAwait(false);
        if (post is null) return null;

        post.SetModerationStatus(status);

        if (status != ModerationStatus.Rejected || !_opts.AutoRejectOnViolation)
            return null;

        post.SoftDelete(SystemActorId, ctx.Clock);

        var author = await ctx.Db.Users
            .FirstOrDefaultAsync(u => u.Id == post.AuthorId, ct).ConfigureAwait(false);
        author?.DecrementPostsCount();

        var community = await ctx.Db.Communities
            .FirstOrDefaultAsync(c => c.Id == post.CommunityId, ct).ConfigureAwait(false);
        community?.DecrementPosts();

        return post.CommunityId;
    }

    // ── Reply mutation + counter decrements ────────────────────────────────

    private async Task<System.Guid?> HandleReplyAsync(
        System.Guid contentId, ModerationStatus status, ModerationContext ctx, CancellationToken ct)
    {
        var reply = await ctx.ModerationService.FindReplyAsync(contentId, ct).ConfigureAwait(false);
        if (reply is null) return null;

        reply.SetModerationStatus(status);

        if (status != ModerationStatus.Rejected || !_opts.AutoRejectOnViolation)
            return null;

        reply.SoftDelete(SystemActorId, ctx.Clock);

        var parentPost = await ctx.PostRepo
            .GetIncludingDeletedAsync(reply.PostId, ct).ConfigureAwait(false);
        parentPost?.DecrementCommentsCount(ctx.Clock);

        var author = await ctx.Db.Users
            .FirstOrDefaultAsync(u => u.Id == reply.AuthorId, ct).ConfigureAwait(false);
        author?.DecrementCommentsCount();

        return null;
    }

    // ── Side effects (execute after DB commit) ─────────────────────────────

    private async Task RunSideEffectsAsync(
        bool isPost,
        ModerationStatus status,
        ContentModerationRequestedIntegrationEvent evt,
        System.Guid? rejectedCommunityId,
        ModerationScore score,
        ModerationContext ctx,
        CancellationToken ct)
    {
        if (status == ModerationStatus.Rejected && _opts.AutoRejectOnViolation)
        {
            if (isPost && rejectedCommunityId.HasValue)
            {
                await ctx.SearchClient
                    .DeleteAsync(SearchableType.CommunityPosts, evt.ContentId, ct)
                    .ConfigureAwait(false);
                await ctx.FeedStore
                    .RemovePostFromAllFeedsAsync(rejectedCommunityId.Value, evt.ContentId, ct)
                    .ConfigureAwait(false);
            }
            else if (!isPost)
            {
                await ctx.SearchClient
                    .DeleteAsync(SearchableType.CommunityReplies, evt.ContentId, ct)
                    .ConfigureAwait(false);
            }
        }

        if (status is ModerationStatus.Flagged or ModerationStatus.Rejected)
        {
            await ctx.Publisher.PublishAsync(new ContentFlaggedIntegrationEvent(
                evt.ContentId, evt.ContentType, status,
                score.Category, score.Reason, ctx.Clock.UtcNow), ct)
                .ConfigureAwait(false);
        }
    }

    // ── Decision table ─────────────────────────────────────────────────────

    private static ModerationStatus ResolveStatus(ModerationScore score, string phase)
    {
        if (phase == ModerationPhase.Rule)
            return ModerationStatus.Flagged;

        if (score.Category is ModerationCategory.ParseError or ModerationCategory.RateLimited)
            return ModerationStatus.Flagged;

        if (score.IsSafe && score.Confidence >= SafeConfidenceThreshold)
            return ModerationStatus.Approved;

        if (!score.IsSafe)
        {
            if (score.Category is ModerationCategory.Spam or ModerationCategory.Hate
                && score.Confidence >= RejectConfidenceThreshold)
                return ModerationStatus.Rejected;

            // explicit/harassment or low-confidence negative → human review
            return ModerationStatus.Flagged;
        }

        // IsSafe=true but confidence < SafeConfidenceThreshold — uncertain
        return ModerationStatus.Flagged;
    }
}
