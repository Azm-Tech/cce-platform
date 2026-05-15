# Error Codes Implementation Plan

## How to Adopt in Another Solution

1. Replace all `[YourAppName]` occurrences with your root namespace.
2. Copy each file into the matching layer (Domain / Application / API).
3. Register the middleware in your `Program.cs` pipeline **before** routing and auth.
4. Keep `ApplicationErrors` constants in sync with your YAML localization keys.

---

## Overview

This plan implements a standardized, bilingual, typed error system that maps domain errors to proper HTTP status codes without throwing exceptions for expected failures.

**Packages required:** None (pure .NET). Optional: `FluentValidation` for validation pipeline.

---

### 1. Create the `ErrorType` Enum and `Error` Record (Domain Layer)

**File:** `Domain/Common/Error.cs`

```csharp
using System.Text.Json.Serialization;

namespace [YourAppName].Domain.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    BusinessRule,
    Internal
}

public sealed record Error(
    string Code,
    string MessageAr,
    string MessageEn,
    ErrorType Type = ErrorType.Internal,
    IDictionary<string, string[]>? Details = null);
```

---

### 2. Create the `Result<T>` Wrapper (Application Layer)

**File:** `Application/Contracts/Result.cs`

```csharp
using MediatR;

namespace [YourAppName].Application.Contracts;

public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public [YourAppName].Domain.Common.Error? Error { get; init; }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure([YourAppName].Domain.Common.Error error) => new() { IsSuccess = false, Error = error };

    public static implicit operator Result<T>(T data) => Success(data);
}

public static class Result
{
    public static Result<Unit> Success() => Result<Unit>.Success(Unit.Value);
    public static Result<Unit> Failure([YourAppName].Domain.Common.Error error) => Result<Unit>.Failure(error);
}
```

---

### 3. Define Application Error Constants (Application Layer)

**File:** `Application/Errors/ApplicationErrors.cs`

