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
///
/// <para>
/// Idempotent under at-least-once delivery: content is moderated only while its
/// <c>ModerationStatus</c> is <see cref="ModerationStatus.Pending"/>. A redelivered message
/// finds a non-Pending status and is skipped — no second AI call, audit row, or counter change.
/// </para>
/// </summary>
public sealed class ContentModerationConsumer : IConsumer<ContentModerationRequestedIntegrationEvent>
{
    // Sentinel actor for automated actions; domain rejects Guid.Empty so we use a fixed non-zero value.
    private static readonly System.Guid SystemActorId =
        System.Guid.Parse("00000000-0000-0000-0000-000000000001");

    // Only confidently-safe content auto-approves; safe-but-uncertain (< 0.75) is sent to human
    // review instead of slipping through. Raised from 0.6 to reduce false-negatives.
    private const float SafeConfidenceThreshold   = 0.75f;
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

        await using var scope = _scopeFactory.CreateAsyncScope();
        var ctx = new ModerationContext(
            Db:                scope.ServiceProvider.GetRequiredService<ICceDbContext>(),
            ModerationService: scope.ServiceProvider.GetRequiredService<ICommunityModerationService>(),
            PostRepo:          scope.ServiceProvider.GetRequiredService<IPostRepository>(),
            SearchClient:      scope.ServiceProvider.GetRequiredService<ISearchClient>(),
            FeedStore:         scope.ServiceProvider.GetRequiredService<IRedisFeedStore>(),
            Publisher:         scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>(),
            Clock:             scope.ServiceProvider.GetRequiredService<ISystemClock>());

