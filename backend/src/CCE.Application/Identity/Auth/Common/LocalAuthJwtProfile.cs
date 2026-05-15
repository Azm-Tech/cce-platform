namespace CCE.Application.Identity.Auth.Common;

public sealed record LocalAuthJwtProfile
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
}
