using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Queries.GetUserById;
using MediatR;

namespace CCE.Application.Identity.Commands.AssignUserRoles;

public sealed class AssignUserRolesCommandHandler : IRequestHandler<AssignUserRolesCommand, Result<UserDetailDto>>
{
    private readonly IUserRoleAssignmentRepository _service;
    private readonly IMediator _mediator;
    private readonly CCE.Application.Common.Errors _errors;

    public AssignUserRolesCommandHandler(
        IUserRoleAssignmentRepository service,
        IMediator mediator,
        CCE.Application.Common.Errors errors)
    {
        _service = service;
        _mediator = mediator;
        _errors = errors;
    }

    public async Task<Result<UserDetailDto>> Handle(AssignUserRolesCommand request, CancellationToken cancellationToken)
    {
        var ok = await _service.ReplaceRolesAsync(request.Id, request.Roles, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return _errors.UserNotFound();
        }

        var result = await _mediator.Send(new GetUserByIdQuery(request.Id), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result;
        }

        return result.Data!;
    }
}
