using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Realtime;
using CCE.Application.Community;
using CCE.Application.Identity;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.SoftDeleteReply;

public sealed class SoftDeleteReplyCommandHandler : IRequestHandler<SoftDeleteReplyCommand, Unit>
{
    private readonly ICommunityModerationService _service;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly ICommunityRealtimePublisher _realtime;
    private readonly IUserRepository _userRepo;

    public SoftDeleteReplyCommandHandler(
        ICommunityModerationService service,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        ICommunityRealtimePublisher realtime,
        IUserRepository userRepo)
    {
        _service = service;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _realtime = realtime;
        _userRepo = userRepo;
    }

    public async Task<Unit> Handle(SoftDeleteReplyCommand request, CancellationToken cancellationToken)
    {
        var reply = await _service.FindReplyAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (reply is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"PostReply {request.Id} not found.");
        }

        var moderatorId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot moderate a reply from a request without a user identity.");

        reply.SoftDelete(moderatorId, _clock);

        // Decrement the denormalized comment count atomically with the reply soft-delete.
        var post = await _service.FindPostAsync(reply.PostId, cancellationToken).ConfigureAwait(false);
        if (post is not null)
        {
            post.DecrementCommentsCount(_clock);
        }

        var replyAuthor = await _userRepo.FindAsync(reply.AuthorId, cancellationToken).ConfigureAwait(false);
        replyAuthor?.DecrementCommentsCount();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Notify the post room (a reply was removed) and the moderation room.
        // Wrap each envelope once for consistency with the SoftDeletePost handler.
        var postModerated = RealtimeEnvelope.Wrap(new PostModeratedRealtime(reply.PostId, reply.Id, "SoftDeleted"));
        await _realtime.PublishToPostAsync(reply.PostId, RealtimeEvents.PostModerated, postModerated, cancellationToken).ConfigureAwait(false);

        var contentModerated = RealtimeEnvelope.Wrap(new ContentModeratedRealtime("Reply", reply.Id, reply.PostId, moderatorId, "SoftDeleted"));
        await _realtime.PublishToModeratorsAsync(RealtimeEvents.ContentModerated, contentModerated, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
