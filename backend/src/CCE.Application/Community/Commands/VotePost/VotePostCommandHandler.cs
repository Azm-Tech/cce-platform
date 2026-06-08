using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.VotePost;

/// <summary>
/// US027 write path (§A.1): fetch the post via the repository, upsert/retract the caller's vote,
/// adjust denormalized counters + score on the aggregate, and commit once via the context (UoW).
/// Only the upvote count is exposed publicly; the downvote feeds the score only.
/// </summary>
public sealed class VotePostCommandHandler
    : IRequestHandler<VotePostCommand, Response<VoidData>>
{
    private readonly ICommunityVoteRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;
    private readonly ICommunityRealtimePublisher _realtime;

    public VotePostCommandHandler(
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

    public async Task<Response<VoidData>> Handle(VotePostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty)
            return _msg.NotAuthenticated<VoidData>();

        var post = await _repo.GetPostAsync(request.PostId, cancellationToken).ConfigureAwait(false);
        if (post is null)
            return _msg.NotFound<VoidData>(ApplicationErrors.Community.POST_NOT_FOUND);

        var existing = await _repo.FindPostVoteAsync(request.PostId, userId.Value, cancellationToken).ConfigureAwait(false);
        var oldValue = existing?.Value ?? 0;
        var newValue = (int)request.Direction;

        if (newValue == 0)
        {
            if (existing is not null) _repo.RemovePostVote(existing);
        }
        else if (existing is null)
        {
            _repo.AddPostVote(PostVote.Cast(request.PostId, userId.Value, newValue, _clock));
        }
        else
        {
            existing.ChangeTo(newValue, _clock);
        }

        post.ApplyVote(oldValue, newValue);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _realtime.PublishToPostAsync(request.PostId, "VoteChanged",
            new { postId = request.PostId, post.UpvoteCount, post.Score }, cancellationToken).ConfigureAwait(false);

        return _msg.Ok(ApplicationErrors.Community.POST_VOTED);
    }
}
