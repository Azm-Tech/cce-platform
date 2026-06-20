using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Common.Realtime;
using CCE.Application.Community;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.SoftDeletePost;

public sealed class SoftDeletePostCommandHandler : IRequestHandler<SoftDeletePostCommand, Unit>
{
    private readonly ICommunityModerationService _service;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly ICommunityRealtimePublisher _realtime;
    private readonly IRedisFeedStore _feedStore;

    public SoftDeletePostCommandHandler(
        ICommunityModerationService service,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        ICommunityRealtimePublisher realtime,
        IRedisFeedStore feedStore)
    {
        _service = service;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _realtime = realtime;
        _feedStore = feedStore;
    }

    public async Task<Unit> Handle(SoftDeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _service.FindPostAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Post {request.Id} not found.");
        }

        var moderatorId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot moderate a post from a request without a user identity.");

        var wasPublished = post.Status == PostStatus.Published;
        post.SoftDelete(moderatorId, _clock);

        if (wasPublished)
        {
            var author = await _db.Users
                .FirstOrDefaultAsyncEither(u => u.Id == post.AuthorId, cancellationToken)
                .ConfigureAwait(false);
            author?.DecrementPostsCount();
        }

        await _service.UpdatePostAsync(post, cancellationToken).ConfigureAwait(false);

        // Remove the deleted post from Redis immediately so pagination totals stay accurate.
        // Personal feed:user:{*} keys self-heal at 24h TTL — there is no reverse index to enumerate them.
        if (wasPublished)
        {
            await _feedStore.RemovePostFromAllFeedsAsync(post.CommunityId, post.Id, cancellationToken)
                .ConfigureAwait(false);
        }

        // Tell the post + community rooms the post was removed, and the moderation room who did it.
        await _realtime.PublishToPostAsync(post.Id, RealtimeEvents.PostModerated,
            new PostModeratedRealtime(post.Id, null, "SoftDeleted"), cancellationToken).ConfigureAwait(false);
        await _realtime.PublishToCommunityAsync(post.CommunityId, RealtimeEvents.PostModerated,
            new PostModeratedRealtime(post.Id, null, "SoftDeleted"), cancellationToken).ConfigureAwait(false);
        await _realtime.PublishToModeratorsAsync(RealtimeEvents.ContentModerated,
            new ContentModeratedRealtime("Post", post.Id, post.Id, moderatorId, "SoftDeleted"), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
