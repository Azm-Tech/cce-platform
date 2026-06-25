using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Realtime;
using CCE.Application.Messages;

using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.VoteReply;

/// <summary>US027 reply-voting write path (§A.1). Mirrors <c>VotePostCommandHandler</c> for replies.</summary>
public sealed class VoteReplyCommandHandler
    : IRequestHandler<VoteReplyCommand, Response<VoidData>>
{
    private readonly ICommunityVoteRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;
    private readonly ICommunityRealtimePublisher _realtime;

    public VoteReplyCommandHandler(
        ICommunityVoteRepository repo,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg,
        ICommunityRealtimePublisher realtime)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
        _realtime = realtime;
    }

    public async Task<Response<VoidData>> Handle(VoteReplyCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty)
            return _msg.Unauthorized<VoidData>(MessageKeys.Identity.NOT_AUTHENTICATED);

        var reply = await _repo.GetReplyAsync(request.ReplyId, cancellationToken).ConfigureAwait(false);
        if (reply is null)
            return _msg.NotFound<VoidData>(MessageKeys.Community.REPLY_NOT_FOUND);

        var existing = await _repo.FindReplyVoteAsync(request.ReplyId, userId.Value, cancellationToken).ConfigureAwait(false);
        var oldValue = existing?.Value ?? 0;
        var newValue = (int)request.Direction;

        if (newValue == 0)
        {
            if (existing is not null) _repo.RemoveReplyVote(existing);
        }
        else if (existing is null)
        {
            _repo.AddReplyVote(ReplyVote.Cast(request.ReplyId, userId.Value, newValue, _clock));
        }
        else
        {
            existing.ChangeTo(newValue, _clock);
        }

        reply.ApplyVote(oldValue, newValue);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _realtime.PublishToPostAsync(reply.PostId, RealtimeEvents.VoteChanged,
            new { replyId = reply.Id, reply.UpvoteCount, reply.DownvoteCount, reply.Score }, cancellationToken).ConfigureAwait(false);

        return _msg.Ok(MessageKeys.Community.POST_VOTED);
    }
}
