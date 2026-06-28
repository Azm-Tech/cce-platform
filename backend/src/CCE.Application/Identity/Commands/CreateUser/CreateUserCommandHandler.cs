using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Queries.GetUserById;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Commands.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Response<UserDetailDto>>
{
    private readonly IAuthService _auth;
    private readonly IMediator _mediator;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public CreateUserCommandHandler(IAuthService auth, IMediator mediator, ICurrentUserAccessor currentUser, MessageFactory msg)
    {
        _auth = auth;
        _mediator = mediator;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<UserDetailDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var createdBy = _currentUser.GetUserId() ?? Guid.Empty;
        var result = await _auth.AdminCreateUserAsync(
            request.FirstName, request.LastName, request.Email,
            request.PhoneNumber, request.CountryId, request.Role, createdBy, cancellationToken).ConfigureAwait(false);

        if (result.EmailTaken) return _msg.Conflict<UserDetailDto>(MessageKeys.Identity.EMAIL_EXISTS);
        if (result.Failed || result.User is null) return _msg.BusinessRule<UserDetailDto>(MessageKeys.Identity.REGISTRATION_FAILED);

        var detail = await _mediator.Send(new GetUserByIdQuery(result.User.Id), cancellationToken).ConfigureAwait(false);
        if (!detail.Success) return detail;

        return _msg.Ok(detail.Data!, result.PasswordResetSent ? MessageKeys.Identity.USER_CREATED : MessageKeys.Identity.REGISTER_SUCCESS);
    }
}
