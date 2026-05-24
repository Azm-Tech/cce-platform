using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Commands.CreateUser;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    Guid? CountryId,
    string Role) : IRequest<Response<UserDetailDto>>;
