using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using MediatR;

namespace CCE.Application.Identity.Auth.Register;

public sealed record RegisterUserCommand(
    string FirstName,
    string LastName,
    string EmailAddress,
    string JobTitle,
    string OrganizationName,
    string PhoneNumber,
    string Password,
    string ConfirmPassword)
    : IRequest<Response<AuthUserDto>>;
