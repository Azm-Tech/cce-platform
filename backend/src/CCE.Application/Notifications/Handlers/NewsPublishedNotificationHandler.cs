using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Content.Events;
using CCE.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Application.Notifications.Handlers;

public sealed class NewsPublishedNotificationHandler
    : INotificationHandler<NewsPublishedEvent>
{
    private readonly INewsRepository _newsRepo;
    private readonly INotificationMessageDispatcher _dispatcher;
    private readonly ICceDbContext _db;
    private readonly ISystemClock _clock;
    private readonly ILogger<NewsPublishedNotificationHandler> _logger;

    public NewsPublishedNotificationHandler(
        INewsRepository newsRepo,
        INotificationMessageDispatcher dispatcher,
        ICceDbContext db,
        ISystemClock clock,
        ILogger<NewsPublishedNotificationHandler> logger)
    {
        _newsRepo = newsRepo;
        _dispatcher = dispatcher;
        _db = db;
        _clock = clock;
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

        var followerIds = await _db.NewsFollows
            .Where(f => f.Status == FollowStatus.Followed && f.UserId != news.AuthorId)
            .Select(f => f.UserId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var recipientIds = new HashSet<System.Guid>(followerIds) { news.AuthorId };

        foreach (var userId in followerIds)
            _db.Add(NewsFollowLog.Log(userId, notification.NewsId, _clock));

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var userId in recipientIds)
        {
            await _dispatcher.DispatchAsync(new NotificationMessage(
                TemplateCode: "NEWS_PUBLISHED",
                RecipientUserId: userId,
                EventType: NotificationEventType.NewsPublished,
                Channels: [NotificationChannel.InApp, NotificationChannel.Email],
                MetaData: new Dictionary<string, string>(),
                Locale: "en"), cancellationToken).ConfigureAwait(false);
        }
    }
}
