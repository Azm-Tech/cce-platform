namespace CCE.Application.Identity.Auth.Common;

public sealed record LocalAuthOptions
{
    public const string SectionName = "LocalAuth";

    public LocalAuthJwtProfile External { get; init; } = new();
    public LocalAuthJwtProfile Internal { get; init; } = new();
    public int AccessTokenMinutes { get; init; } = 10;
    public int RefreshTokenDays { get; init; } = 30;
    public int PasswordResetTokenHours { get; init; } = 2;
    public bool RequireConfirmedEmail { get; init; }

    public LocalAuthJwtProfile GetProfile(LocalAuthApi api)
        => api == LocalAuthApi.Internal ? Internal : External;
}
