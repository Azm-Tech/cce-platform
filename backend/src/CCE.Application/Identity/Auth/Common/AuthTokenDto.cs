namespace CCE.Application.Identity.Auth.Common;

public sealed record AuthTokenDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    string TokenType,
    AuthUserDto User);
