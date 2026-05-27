namespace CCE.Application.Identity.Public.Commands.ConfirmEmailChange;

public sealed record ConfirmEmailChangeRequest(System.Guid VerificationId, string Code);
