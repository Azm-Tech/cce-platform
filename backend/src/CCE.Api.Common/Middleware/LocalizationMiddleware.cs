using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace CCE.Api.Common.Middleware;

public sealed class LocalizationMiddleware
{
    private readonly string[] _supported;
    private readonly string _defaultLocale;
    private readonly RequestDelegate _next;

    public LocalizationMiddleware(RequestDelegate next, IConfiguration? configuration = null)
    {
        _next = next;
        _supported = configuration?.GetSection("Localization:Supported").Get<string[]>() ?? ["ar", "en"];
        _defaultLocale = configuration?.GetValue<string>("Localization:Default") ?? "ar";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var locale = PickLocale(context.Request.Headers.AcceptLanguage.ToString(), _supported, _defaultLocale);
        var culture = CultureInfo.GetCultureInfo(locale);

        var prevCulture = CultureInfo.CurrentCulture;
        var prevUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            CultureInfo.CurrentCulture = prevCulture;
            CultureInfo.CurrentUICulture = prevUiCulture;
        }
    }

    internal static string PickLocale(string acceptLanguage, string[]? supported = null, string? defaultLocale = null)
    {
        supported ??= ["ar", "en"];
        defaultLocale ??= "ar";

        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return defaultLocale;
        }
        foreach (var entry in acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tag = entry.Split(';', 2)[0].Trim();
            var primary = tag.Split('-', 2)[0].ToLowerInvariant();
            if (Array.IndexOf(supported, primary) >= 0)
            {
                return primary;
            }
        }
        return defaultLocale;
    }
}