using CCE.Application.Common;
using CCE.Application.Verification.Dtos;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.RequestPhoneChange;

public sealed record RequestPhoneChangeCommand(
    System.Guid UserId,
    string NewPhone,
    System.Guid? CountryCodeId) : IRequest<Response<RequestVerificationResponseDto>>;