```csharp
namespace [YourAppName].Application.Errors;

public static class ApplicationErrors
{
    public static class Auth
    {
        public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
        public const string INVALID_TOKEN = "INVALID_TOKEN";
        public const string INVALID_REFRESH_TOKEN = "INVALID_REFRESH_TOKEN";
        public const string ACCOUNT_DEACTIVATED = "ACCOUNT_DEACTIVATED";
        public const string NOT_AUTHENTICATED = "NOT_AUTHENTICATED";
        public const string LOGIN_SUCCESS = "LOGIN_SUCCESS";
        public const string REGISTER_SUCCESS = "REGISTER_SUCCESS";
        public const string LOGOUT_SUCCESS = "LOGOUT_SUCCESS";
        public const string TOKEN_REFRESHED = "TOKEN_REFRESHED";
    }

    public static class User
    {
        public const string NOT_FOUND = "USER_NOT_FOUND";
        public const string EMAIL_EXISTS = "EMAIL_EXISTS";
        public const string USERNAME_EXISTS = "USERNAME_EXISTS";
        public const string CREATED = "USER_CREATED";
        public const string UPDATED = "USER_UPDATED";
        public const string DELETED = "USER_DELETED";
        public const string ACTIVATED = "USER_ACTIVATED";
        public const string DEACTIVATED = "USER_DEACTIVATED";
        public const string ROLES_ASSIGNED = "ROLES_ASSIGNED";
        public const string CREATION_FAILED = "USER_CREATION_FAILED";
        public const string UPDATE_FAILED = "USER_UPDATE_FAILED";
        public const string DELETE_FAILED = "USER_DELETE_FAILED";
        public const string ACTIVATE_FAILED = "ACTIVATE_FAILED";
        public const string DEACTIVATE_FAILED = "DEACTIVATE_FAILED";
        public const string REMOVE_ROLES_FAILED = "REMOVE_ROLES_FAILED";
        public const string ADD_ROLES_FAILED = "ADD_ROLES_FAILED";
    }

    public static class Content
    {
        public const string NOT_FOUND = "CONTENT_NOT_FOUND";
        public const string ALREADY_EXISTS = "CONTENT_EXISTS";
        public const string CREATED = "CONTENT_CREATED";
        public const string UPDATED = "CONTENT_UPDATED";
        public const string DELETED = "CONTENT_DELETED";
        public const string PUBLISHED = "CONTENT_PUBLISHED";
        public const string ARCHIVED = "CONTENT_ARCHIVED";
    }

    public static class Notification
    {
        public const string NOT_FOUND = "NOTIFICATION_NOT_FOUND";
        public const string ACCESS_DENIED = "ACCESS_DENIED";
        public const string CREATED = "NOTIFICATION_CREATED";
        public const string MARKED_READ = "NOTIFICATION_MARKED_READ";
        public const string DELETED = "NOTIFICATION_DELETED";
    }

    public static class PlatformSetting
    {
        public const string NOT_FOUND = "SETTING_NOT_FOUND";
        public const string ALREADY_EXISTS = "SETTING_EXISTS";
        public const string CREATED = "SETTING_CREATED";
        public const string UPDATED = "SETTING_UPDATED";
        public const string DELETED = "SETTING_DELETED";
        public const string REPROTECT_FAILED = "SETTING_REPROTECT_FAILED";
    }

    public static class ExternalApi
    {
        public const string NOT_CONFIGURED = "EXTERNAL_API_NOT_CONFIGURED";
        public const string ERROR = "EXTERNAL_API_ERROR";
        public const string NOT_FOUND = "EXTERNAL_API_CONFIG_NOT_FOUND";
        public const string ALREADY_EXISTS = "EXTERNAL_API_CONFIG_EXISTS";
    }

    public static class General
    {
        public const string VALIDATION_ERROR = "VALIDATION_ERROR";
        public const string INTERNAL_ERROR = "INTERNAL_ERROR";
        public const string UNAUTHORIZED = "UNAUTHORIZED_ACCESS";
        public const string FORBIDDEN = "FORBIDDEN_ACCESS";
        public const string BAD_REQUEST = "BAD_REQUEST";
        public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";
        public const string SUCCESS_CREATED = "SUCCESS_CREATED";
        public const string SUCCESS_UPDATED = "SUCCESS_UPDATED";
        public const string SUCCESS_DELETED = "SUCCESS_DELETED";
        public const string SUCCESS_OPERATION = "SUCCESS_OPERATION";
    }

    public static class Validation
    {
        public const string REQUIRED_FIELD = "REQUIRED_FIELD";
        public const string INVALID_EMAIL = "INVALID_EMAIL";
        public const string INVALID_PHONE = "INVALID_PHONE";
        public const string MIN_LENGTH = "MIN_LENGTH";
        public const string MAX_LENGTH = "MAX_LENGTH";
        public const string INVALID_FORMAT = "INVALID_FORMAT";
        public const string EMAIL_REQUIRED = "EMAIL_REQUIRED";
        public const string PASSWORD_REQUIRED = "PASSWORD_REQUIRED";
        public const string USERNAME_REQUIRED = "USERNAME_REQUIRED";
        public const string FIRST_NAME_REQUIRED = "FIRST_NAME_REQUIRED";
        public const string LAST_NAME_REQUIRED = "LAST_NAME_REQUIRED";
        public const string TOKEN_REQUIRED = "TOKEN_REQUIRED";
        public const string TITLE_REQUIRED = "TITLE_REQUIRED";
        public const string TITLE_MAX_LENGTH = "TITLE_MAX_LENGTH";
        public const string BODY_REQUIRED = "BODY_REQUIRED";
        public const string SUMMARY_MAX_LENGTH = "SUMMARY_MAX_LENGTH";
        public const string CONTENT_TYPE_REQUIRED = "CONTENT_TYPE_REQUIRED";
        public const string CONTENT_TYPE_MAX_LENGTH = "CONTENT_TYPE_MAX_LENGTH";
        public const string AUTHOR_ID_REQUIRED = "AUTHOR_ID_REQUIRED";
        public const string STATUS_REQUIRED = "STATUS_REQUIRED";
        public const string STATUS_INVALID = "STATUS_INVALID";
        public const string FEATURED_IMAGE_URL_MAX_LENGTH = "FEATURED_IMAGE_URL_MAX_LENGTH";
        public const string CATEGORY_MAX_LENGTH = "CATEGORY_MAX_LENGTH";
        public const string USER_ID_REQUIRED = "USER_ID_REQUIRED";
        public const string MESSAGE_REQUIRED = "MESSAGE_REQUIRED";
        public const string MESSAGE_MAX_LENGTH = "MESSAGE_MAX_LENGTH";
        public const string NOTIFICATION_TYPE_REQUIRED = "NOTIFICATION_TYPE_REQUIRED";
        public const string NOTIFICATION_TYPE_MAX_LENGTH = "NOTIFICATION_TYPE_MAX_LENGTH";
        public const string CHANNEL_REQUIRED = "CHANNEL_REQUIRED";
        public const string CHANNEL_INVALID = "CHANNEL_INVALID";
        public const string KEY_REQUIRED = "KEY_REQUIRED";
        public const string KEY_MAX_LENGTH = "KEY_MAX_LENGTH";
        public const string VALUE_REQUIRED = "VALUE_REQUIRED";
        public const string VALUE_MAX_LENGTH = "VALUE_MAX_LENGTH";
        public const string PASSWORD_UPPERCASE = "PASSWORD_UPPERCASE";
        public const string PASSWORD_LOWERCASE = "PASSWORD_LOWERCASE";
        public const string PASSWORD_NUMBER = "PASSWORD_NUMBER";
    }
}
```

