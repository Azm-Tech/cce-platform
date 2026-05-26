namespace CCE.Application.Identity.Public.Commands.ConfirmPhoneChange;

public sealed record ConfirmPhoneChangeRequest(System.Guid VerificationId, string Code);
