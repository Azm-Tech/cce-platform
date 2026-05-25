namespace CCE.Application.Identity.Auth.Register;

public sealed record RegisterUserRequest(
    string FirstName,
    string LastName,
    string EmailAddress,
    string JobTitle,
    string OrganizationName,
    string PhoneNumber,
    string Password,
    string ConfirmPassword);
