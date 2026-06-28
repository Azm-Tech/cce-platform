using System.Collections.Generic;
using System.Globalization;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Content;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Notifications;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

public sealed class ContentNotificationConsumer :
    IConsumer<NewsPublishedIntegrationEvent>,
    IConsumer<ResourcePublishedIntegrationEvent>,
    IConsumer<EventScheduledIntegrationEvent>
{
    private readonly INewsletterSubscriptionRepository _newsletterRepo;
    private readonly INewsRepository _newsRepo;
    private readonly IResourceRepository _resourceRepo;
    private readonly IEventRepository _eventRepo;
    private readonly INotificationMessageDispatcher _dispatcher;
    private readonly ILogger<ContentNotificationConsumer> _logger;
    private readonly int _fanOutConcurrency;
    private readonly string _frontendBaseUrl;

    public ContentNotificationConsumer(
        INewsletterSubscriptionRepository newsletterRepo,
        INewsRepository newsRepo,
        IResourceRepository resourceRepo,
        IEventRepository eventRepo,
        INotificationMessageDispatcher dispatcher,
        IOptions<CceInfrastructureOptions> options,
        ILogger<ContentNotificationConsumer> logger,
        IConfiguration configuration)
    {
        _newsletterRepo = newsletterRepo;
        _newsRepo = newsRepo;
        _resourceRepo = resourceRepo;
        _eventRepo = eventRepo;
        _dispatcher = dispatcher;
        _logger = logger;
        _fanOutConcurrency = options.Value.NewsletterFanOutConcurrency;
        _frontendBaseUrl = configuration.GetValue<string>("Frontend:BaseUrl") ?? "http://localhost:4201";
    }

    // ── News ────────────────────────────────────────────────────────────────────

    public async Task Consume(ConsumeContext<NewsPublishedIntegrationEvent> context)
    {
        var evt = context.Message;
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "ContentNotificationConsumer: News={Id} starting fan-out.", evt.NewsId);

        var newsData = await _newsRepo
            .GetNotificationDataAsync(evt.NewsId, ct)
            .ConfigureAwait(false);

        if (newsData is null)
        {
            _logger.LogWarning("News {NewsId} not found; skipping fan-out.", evt.NewsId);
            return;
        }

        var articleUrl = $"{_frontendBaseUrl}/news/{evt.NewsId:D}";
        var subscribers = await _newsletterRepo
            .GetAudienceAsync(evt.AuthorId, ct)
            .ConfigureAwait(false);

        await Parallel.ForEachAsync(
            subscribers,
            new ParallelOptions { MaxDegreeOfParallelism = _fanOutConcurrency, CancellationToken = ct },
            async (sub, token) =>
            {
                var recipientName = !string.IsNullOrEmpty(sub.RecipientName)
                    ? sub.RecipientName
                    : sub.Locale == "ar" ? "عزيزي المشترك" : "Dear Subscriber";

                var meta = new Dictionary<string, string>
                {
                    ["TitleAr"] = newsData.TitleAr,
                    ["TitleEn"] = newsData.TitleEn,
                    ["ContentBodyAr"] = newsData.ContentAr,
                    ["ContentBodyEn"] = newsData.ContentEn,
                    ["ArticleUrl"] = articleUrl,
                    ["RecipientName"] = recipientName,
                };

                NotificationChannel[] channels = sub.UserId.HasValue
                    ? [NotificationChannel.InApp, NotificationChannel.Email]
                    : [NotificationChannel.Email];

                await _dispatcher.DispatchAsync(new NotificationMessage(
                    TemplateCode: "NEWS_PUBLISHED",
                    RecipientUserId: sub.UserId,
                    EventType: NotificationEventType.NewsPublished,
                    Channels: channels,
                    MetaData: meta,
                    Locale: sub.Locale,
                    Email: sub.UserId.HasValue ? null : sub.Email), token).ConfigureAwait(false);
            }).ConfigureAwait(false);

        _logger.LogInformation(
            "ContentNotificationConsumer: News={Id} dispatched {Count} notifications.",
            evt.NewsId, subscribers.Count);
    }

    // ── Resource ────────────────────────────────────────────────────────────────

    public Task Consume(ConsumeContext<ResourcePublishedIntegrationEvent> context)
    {
        var evt = context.Message;
        return FanOutAsync("Resource", evt.ResourceId, _resourceRepo.GetTitleAsync,
            excludeUserId: evt.UploadedById,
            "RESOURCE_PUBLISHED", NotificationEventType.ResourcePublished,
            enrichMeta: null, context.CancellationToken);
    }

    // ── Event ───────────────────────────────────────────────────────────────────

    public Task Consume(ConsumeContext<EventScheduledIntegrationEvent> context)
    {
        var evt = context.Message;
        return FanOutAsync("Event", evt.EventId, _eventRepo.GetTitleAsync,
            excludeUserId: null,
            "EVENT_SCHEDULED", NotificationEventType.EventScheduled,
            enrichMeta: meta => meta["StartsOn"] =
                evt.StartsOn.ToString("yyyy-MM-dd HH:mm UTC", CultureInfo.InvariantCulture),
            context.CancellationToken);
    }

    // ── Generic fan-out (Resource, Event) ───────────────────────────────────────

    private async Task FanOutAsync(
        string entityKind,
        Guid entityId,
        Func<Guid, CancellationToken, Task<ContentTitle?>> getTitleAsync,
        Guid? excludeUserId,
        string templateCode,
        NotificationEventType eventType,
        Action<Dictionary<string, string>>? enrichMeta,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "ContentNotificationConsumer: {Kind}={Id} starting fan-out.", entityKind, entityId);

        var title = await getTitleAsync(entityId, ct).ConfigureAwait(false);
        var meta = BuildTitleMeta(title);
        enrichMeta?.Invoke(meta);

        var subscribers = await _newsletterRepo
            .GetAudienceAsync(excludeUserId, ct)
            .ConfigureAwait(false);

        await Parallel.ForEachAsync(
            subscribers,
            new ParallelOptions { MaxDegreeOfParallelism = _fanOutConcurrency, CancellationToken = ct },
            async (sub, token) =>
            {
                var recipientName = !string.IsNullOrEmpty(sub.RecipientName)
                    ? sub.RecipientName
                    : sub.Locale == "ar" ? "عزيزي المشترك" : "Dear Subscriber";

                var subscriberMeta = new Dictionary<string, string>(meta)
                {
                    ["RecipientName"] = recipientName,
                };

                NotificationChannel[] channels = sub.UserId.HasValue
                    ? [NotificationChannel.InApp, NotificationChannel.Email]
                    : [NotificationChannel.Email];

                await _dispatcher.DispatchAsync(new NotificationMessage(
                    TemplateCode: templateCode,
                    RecipientUserId: sub.UserId,
                    EventType: eventType,
                    Channels: channels,
                    MetaData: subscriberMeta,
                    Locale: sub.Locale,
                    Email: sub.UserId.HasValue ? null : sub.Email), token).ConfigureAwait(false);
            }).ConfigureAwait(false);

        _logger.LogInformation(
            "ContentNotificationConsumer: {Kind}={Id} dispatched {Count} notifications.",
            entityKind, entityId, subscribers.Count);
    }

    private static Dictionary<string, string> BuildTitleMeta(ContentTitle? title)
        => new()
        {
            ["TitleAr"] = title?.TitleAr ?? string.Empty,
            ["TitleEn"] = title?.TitleEn ?? string.Empty,
        };
}
