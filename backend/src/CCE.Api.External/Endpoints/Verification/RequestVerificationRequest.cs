using CCE.Domain.Verification;

namespace CCE.Api.External.Endpoints.Verification;

public sealed record RequestVerificationRequest(
    string? Token,
    string? ProviderName,
    string Contact,
    OtpVerificationType TypeId);
