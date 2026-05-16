using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Queries.GetUserById;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Commands.AssignUserRoles;

public sealed class AssignUserRolesCommandHandler : IRequestHandler<AssignUserRolesCommand, Response<UserDetailDto>>
{
    private readonly IUserRoleAssignmentRepository _service;
    private readonly IMediator _mediator;
    private readonly MessageFactory _msg;

    public AssignUserRolesCommandHandler(
        IUserRoleAssignmentRepository service,
        IMediator mediator,
        MessageFactory msg)
    {
        _service = service;
        _mediator = mediator;
        _msg = msg;
    }

    public async Task<Response<UserDetailDto>> Handle(AssignUserRolesCommand request, CancellationToken cancellationToken)
    {
        var ok = await _service.ReplaceRolesAsync(request.Id, request.Roles, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return _msg.UserNotFound<UserDetailDto>();
        }

        var result = await _mediator.Send(new GetUserByIdQuery(request.Id), cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            return result;
        }

        return _msg.Ok(result.Data!, "ROLES_ASSIGNED");
    }
}