---

### 4. Create `ResultActionResultExtensions` (API Layer)

**File:** `API/Extensions/ResultActionResultExtensions.cs`

```csharp
using [YourAppName].Application.Contracts;
using [YourAppName].Domain.Common;

using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace [YourAppName].API.Extensions;

public static class ResultActionResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this ControllerBase controller,
        Result<T> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            if (typeof(T) == typeof(Unit) && successStatusCode == StatusCodes.Status204NoContent)
            {
                return controller.NoContent();
            }

            return successStatusCode switch
            {
                StatusCodes.Status201Created => controller.StatusCode(StatusCodes.Status201Created, result),
                StatusCodes.Status204NoContent => controller.NoContent(),
                _ => controller.StatusCode(successStatusCode, result)
            };
        }

        return controller.StatusCode(MapFailureStatusCode(result.Error), result);
    }

    private static int MapFailureStatusCode(Error? error) => error?.Type switch
    {
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status400BadRequest
    };
}
```

---

### 5. Create `ExceptionHandlingMiddleware` (API Layer)

**File:** `API/Middleware/ExceptionHandlingMiddleware.cs`

```csharp
using [YourAppName].Application.Errors;
using [YourAppName].Application.Localization;
using [YourAppName].Domain.Common;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace [YourAppName].API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ILocalizationService localizationService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, localizationService);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, ILocalizationService localizationService)
    {
        var (statusCode, error) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                BuildValidationError(localizationService, validationEx)),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                BuildError(localizationService, ApplicationErrors.General.UNAUTHORIZED, ErrorType.Unauthorized)),
            ArgumentException => (
                HttpStatusCode.BadRequest,
                BuildError(localizationService, ApplicationErrors.General.BAD_REQUEST, ErrorType.Validation)),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                BuildError(localizationService, ApplicationErrors.General.RESOURCE_NOT_FOUND, ErrorType.NotFound)),
            _ => (
                HttpStatusCode.InternalServerError,
                BuildError(localizationService, ApplicationErrors.General.INTERNAL_ERROR, ErrorType.Internal))
        };

        _logger.LogError(exception, "Error handling request: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            isSuccess = false,
            data = (object?)null,
            error
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static Error BuildError(ILocalizationService localizationService, string key, ErrorType type)
    {
        var localized = localizationService.GetLocalizedMessage(key);
        return new Error(key, localized.Ar, localized.En, type);
    }

    private static Error BuildValidationError(ILocalizationService localizationService, ValidationException validationEx)
    {
        var localized = localizationService.GetLocalizedMessage(ApplicationErrors.General.VALIDATION_ERROR);
        var details = validationEx.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new Error(
            ApplicationErrors.General.VALIDATION_ERROR,
            localized.Ar,
            localized.En,
            ErrorType.Validation,
            details);
    }
}
```

---

### 6. Wire Middleware into the Pipeline (API Layer)

**File:** `API/Extensions/WebApplicationExtensions.cs` (or directly in `Program.cs`)

```csharp
using [YourAppName].API.Middleware;

namespace [YourAppName].API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UsePlatformPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseRateLimiter();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
```

> **Important:** `ExceptionHandlingMiddleware` must be the **first** middleware in the pipeline so it wraps all subsequent request processing.

---

### 7. Handler Usage Pattern (Application Layer)

In every command/query handler, return `Result<T>.Failure(...)` instead of throwing exceptions for expected failures.

```csharp
public async Task<Result<CreateSuccessDto>> Handle(CreateUserCommand request, CancellationToken ct)
{
    var exists = await _repository.ExistsAsync(c => c.Email == request.Email, ct);
    if (exists)
        return Result<CreateSuccessDto>.Failure(new Error(
            ApplicationErrors.User.EMAIL_EXISTS,
            "...", "...", ErrorType.Conflict));

    var user = User.Create(request.Email, request.Username, ...);
    await _repository.AddAsync(user, ct);
    await _unitOfWork.SaveChangesAsync(ct);

    return Result<CreateSuccessDto>.Success(new CreateSuccessDto(user.Id));
}
```

---

### 8. Controller Usage Pattern (API Layer)

```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateRequest request, CancellationToken ct)
{
    var result = await _mediator.Send(new CreateCommand(...), ct);
    return this.ToActionResult(result, StatusCodes.Status201Created);
}
```

---

## HTTP Status Code Mapping Reference

| `ErrorType`        | HTTP Status Code |
|--------------------|------------------|
| `Forbidden`        | 403              |
| `Unauthorized`     | 401              |
| `NotFound`         | 404              |
| `Conflict`         | 409              |
| `Validation`       | 422              |
| `BusinessRule`     | 400              |
| `Internal`         | 400 (default)    |
| `None`             | 400 (default)    |
