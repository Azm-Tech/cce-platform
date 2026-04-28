using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Queries.GetUserById;
using MediatR;

namespace CCE.Application.Identity.Commands.AssignUserRoles;

public sealed class AssignUserRolesCommandHandler : IRequestHandler<AssignUserRolesCommand, UserDetailDto?>
{
    private readonly IUserRoleAssignmentService _service;
    private readonly IMediator _mediator;

    public AssignUserRolesCommandHandler(IUserRoleAssignmentService service, IMediator mediator)
    {
        _service = service;
        _mediator = mediator;
    }

    public async Task<UserDetailDto?> Handle(AssignUserRolesCommand request, CancellationToken cancellationToken)
    {
        var ok = await _service.ReplaceRolesAsync(request.Id, request.Roles, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return null;
        }
        return await _mediator.Send(new GetUserByIdQuery(request.Id), cancellationToken).ConfigureAwait(false);
    }
}