        if (isPost)
            await ProcessPostAsync(evt, ctx, ct).ConfigureAwait(false);
        else
            await ProcessReplyAsync(evt, ctx, ct).ConfigureAwait(false);
    }

    // ── Post pipeline ──────────────────────────────────────────────────────

    private async Task ProcessPostAsync(
        ContentModerationRequestedIntegrationEvent evt, ModerationContext ctx, CancellationToken ct)
    {
        var post = await ctx.ModerationService.FindPostAsync(evt.ContentId, ct).ConfigureAwait(false);
        if (post is null)
        {
            _logger.LogWarning("ContentModerationConsumer: post {ContentId} no longer exists; skipping", evt.ContentId);
            return;
        }

        if (post.ModerationStatus != ModerationStatus.Pending)
        {
            _logger.LogInformation(
                "ContentModerationConsumer: post {ContentId} already moderated ({Status}); skipping (idempotent)",
                evt.ContentId, post.ModerationStatus);
            return;
        }

        var (score, phase, provider) = await AnalyseContentAsync(evt.ContentId, evt.Content, ct).ConfigureAwait(false);
        var status = ResolveStatus(score, phase);

        ctx.Db.Add(ModerationRecord.CreateAutomated(
            ModerationContentType.Post, evt.ContentId, status, phase, provider,
            score.Confidence, score.Category, score.Reason, ctx.Clock));

        post.SetModerationStatus(status);

        var autoRejected = status == ModerationStatus.Rejected && _opts.AutoRejectOnViolation;
        if (autoRejected && !post.IsDeleted)
        {
            post.SoftDelete(SystemActorId, ctx.Clock);

            var author = await ctx.Db.Users
                .FirstOrDefaultAsync(u => u.Id == post.AuthorId, ct).ConfigureAwait(false);
            author?.DecrementPostsCount();

            var community = await ctx.Db.Communities
                .FirstOrDefaultAsync(c => c.Id == post.CommunityId, ct).ConfigureAwait(false);
            community?.DecrementPosts();
        }

        // Stage outbox events BEFORE the save so they commit atomically with the DB changes.
        // Publishing after SaveChanges would stage an outbox row that is never committed (lost).
        await StageOutboxEventsAsync(evt, status, score, post.AuthorId, post.Locale, autoRejected, ctx, ct)
            .ConfigureAwait(false);

        await ctx.Db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (autoRejected)
        {
            await ctx.SearchClient.DeleteAsync(SearchableType.CommunityPosts, post.Id, ct).ConfigureAwait(false);
            await ctx.FeedStore.RemovePostFromAllFeedsAsync(post.CommunityId, post.Id, ct).ConfigureAwait(false);
        }

        LogOutcome(evt, status);
    }

    // ── Reply pipeline ─────────────────────────────────────────────────────

    private async Task ProcessReplyAsync(
        ContentModerationRequestedIntegrationEvent evt, ModerationContext ctx, CancellationToken ct)
    {
        var reply = await ctx.ModerationService.FindReplyAsync(evt.ContentId, ct).ConfigureAwait(false);
        if (reply is null)
        {
            _logger.LogWarning("ContentModerationConsumer: reply {ContentId} no longer exists; skipping", evt.ContentId);
            return;
        }

        if (reply.ModerationStatus != ModerationStatus.Pending)
        {
            _logger.LogInformation(
                "ContentModerationConsumer: reply {ContentId} already moderated ({Status}); skipping (idempotent)",
                evt.ContentId, reply.ModerationStatus);
            return;
        }

        var (score, phase, provider) = await AnalyseContentAsync(evt.ContentId, evt.Content, ct).ConfigureAwait(false);
        var status = ResolveStatus(score, phase);

        ctx.Db.Add(ModerationRecord.CreateAutomated(
            ModerationContentType.Reply, evt.ContentId, status, phase, provider,
            score.Confidence, score.Category, score.Reason, ctx.Clock));

        reply.SetModerationStatus(status);

        var autoRejected = status == ModerationStatus.Rejected && _opts.AutoRejectOnViolation;
        if (autoRejected && !reply.IsDeleted)
        {
            reply.SoftDelete(SystemActorId, ctx.Clock);

            var parentPost = await ctx.PostRepo.GetIncludingDeletedAsync(reply.PostId, ct).ConfigureAwait(false);
            parentPost?.DecrementCommentsCount(ctx.Clock);

            var author = await ctx.Db.Users
                .FirstOrDefaultAsync(u => u.Id == reply.AuthorId, ct).ConfigureAwait(false);
            author?.DecrementCommentsCount();
        }

        await StageOutboxEventsAsync(evt, status, score, reply.AuthorId, reply.Locale, autoRejected, ctx, ct)
            .ConfigureAwait(false);

        await ctx.Db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (autoRejected)
            await ctx.SearchClient.DeleteAsync(SearchableType.CommunityReplies, reply.Id, ct).ConfigureAwait(false);

        LogOutcome(evt, status);
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

    // ── Stage moderation outbox events ─────────────────────────────────────
    // Called BEFORE SaveChanges so each Publish writes an outbox_message row that commits in the
    // same transaction as the moderation result (publishing after the save would be lost).

    private static async Task StageOutboxEventsAsync(
        ContentModerationRequestedIntegrationEvent evt,
        ModerationStatus status,
        ModerationScore score,
        System.Guid authorId,
        string locale,
        bool autoRejected,
        ModerationContext ctx,
        CancellationToken ct)
    {
        // Moderator alert — flagged or rejected.
        if (status is ModerationStatus.Flagged or ModerationStatus.Rejected)
            await ctx.Publisher.PublishAsync(new ContentFlaggedIntegrationEvent(
                evt.ContentId, evt.ContentType, status,
                score.Category, score.Reason, ctx.Clock.UtcNow), ct).ConfigureAwait(false);

        // Author takedown notice — only when content was actually removed.
        if (autoRejected)
            await ctx.Publisher.PublishAsync(new ContentRejectedIntegrationEvent(
                evt.ContentId, evt.ContentType, authorId, locale), ct).ConfigureAwait(false);
    }

    private void LogOutcome(ContentModerationRequestedIntegrationEvent evt, ModerationStatus status)
        => _logger.LogInformation(
            "ContentModerationConsumer: {ContentType} {ContentId} → {Status}",
            evt.ContentType, evt.ContentId, status);

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
