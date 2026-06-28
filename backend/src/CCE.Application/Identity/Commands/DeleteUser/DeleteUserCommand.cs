using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : IRequest<Response<UserDetailDto>>;
