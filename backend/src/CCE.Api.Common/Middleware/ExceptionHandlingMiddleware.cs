using CCE.Api.Common.Results;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
        catch (OperationCanceledException)
        {
            // Client disconnected — not a server error.
        }
        catch (ValidationException ex)
        {
            await WriteValidationResultAsync(context, ex).ConfigureAwait(false);
        }
        catch (ConcurrencyException ex)
        {
            await EnvelopeWriter.WriteAsync(context, StatusCodes.Status409Conflict,
                MessageKeys.General.CONCURRENCY_CONFLICT, ex.Message).ConfigureAwait(false);
        }
        catch (DuplicateException ex)
        {
            await EnvelopeWriter.WriteAsync(context, StatusCodes.Status409Conflict,
                MessageKeys.General.DUPLICATE_VALUE, ex.Message).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            await EnvelopeWriter.WriteAsync(context, StatusCodes.Status422UnprocessableEntity,
                MessageKeys.General.BUSINESS_RULE_VIOLATION, ex.Message).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogInformation(ex, "Unauthorized access");
            await EnvelopeWriter.WriteAsync(context, StatusCodes.Status401Unauthorized,
                MessageKeys.General.UNAUTHORIZED).ConfigureAwait(false);
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            await EnvelopeWriter.WriteAsync(context, StatusCodes.Status404NotFound,
                MessageKeys.General.RESOURCE_NOT_FOUND_GENERIC, ex.Message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await EnvelopeWriter.WriteAsync(context, StatusCodes.Status500InternalServerError,
                MessageKeys.General.INTERNAL_ERROR).ConfigureAwait(false);
        }
    }

    private static async Task WriteValidationResultAsync(HttpContext ctx, ValidationException ex)
    {
        var l = ctx.RequestServices.GetService<ILocalizationService>();
        var config = ctx.RequestServices.GetService<IConfiguration>();
        var supported = config?.GetSection("Localization:Supported").Get<string[]>();
        var defaultLocale = config?.GetValue<string>("Localization:Default");
        var locale = LocalizationMiddleware.PickLocale(
            ctx.Request.Headers.AcceptLanguage.ToString(), supported, defaultLocale);

        var fieldErrors = ex.Errors.Select(e =>
        {
            var domainKey = e.ErrorCode;
            var valCode = SystemCodeMap.ToSystemCode(domainKey);
            var valMsg = l?.GetString(domainKey, locale) ?? domainKey;
            if (valMsg == domainKey) valMsg = e.ErrorMessage;
            return new
            {
                field = JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName ?? string.Empty),
                code = valCode,
                message = valMsg
            };
        }).ToList<object>();

        await EnvelopeWriter.WriteAsync(ctx, StatusCodes.Status400BadRequest,
            MessageKeys.General.VALIDATION_ERROR, errors: fieldErrors).ConfigureAwait(false);
    }
}
