using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.PublishPost;

public sealed class PublishPostCommandHandler
    : IRequestHandler<PublishPostCommand, Response<VoidData>>
{
    private readonly IPostRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public PublishPostCommandHandler(
        IPostRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser,
        ISystemClock clock, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(PublishPostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        var post = await _repo.GetAsync(request.PostId, cancellationToken).ConfigureAwait(false);
        if (post is null) return _msg.NotFound<VoidData>(ApplicationErrors.Community.POST_NOT_FOUND);
        if (post.AuthorId != userId.Value) return _msg.Forbidden<VoidData>(ApplicationErrors.General.FORBIDDEN);

        post.Publish(_clock);

        var author = await _db.Users
            .FirstOrDefaultAsyncEither(u => u.Id == post.AuthorId, cancellationToken)
            .ConfigureAwait(false);
        author?.IncrementPostsCount();

        var community = await _db.Communities
            .FirstOrDefaultAsyncEither(c => c.Id == post.CommunityId, cancellationToken)
            .ConfigureAwait(false);
        community?.IncrementPosts();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok(ApplicationErrors.Community.POST_PUBLISHED);
    }
}
