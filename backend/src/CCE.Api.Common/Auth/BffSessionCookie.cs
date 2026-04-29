using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CCE.Api.Common.Auth;

public sealed class BffSessionCookie
{
    public const string CookieName = "cce.session";
    private const string Purpose = "cce.bff.session.v1";

    private readonly IDataProtector _protector;
    private readonly IOptions<BffOptions> _opts;

    public BffSessionCookie(IDataProtectionProvider provider, IOptions<BffOptions> opts)
    {
        _protector = provider.CreateProtector(Purpose);
        _opts = opts;
    }

    public BffSession? TryRead(HttpContext ctx)
    {
        if (!ctx.Request.Cookies.TryGetValue(CookieName, out var encrypted) || string.IsNullOrEmpty(encrypted))
        {
            return null;
        }
        try
        {
            var json = _protector.Unprotect(encrypted);
            return JsonSerializer.Deserialize<BffSession>(json);
        }
        catch (System.Exception)
        {
            return null;
        }
    }

    public void Write(HttpContext ctx, BffSession session)
    {
        var json = JsonSerializer.Serialize(session);
        var encrypted = _protector.Protect(json);
        ctx.Response.Cookies.Append(CookieName, encrypted, BuildCookieOptions());
    }

    public void Clear(HttpContext ctx)
    {
        ctx.Response.Cookies.Delete(CookieName, BuildCookieOptions());
    }

    private CookieOptions BuildCookieOptions()
    {
        var opts = _opts.Value;
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Domain = opts.CookieDomain,
            Path = "/",
            MaxAge = System.TimeSpan.FromMinutes(opts.SessionLifetimeMinutes),
        };
    }
}
