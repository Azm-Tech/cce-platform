using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Identity.Commands.RevokeStateRepAssignment;

public sealed class RevokeStateRepAssignmentCommandHandler : IRequestHandler<RevokeStateRepAssignmentCommand, Response<VoidData>>
{
    private readonly ICceDbContext _db;
    private readonly IStateRepAssignmentRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public RevokeStateRepAssignmentCommandHandler(
        ICceDbContext db,
        IStateRepAssignmentRepository service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg)
    {
        _db = db;
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(RevokeStateRepAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _service.FindIncludingRevokedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (assignment is null)
        {
            return _msg.NotFound<VoidData>("STATE_REP_ASSIGNMENT_NOT_FOUND");
        }

        var revokedById = _currentUser.GetUserId();
        if (revokedById is null)
        {
            return _msg.NotAuthenticated<VoidData>();
        }

        assignment.Revoke(revokedById.Value, _clock);
        _service.Update(assignment);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok("STATE_REP_ASSIGNMENT_REVOKED");
    }
}
