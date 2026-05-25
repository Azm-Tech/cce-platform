using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Content.Events;
using CCE.Domain.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CCE.Application.Notifications.Handlers;

public sealed class NewsPublishedNotificationHandler
    : INotificationHandler<NewsPublishedEvent>
{
    private readonly INewsRepository _newsRepo;
    private readonly INotificationMessageDispatcher _dispatcher;
    private readonly ILogger<NewsPublishedNotificationHandler> _logger;

    public NewsPublishedNotificationHandler(
        INewsRepository newsRepo,
        INotificationMessageDispatcher dispatcher,
        ILogger<NewsPublishedNotificationHandler> logger)
    {
        _newsRepo = newsRepo;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(NewsPublishedEvent notification, CancellationToken cancellationToken)
    {
        var news = await _newsRepo.FindAsync(notification.NewsId, cancellationToken)
            .ConfigureAwait(false);

        if (news is null)
        {
            _logger.LogWarning(
                "News {NewsId} not found for notification.", notification.NewsId);
            return;
        }

        await _dispatcher.DispatchAsync(new NotificationMessage(
            TemplateCode: "NEWS_PUBLISHED",
            RecipientUserId: news.AuthorId,
            EventType: NotificationEventType.NewsPublished,
            Channels: [NotificationChannel.InApp],
            MetaData: new Dictionary<string, string>(),
            Locale: "en"), cancellationToken).ConfigureAwait(false);
    }
}
