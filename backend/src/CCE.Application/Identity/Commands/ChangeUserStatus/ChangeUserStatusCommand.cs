using CCE.Application.Common;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Commands.ChangeUserStatus;

public sealed record ChangeUserStatusCommand(
    Guid UserId,
    bool IsActive) : IRequest<Response<UserDetailDto>>;
