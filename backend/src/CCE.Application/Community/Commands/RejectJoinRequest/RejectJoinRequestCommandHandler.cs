using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;

using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.RejectJoinRequest;

public sealed class RejectJoinRequestCommandHandler
    : IRequestHandler<RejectJoinRequestCommand, Response<VoidData>>
{
    private readonly ICommunityRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public RejectJoinRequestCommandHandler(
        ICommunityRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser,
        ISystemClock clock, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(RejectJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var by = _currentUser.GetUserId();
        if (by is null || by == Guid.Empty) return _msg.Unauthorized<VoidData>(MessageKeys.Identity.NOT_AUTHENTICATED);

        var joinRequest = await _repo.GetRequestAsync(request.RequestId, cancellationToken).ConfigureAwait(false);
        if (joinRequest is null) return _msg.NotFound<VoidData>(MessageKeys.Community.JOIN_REQUEST_NOT_FOUND);

        joinRequest.Reject(by.Value, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.General.SUCCESS_OPERATION);
    }
}
