namespace CCE.Application.Verification.Dtos;

public sealed record RequestVerificationResponseDto(
    Guid VerificationId,
    DateTimeOffset ExpiresAt,
    int CooldownSeconds = 60);
