using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.FollowCommunity;

public sealed class FollowCommunityCommandHandler
    : IRequestHandler<FollowCommunityCommand, Response<VoidData>>
{
    private readonly ICommunityRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public FollowCommunityCommandHandler(
        ICommunityRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser,
        ISystemClock clock, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(FollowCommunityCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        var community = await _repo.GetAsync(request.CommunityId, cancellationToken).ConfigureAwait(false);
        if (community is null || !community.IsActive)
            return _msg.NotFound<VoidData>(ApplicationErrors.Community.COMMUNITY_NOT_FOUND);

        var existing = await _repo.FindFollowAsync(request.CommunityId, userId.Value, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            _repo.AddFollow(CommunityFollow.Follow(community.Id, userId.Value, _clock));
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return _msg.Ok(ApplicationErrors.General.SUCCESS_OPERATION);
    }
}
