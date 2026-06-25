using System.Collections.Generic;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Services;
using CCE.Application.Messages;
using CCE.Application.Identity;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Community.Commands.PublishPost;

public sealed class PublishPostCommandHandler
    : IRequestHandler<PublishPostCommand, Response<VoidData>>
{
    private readonly IPostRepository _repo;
    private readonly ICommunityRepository _communityRepo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;
    private readonly IUserRepository _userRepo;
    private readonly IMentionService _mentions;
    private readonly INotificationMessageDispatcher _dispatcher;

    public PublishPostCommandHandler(
        IPostRepository repo, ICommunityRepository communityRepo, ICceDbContext db,
        ICurrentUserAccessor currentUser, ISystemClock clock, MessageFactory msg,
        IUserRepository userRepo, IMentionService mentions, INotificationMessageDispatcher dispatcher)
    {
        _repo = repo;
        _communityRepo = communityRepo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
        _userRepo = userRepo;
        _mentions = mentions;
        _dispatcher = dispatcher;
    }

    public async Task<Response<VoidData>> Handle(PublishPostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty)
            return _msg.Unauthorized<VoidData>(MessageKeys.Identity.NOT_AUTHENTICATED);

        var post = await _repo.GetAsync(request.PostId, cancellationToken).ConfigureAwait(false);
        if (post is null) return _msg.NotFound<VoidData>(MessageKeys.Community.POST_NOT_FOUND);
        if (post.AuthorId != userId.Value) return _msg.Forbidden<VoidData>(MessageKeys.General.FORBIDDEN);

        post.Publish(_clock);

        var author = await _userRepo.FindAsync(post.AuthorId, cancellationToken).ConfigureAwait(false);
        author?.IncrementPostsCount();

        var community = await _communityRepo.GetAsync(post.CommunityId, cancellationToken).ConfigureAwait(false);
        community?.IncrementPosts();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Extract and persist mentions from post body after the commit (locale defaults to "en" for drafts).
        var postContent = post.Content ?? post.Title ?? string.Empty;
        var snippet = postContent.Length > 120 ? postContent[..120] : postContent;
        var mentioned = await _mentions.ExtractAndPersistAsync(
            postContent, MentionSourceType.Post, post.Id, post.Id, post.CommunityId,
            snippet, userId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (mentioned.Count > 0)
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var recipientId in mentioned)
        {
            await _dispatcher.DispatchAsync(new NotificationMessage(
                TemplateCode: "COMMUNITY_MENTION",
                RecipientUserId: recipientId,
                EventType: NotificationEventType.CommunityUserMentioned,
                Channels: [NotificationChannel.InApp, NotificationChannel.Push],
                MetaData: new Dictionary<string, string>
                {
                    ["postId"] = post.Id.ToString(),
                    ["sourceType"] = "post",
                },
                Locale: request.Locale), cancellationToken).ConfigureAwait(false);
        }

        return _msg.Ok(MessageKeys.Community.POST_PUBLISHED);
    }
}
