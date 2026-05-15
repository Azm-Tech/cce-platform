namespace CCE.Application.Identity.Auth.ResetPassword;

public sealed record ResetPasswordRequest(
    string EmailAddress,
    string Token,
    string NewPassword,
    string ConfirmPassword);
