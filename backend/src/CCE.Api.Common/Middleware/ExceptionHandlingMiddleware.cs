using CCE.Application.Common;
using CCE.Application.Localization;
using CCE.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CCE.Api.Common.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            await WriteValidationResultAsync(context, ex).ConfigureAwait(false);
        }
        // Expected business outcomes — not logged (not server errors).
        catch (ConcurrencyException ex)
        {
            await WriteErrorResultAsync(context, StatusCodes.Status409Conflict,
                "CONCURRENCY_CONFLICT", ErrorType.Conflict, ex.Message).ConfigureAwait(false);
        }
        catch (DuplicateException ex)
        {
            await WriteErrorResultAsync(context, StatusCodes.Status409Conflict,
                "DUPLICATE_VALUE", ErrorType.Conflict, ex.Message).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            await WriteErrorResultAsync(context, StatusCodes.Status400BadRequest,
                "GENERAL_BAD_REQUEST", ErrorType.BusinessRule, ex.Message).ConfigureAwait(false);
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            // Legacy — still caught for non-migrated handlers
            await WriteErrorResultAsync(context, StatusCodes.Status404NotFound,
                "GENERAL_NOT_FOUND", ErrorType.NotFound, ex.Message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorResultAsync(context, StatusCodes.Status500InternalServerError,
                "GENERAL_INTERNAL_ERROR", ErrorType.Internal, null).ConfigureAwait(false);
        }
    }

    private static string GetCorrelationId(HttpContext ctx) =>
        ctx.Items[CorrelationIdMiddleware.ItemKey]?.ToString() ?? Guid.NewGuid().ToString();

    /// <summary>
    /// Writes a unified error response matching the <see cref="Result{T}"/> shape,
    /// so clients always see the same JSON structure regardless of whether
    /// the error came from a handler or the middleware.
    /// </summary>
    private static async Task WriteErrorResultAsync(
        HttpContext ctx, int statusCode, string code, ErrorType type, string? fallbackMessage)
    {
        var l = ctx.RequestServices.GetService<ILocalizationService>();
        var msg = l?.GetLocalizedMessage(code);

        var error = new Error(
            code,
            msg?.Ar ?? fallbackMessage ?? "خطأ",
            msg?.En ?? fallbackMessage ?? "Error",
            type);

        var envelope = new { isSuccess = false, data = (object?)null, error };

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, envelope, JsonOptions)
            .ConfigureAwait(false);
    }

    private static async Task WriteValidationResultAsync(HttpContext ctx, ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var l = ctx.RequestServices.GetService<ILocalizationService>();
        var msg = l?.GetLocalizedMessage("GENERAL_VALIDATION_ERROR");

        var error = new Error(
            "GENERAL_VALIDATION_ERROR",
            msg?.Ar ?? "عذرًا، البيانات المدخلة غير صحيحة",
            msg?.En ?? "Sorry, the entered data is invalid",
            ErrorType.Validation,
            errors);

        var envelope = new { isSuccess = false, data = (object?)null, error };

        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, envelope, JsonOptions)
            .ConfigureAwait(false);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
