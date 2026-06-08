using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.ApproveJoinRequest;

public sealed class ApproveJoinRequestCommandHandler
    : IRequestHandler<ApproveJoinRequestCommand, Response<VoidData>>
{
    private readonly ICommunityRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public ApproveJoinRequestCommandHandler(
        ICommunityRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser,
        ISystemClock clock, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(ApproveJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var by = _currentUser.GetUserId();
        if (by is null || by == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        var joinRequest = await _repo.GetRequestAsync(request.RequestId, cancellationToken).ConfigureAwait(false);
        if (joinRequest is null) return _msg.NotFound<VoidData>(ApplicationErrors.Community.JOIN_REQUEST_NOT_FOUND);

        joinRequest.Approve(by.Value, _clock);

        // Idempotency: only add membership if the user isn't already a member.
        if (!await _repo.HasMembershipAsync(joinRequest.CommunityId, joinRequest.UserId, cancellationToken).ConfigureAwait(false))
        {
            _repo.AddMembership(CommunityMembership.Join(joinRequest.CommunityId, joinRequest.UserId, CommunityRole.Member, _clock));
            var community = await _repo.GetAsync(joinRequest.CommunityId, cancellationToken).ConfigureAwait(false);
            community?.IncrementMembers();
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok(ApplicationErrors.General.SUCCESS_OPERATION);
    }
}
