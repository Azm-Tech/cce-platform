namespace CCE.Application.Identity.Auth.Common;

public sealed record AuthUserDto(
    System.Guid Id,
    string EmailAddress,
    string FirstName,
    string LastName,
    IReadOnlyCollection<string> Roles);
