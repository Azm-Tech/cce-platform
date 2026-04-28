using CCE.Application.Common.Interfaces;
using CCE.Application.Identity;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Identity.Commands.RevokeStateRepAssignment;

public sealed class RevokeStateRepAssignmentCommandHandler : IRequestHandler<RevokeStateRepAssignmentCommand, Unit>
{
    private readonly IStateRepAssignmentService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public RevokeStateRepAssignmentCommandHandler(
        IStateRepAssignmentService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(RevokeStateRepAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _service.FindIncludingRevokedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (assignment is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"State-rep assignment {request.Id} not found.");
        }

        var revokedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot revoke state-rep assignment from a request without a user identity.");

        assignment.Revoke(revokedById, _clock);
        await _service.UpdateAsync(assignment, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
