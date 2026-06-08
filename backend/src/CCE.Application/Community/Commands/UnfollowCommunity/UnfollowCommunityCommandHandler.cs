using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Errors;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Community.Commands.UnfollowCommunity;

public sealed class UnfollowCommunityCommandHandler
    : IRequestHandler<UnfollowCommunityCommand, Response<VoidData>>
{
    private readonly ICommunityRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public UnfollowCommunityCommandHandler(
        ICommunityRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(UnfollowCommunityCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        var existing = await _repo.FindFollowAsync(request.CommunityId, userId.Value, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            _repo.RemoveFollow(existing);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return _msg.Ok(ApplicationErrors.General.SUCCESS_OPERATION);
    }
}
