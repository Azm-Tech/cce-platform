using CCE.Application.Localization;
using CCE.Application.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CCE.Api.Common.Results;

/// <summary>
/// Single source of truth for the error envelope shape written by
/// <see cref="Middleware.ExceptionHandlingMiddleware"/>, the rate-limiter
/// <c>OnRejected</c> handlers, and the JWT <c>OnChallenge</c>/<c>OnForbidden</c> events.
/// </summary>
public static class EnvelopeWriter
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Writes a failure envelope with the given status code, localized message, and system code.
    /// Resolves locale from <c>Accept-Language</c> so the envelope is correct regardless of
    /// middleware ordering. Includes <c>traceId</c> and <c>correlationId</c>.
    /// </summary>
    public static async Task WriteAsync(
        HttpContext ctx,
        int statusCode,
        string domainKey,
        string? fallbackMessage = null,
        object? errors = null)
    {
        if (ctx.Response.HasStarted)
            return;

        var l = ctx.RequestServices.GetService<ILocalizationService>();
        var config = ctx.RequestServices.GetService<IConfiguration>();
        var supported = config?.GetSection("Localization:Supported").Get<string[]>();
        var defaultLocale = config?.GetValue<string>("Localization:Default");
        var locale = Middleware.LocalizationMiddleware.PickLocale(
            ctx.Request.Headers.AcceptLanguage.ToString(), supported, defaultLocale);
        var msg = l?.GetString(domainKey, locale) ?? fallbackMessage ?? "خطأ";
        var code = SystemCodeMap.ToSystemCode(domainKey);

        var envelope = new
        {
            success = false,
            code,
            message = msg,
            data = (object?)null,
            errors = errors ?? Array.Empty<object>(),
            traceId = Activity.Current?.Id ?? ctx.TraceIdentifier,
            correlationId = ctx.Items.TryGetValue(Middleware.CorrelationIdMiddleware.ItemKey, out var cid)
                ? cid?.ToString() ?? string.Empty
                : string.Empty,
            timestamp = DateTimeOffset.UtcNow,
        };

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, envelope, JsonOptions)
            .ConfigureAwait(false);
    }
}
