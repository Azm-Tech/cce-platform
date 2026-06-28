namespace CCE.Api.External.Endpoints.Verification;

public sealed record VerifyOtpRequest(Guid VerificationId, string Code);
