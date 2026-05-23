using CCE.Application.Common;
using CCE.Application.Verification.Dtos;
using MediatR;

namespace CCE.Application.Verification.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(
    Guid VerificationId,
    string Code)
    : IRequest<Response<VerifyOtpResponseDto>>;
