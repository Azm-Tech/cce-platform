using CCE.Application.Community;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Community.Events;
using CCE.Domain.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CCE.Application.Notifications.Handlers;

public sealed class PostCreatedNotificationHandler
    : INotificationHandler<PostCreatedEvent>
{
    private readonly ICommunityReadService _communityRead;
    private readonly INotificationMessageDispatcher _dispatcher;
    private readonly ILogger<PostCreatedNotificationHandler> _logger;

    public PostCreatedNotificationHandler(
        ICommunityReadService communityRead,
        INotificationMessageDispatcher dispatcher,
        ILogger<PostCreatedNotificationHandler> logger)
    {
        _communityRead = communityRead;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(PostCreatedEvent notification, CancellationToken cancellationToken)
    {
        var topicFollowers = await _communityRead.GetTopicFollowerIdsAsync(
            notification.TopicId, notification.AuthorId, cancellationToken).ConfigureAwait(false);
        var communityFollowers = await _communityRead.GetCommunityFollowerIdsAsync(
            notification.CommunityId, notification.AuthorId, cancellationToken).ConfigureAwait(false);

        // Union the two audiences so a user following both the topic and the community is notified once.
        var recipients = new HashSet<System.Guid>(topicFollowers);
        foreach (var id in communityFollowers) recipients.Add(id);

        if (recipients.Count == 0)
        {
            _logger.LogInformation(
                "No followers to notify for post {PostId} (topic {TopicId}, community {CommunityId})",
                notification.PostId, notification.TopicId, notification.CommunityId);
            return;
        }

        foreach (var userId in recipients)
        {
            await _dispatcher.DispatchAsync(new NotificationMessage(
                TemplateCode: "COMMUNITY_POST_CREATED",
                RecipientUserId: userId,
                EventType: NotificationEventType.CommunityPostCreated,
                Channels: [NotificationChannel.InApp],
                MetaData: new Dictionary<string, string>(),
                Locale: notification.Locale), cancellationToken).ConfigureAwait(false);
        }
    }
}
