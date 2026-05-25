namespace CCE.Application.Verification.Dtos;

public sealed record VerifyOtpResponseDto(
    bool Verified,
    Guid? UserId);
