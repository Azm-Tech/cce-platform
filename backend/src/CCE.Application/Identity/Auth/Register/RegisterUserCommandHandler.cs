using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Auth.Register;

internal sealed class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, Response<AuthUserDto>>
{
    private readonly IAuthService _auth;
    private readonly MessageFactory _msg;

    public RegisterUserCommandHandler(IAuthService auth, MessageFactory msg)
    {
        _auth = auth;
        _msg = msg;
    }

    public async Task<Response<AuthUserDto>> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request.FirstName, request.LastName,
            request.EmailAddress, request.Password, request.JobTitle,
            request.OrganizationName, request.PhoneNumber, request.CountryId, ct).ConfigureAwait(false);

        if (result.EmailTaken) return _msg.Conflict<AuthUserDto>(MessageKeys.Identity.EMAIL_EXISTS);
        if (result.User is null) return _msg.BusinessRule<AuthUserDto>(MessageKeys.Identity.REGISTRATION_FAILED);

        return _msg.Ok(new AuthUserDto(
            result.User.Id,
            result.User.Email ?? request.EmailAddress,
            result.User.FirstName,
            result.User.LastName,
            result.User.AvatarUrl,
            ["cce-user"],
            []), MessageKeys.Identity.REGISTER_SUCCESS);
    }
}
