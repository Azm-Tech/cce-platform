namespace CCE.Integration.AdminAuth;

public sealed record AdAuthResponse(
    string Status,
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    string? DisplayName = null,
    IReadOnlyList<string>? Groups = null,
    string? Error = null);
