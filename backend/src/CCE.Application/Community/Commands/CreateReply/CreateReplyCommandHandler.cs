using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Community.Commands.CreateReply;

/// <summary>
/// US029 write path (§A.1): fetch the post (+ parent for nesting), build a root/child reply with a
/// materialized thread path, persist validated @mentions, and commit once via the context (UoW).
/// </summary>
public sealed class CreateReplyCommandHandler
    : IRequestHandler<CreateReplyCommand, Response<Guid>>
{
    private readonly IReplyRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IHtmlSanitizer _sanitizer;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;
    private readonly ICommunityRealtimePublisher _realtime;
    private readonly INotificationMessageDispatcher _dispatcher;

    public CreateReplyCommandHandler(
        IReplyRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser,
        IHtmlSanitizer sanitizer, ISystemClock clock, MessageFactory msg,
        ICommunityRealtimePublisher realtime, INotificationMessageDispatcher dispatcher)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
        _clock = clock;
        _msg = msg;
        _realtime = realtime;
        _dispatcher = dispatcher;
    }

    public async Task<Response<Guid>> Handle(CreateReplyCommand request, CancellationToken cancellationToken)
    {
        var authorId = _currentUser.GetUserId();
        if (authorId is null || authorId == Guid.Empty) return _msg.NotAuthenticated<Guid>();

        var post = await _repo.GetPostAsync(request.PostId, cancellationToken).ConfigureAwait(false);
        if (post is null) return _msg.NotFound<Guid>(ApplicationErrors.Community.POST_NOT_FOUND);

        var content = _sanitizer.Sanitize(request.Content);

        PostReply reply;
        if (request.ParentReplyId is { } parentId)
        {
            var parent = await _repo.GetParentAsync(parentId, cancellationToken).ConfigureAwait(false);
            if (parent is null || parent.PostId != post.Id)
                return _msg.NotFound<Guid>(ApplicationErrors.Community.REPLY_NOT_FOUND);
            reply = PostReply.CreateChild(parent, authorId.Value, content, request.Locale, isByExpert: false, _clock);
        }
        else
        {
            reply = PostReply.CreateRoot(post.Id, authorId.Value, content, request.Locale, isByExpert: false, _clock);
        }

        _repo.AddReply(reply);

        var mentioned = await PersistMentionsAsync(
            post.CommunityId, reply.Id, authorId.Value, request.MentionedUserIds, cancellationToken)
            .ConfigureAwait(false);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _realtime.PublishToPostAsync(post.Id, "NewReply",
            new { postId = post.Id, replyId = reply.Id, reply.ParentReplyId, reply.Depth }, cancellationToken)
            .ConfigureAwait(false);

        // Notify mentioned users (InApp) after the commit.
        foreach (var userId in mentioned)
        {
            await _dispatcher.DispatchAsync(new NotificationMessage(
                TemplateCode: "COMMUNITY_MENTION",
                RecipientUserId: userId,
                EventType: NotificationEventType.CommunityUserMentioned,
                Channels: [NotificationChannel.InApp],
                MetaData: new Dictionary<string, string> { ["postId"] = post.Id.ToString(), ["replyId"] = reply.Id.ToString() },
                Locale: request.Locale), cancellationToken).ConfigureAwait(false);
        }

        return _msg.Ok(reply.Id, ApplicationErrors.General.SUCCESS_CREATED);
    }

    private async Task<IReadOnlyList<Guid>> PersistMentionsAsync(
        Guid communityId, Guid replyId, Guid authorId,
        IReadOnlyList<Guid> mentionedUserIds, CancellationToken ct)
    {
        var candidates = mentionedUserIds
            .Where(id => id != Guid.Empty && id != authorId) // drop self-mentions
            .Distinct()
            .ToList();
        if (candidates.Count == 0) return System.Array.Empty<Guid>();

        var visible = await _repo.FilterVisibleUsersAsync(communityId, candidates, ct).ConfigureAwait(false);
        foreach (var userId in visible)
        {
            _repo.AddMention(Mention.Create(MentionSourceType.Reply, replyId, userId, authorId, _clock));
        }
        return visible;
    }
}
