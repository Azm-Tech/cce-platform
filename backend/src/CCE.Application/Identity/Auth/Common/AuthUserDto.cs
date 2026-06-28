namespace CCE.Application.Identity.Auth.Common;

public sealed record AuthUserDto(
    System.Guid Id,
    string EmailAddress,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Claims);
