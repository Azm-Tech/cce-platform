using CCE.Application.Common;
using CCE.Application.Verification.Dtos;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.RequestEmailChange;

public sealed record RequestEmailChangeCommand(
    System.Guid UserId,
    string NewEmail) : IRequest<Response<RequestVerificationResponseDto>>;
