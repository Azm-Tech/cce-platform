using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Identity.Commands.RevokeStateRepAssignment;

public sealed class RevokeStateRepAssignmentCommandHandler : IRequestHandler<RevokeStateRepAssignmentCommand, Result<CCE.Application.Common.Unit>>
{
    private readonly IStateRepAssignmentRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly CCE.Application.Common.Errors _errors;

    public RevokeStateRepAssignmentCommandHandler(
        IStateRepAssignmentRepository service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        CCE.Application.Common.Errors errors)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
        _errors = errors;
    }

    public async Task<Result<CCE.Application.Common.Unit>> Handle(RevokeStateRepAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _service.FindIncludingRevokedAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (assignment is null)
        {
            return _errors.StateRepAssignmentNotFound();
        }

        var revokedById = _currentUser.GetUserId();
        if (revokedById is null)
        {
            return _errors.NotAuthenticated();
        }

        assignment.Revoke(revokedById.Value, _clock);
        await _service.UpdateAsync(assignment, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
