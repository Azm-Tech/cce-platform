using CCE.Application.Common;
using CCE.Application.Verification.Dtos;
using CCE.Domain.Verification;
using MediatR;

namespace CCE.Application.Verification.Commands.RequestVerification;

public sealed record RequestVerificationCommand(
    string? Token,
    string? ProviderName,
    string Contact,
    OtpVerificationType TypeId)
    : IRequest<Response<RequestVerificationResponseDto>>;
