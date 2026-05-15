namespace CCE.Application.Identity.Public;

public sealed record RegisterUserRequest(
    string GivenName,
    string Surname,
    string Email,
    string MailNickname);

public sealed record RegisterUserResponse(
    System.Guid EntraIdObjectId,
    string UserPrincipalName,
    string DisplayName);
