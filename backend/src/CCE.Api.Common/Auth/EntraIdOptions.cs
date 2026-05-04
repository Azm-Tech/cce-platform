namespace CCE.Api.Common.Auth;

/// <summary>
/// Strongly-typed accessor for the EntraId: configuration section.
/// Microsoft.Identity.Web reads its own MicrosoftIdentityOptions
/// directly from the same section via AddMicrosoftIdentityWebApi(...);
/// this record exposes the values to downstream CCE services
/// (EntraIdRegistrationService, EntraIdGraphClientFactory) via DI.
/// </summary>
public sealed class EntraIdOptions
{
    public const string SectionName = "EntraId";

    public string Instance { get; init; } = "https://login.microsoftonline.com/";
    public string TenantId { get; init; } = "common";   // multi-tenant default
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// CCE's own tenant ID (a real GUID, not "common"). Used for Microsoft Graph
    /// user-create calls — we only ever write users into CCE's own tenant.
    /// </summary>
    public string GraphTenantId { get; init; } = string.Empty;

    /// <summary>
    /// CCE's verified domain in its own tenant (e.g. cce.onmicrosoft.com or
    /// cce.local if a custom domain is verified). Used to construct UPNs for
    /// new users via Graph: <c>{mailNickname}@{GraphTenantDomain}</c>.
    /// </summary>
    public string GraphTenantDomain { get; init; } = string.Empty;
}
