namespace CCE.Api.Common.Auth;

/// <summary>Decrypted session payload.</summary>
public sealed record BffSession(
    string AccessToken,
    string RefreshToken,
    System.DateTimeOffset ExpiresAt);
