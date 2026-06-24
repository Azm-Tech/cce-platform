using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;

using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.JoinCommunity;

public sealed class JoinCommunityCommandHandler
    : IRequestHandler<JoinCommunityCommand, Response<VoidData>>
{
    private readonly ICommunityRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public JoinCommunityCommandHandler(
        ICommunityRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser,
        ISystemClock clock, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(JoinCommunityCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        var community = await _repo.GetAsync(request.CommunityId, cancellationToken).ConfigureAwait(false);
        if (community is null || !community.IsActive)
            return _msg.NotFound<VoidData>(MessageKeys.Community.COMMUNITY_NOT_FOUND);

        if (await _repo.HasMembershipAsync(request.CommunityId, userId.Value, cancellationToken).ConfigureAwait(false))
            return _msg.Conflict<VoidData>(MessageKeys.General.DUPLICATE_VALUE);

        if (community.IsPublic)
        {
            _repo.AddMembership(CommunityMembership.Join(community.Id, userId.Value, CommunityRole.Member, _clock));
            community.IncrementMembers();
        }
        else
        {
            if (await _repo.HasPendingRequestAsync(request.CommunityId, userId.Value, cancellationToken).ConfigureAwait(false))
                return _msg.Conflict<VoidData>(MessageKeys.General.DUPLICATE_VALUE);
            var joinRequest = CommunityJoinRequest.Submit(community.Id, userId.Value, _clock);
            _repo.AddJoinRequest(joinRequest);

            // Raise the domain event on the aggregate with the REAL join-request id; the bridge handler
            // (CommunityJoinRequestedBusPublisher) stages the integration event into the EF outbox during
            // the SaveChanges below, so the moderator notification is atomic with the request row.
            community.RegisterJoinRequest(joinRequest.Id, userId.Value, _clock);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(MessageKeys.General.SUCCESS_OPERATION);
    }
}
