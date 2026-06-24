using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;

using MediatR;

namespace CCE.Application.Community.Commands.LeaveCommunity;

public sealed class LeaveCommunityCommandHandler
    : IRequestHandler<LeaveCommunityCommand, Response<VoidData>>
{
    private readonly ICommunityRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public LeaveCommunityCommandHandler(
        ICommunityRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(LeaveCommunityCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        var membership = await _repo.FindMembershipAsync(request.CommunityId, userId.Value, cancellationToken).ConfigureAwait(false);
        if (membership is not null)
        {
            _repo.RemoveMembership(membership);
            var community = await _repo.GetAsync(request.CommunityId, cancellationToken).ConfigureAwait(false);
            community?.DecrementMembers();
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return _msg.Ok(MessageKeys.General.SUCCESS_OPERATION);
    }
}
