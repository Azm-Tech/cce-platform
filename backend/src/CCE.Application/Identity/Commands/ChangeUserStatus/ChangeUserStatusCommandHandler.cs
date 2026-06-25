using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Public;
using CCE.Application.Identity.Queries.GetUserById;
using CCE.Application.Messages;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Commands.ChangeUserStatus;

public sealed class ChangeUserStatusCommandHandler : IRequestHandler<ChangeUserStatusCommand, Response<UserDetailDto>>
{
    private readonly ICceDbContext _db;
    private readonly IUserProfileRepository _service;
    private readonly IMediator _mediator;
    private readonly MessageFactory _msg;

    public ChangeUserStatusCommandHandler(
        ICceDbContext db,
        IUserProfileRepository service,
        IMediator mediator,
        MessageFactory msg)
    {
        _db = db;
        _service = service;
        _mediator = mediator;
        _msg = msg;
    }

    public async Task<Response<UserDetailDto>> Handle(ChangeUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return _msg.NotFound<UserDetailDto>(MessageKeys.Identity.USER_NOT_FOUND);
        }

        var newStatus = request.IsActive ? UserStatus.Active : UserStatus.Inactive;
        user.ChangeStatus(newStatus);

        _service.Update(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = await _mediator.Send(new GetUserByIdQuery(request.UserId), cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            return result;
        }

        return _msg.Ok(result.Data!, MessageKeys.Identity.USER_STATUS_CHANGED);
    }
}
