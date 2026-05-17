using CCE.Application.Common;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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
        catch (ConcurrencyException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict,
                "CONCURRENCY_CONFLICT", MessageType.Conflict, ex.Message).ConfigureAwait(false);
        }
        catch (DuplicateException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict,
                "DUPLICATE_VALUE", MessageType.Conflict, ex.Message).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest,
                "BAD_REQUEST", MessageType.BusinessRule, ex.Message).ConfigureAwait(false);
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound,
                "RESOURCE_NOT_FOUND_GENERIC", MessageType.NotFound, ex.Message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR", MessageType.Internal, null).ConfigureAwait(false);
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext ctx, int statusCode, string domainKey, MessageType type, string? fallbackMessage)
    {
        var l = ctx.RequestServices.GetService<ILocalizationService>();
        var msg = l?.GetString(domainKey) ?? fallbackMessage ?? "خطأ";
        var code = SystemCodeMap.ToSystemCode(domainKey);

        var envelope = new
        {
            success = false,
            code,
            message = msg,
            data = (object?)null,
            errors = Array.Empty<object>(),
            traceId = Activity.Current?.Id ?? ctx.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow,
        };

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, envelope, JsonOptions)
            .ConfigureAwait(false);
    }

    private static async Task WriteValidationResultAsync(HttpContext ctx, ValidationException ex)
    {
        var l = ctx.RequestServices.GetService<ILocalizationService>();
        var headerMsg = l?.GetString("VALIDATION_ERROR") ?? "عذرًا، البيانات المدخلة غير صحيحة";
        var headerCode = SystemCodeMap.ToSystemCode("VALIDATION_ERROR");

        var fieldErrors = ex.Errors.Select(e =>
        {
            var domainKey = e.ErrorMessage;
            var valCode = SystemCodeMap.ToSystemCode(domainKey);
            var valMsg = l?.GetString(domainKey) ?? domainKey;
            return new
            {
                field = ToCamelCase(e.PropertyName),
                code = valCode,
                message = valMsg
            };
        }).ToList();

        var envelope = new
        {
            success = false,
            code = headerCode,
            message = headerMsg,
            data = (object?)null,
            errors = fieldErrors,
            traceId = Activity.Current?.Id ?? ctx.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow,
        };

        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, envelope, JsonOptions)
            .ConfigureAwait(false);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
