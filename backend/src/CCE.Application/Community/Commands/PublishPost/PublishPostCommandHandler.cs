using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.Identity;

using CCE.Domain.Common;
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

    public PublishPostCommandHandler(
        IPostRepository repo, ICommunityRepository communityRepo, ICceDbContext db,
        ICurrentUserAccessor currentUser, ISystemClock clock, MessageFactory msg,
        IUserRepository userRepo)
    {
        _repo = repo;
        _communityRepo = communityRepo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
        _userRepo = userRepo;
    }

    public async Task<Response<VoidData>> Handle(PublishPostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        var post = await _repo.GetAsync(request.PostId, cancellationToken).ConfigureAwait(false);
        if (post is null) return _msg.NotFound<VoidData>(MessageKeys.Community.POST_NOT_FOUND);
        if (post.AuthorId != userId.Value) return _msg.Forbidden<VoidData>(MessageKeys.General.FORBIDDEN);

        post.Publish(_clock);

        var author = await _userRepo.FindAsync(post.AuthorId, cancellationToken).ConfigureAwait(false);
        author?.IncrementPostsCount();

        var community = await _communityRepo.GetAsync(post.CommunityId, cancellationToken).ConfigureAwait(false);
        community?.IncrementPosts();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.Community.POST_PUBLISHED);
    }
}
