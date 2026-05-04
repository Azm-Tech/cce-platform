using Microsoft.IdentityModel.Tokens;

namespace CCE.Api.Common.Auth;

/// <summary>
/// Sub-11 — multi-tenant Entra ID issuer validator. Accepts any token whose
/// issuer matches the canonical <c>https://login.microsoftonline.com/&lt;tenant-id&gt;/v2.0</c>
/// shape — regardless of which tenant. Used by JwtBearer's
/// TokenValidationParameters.IssuerValidator and exposed publicly for unit
/// testing.
/// </summary>
public static class EntraIdIssuerValidator
{
    private const string ExpectedPrefix = "https://login.microsoftonline.com/";
    private const string ExpectedSuffix = "/v2.0";

    /// <summary>
    /// Returns the issuer string if it matches the expected shape; otherwise
    /// throws <see cref="SecurityTokenInvalidIssuerException"/> (the contract
    /// the JwtBearer validator expects).
    /// </summary>
    public static string Validate(string issuer)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new SecurityTokenInvalidIssuerException("Issuer is null or empty.");
        }
        if (issuer.StartsWith(ExpectedPrefix, StringComparison.OrdinalIgnoreCase)
            && issuer.EndsWith(ExpectedSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return issuer;
        }
        throw new SecurityTokenInvalidIssuerException($"Issuer '{issuer}' is not a recognized Entra ID issuer.");
    }
}
