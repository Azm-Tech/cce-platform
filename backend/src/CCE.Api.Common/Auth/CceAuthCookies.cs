namespace CCE.Api.Common.Auth;

/// <summary>
/// Sub-11 — well-known cookie names for the Microsoft.Identity.Web auth
/// pipeline. Consumed by RedisOutputCacheMiddleware (cache-bypass for
/// authenticated users) and TieredRateLimiterRegistration (rate-limit
/// partition keying for sessioned users).
///
/// Pre-Sub-11 this surface lived on <c>BffSessionCookie</c> in the
/// custom-BFF cluster (deleted in Phase 04).
/// </summary>
public static class CceAuthCookies
{
    /// <summary>
    /// Default cookie name used by ASP.NET Core's cookie authentication
    /// scheme that Microsoft.Identity.Web's <c>AddMicrosoftIdentityWebApp</c>
    /// pipeline issues. If the operator changes the cookie name via
    /// CookieAuthenticationOptions.Cookie.Name, the consumers need updating.
    /// </summary>
    public const string SessionCookieName = ".AspNetCore.Cookies";
}
