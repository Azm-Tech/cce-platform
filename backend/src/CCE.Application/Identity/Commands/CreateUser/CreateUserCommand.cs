using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Commands.CreateUser;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    System.Guid? CountryId,
    System.Guid? CountryCodeId,
    string Role) : IRequest<Response<UserDetailDto>>;
