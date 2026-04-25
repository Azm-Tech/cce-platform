using MediatR;

namespace CCE.Application.Health;

/// <summary>
/// Authenticated health query — exercised by the Internal API after JWT validation.
/// Caller (the API endpoint) extracts claims from the validated JWT and passes them in.
/// Handler echoes them with status + timestamp.
/// </summary>
public sealed record AuthenticatedHealthQuery(
    string UserId,
    string PreferredUsername,
    string Email,
    string Upn,
    IReadOnlyList<string> Groups,
    string? Locale) : IRequest<AuthenticatedHealthResult>;
