using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace CCE.Api.Common.Middleware;

public sealed class LocalizationMiddleware
{
    private static readonly string[] Supported = ["ar", "en"];
    private const string DefaultLocale = "ar";

    private readonly RequestDelegate _next;

    public LocalizationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var locale = PickLocale(context.Request.Headers.AcceptLanguage.ToString());
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

    private static string PickLocale(string acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return DefaultLocale;
        }
        // Parse comma-separated entries, trim quality factors, take first matching supported tag.
        foreach (var entry in acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tag = entry.Split(';', 2)[0].Trim();
            // "en-US" -> "en"
            var primary = tag.Split('-', 2)[0].ToLowerInvariant();
            if (Array.IndexOf(Supported, primary) >= 0)
            {
                return primary;
            }
        }
        return DefaultLocale;
    }
}
