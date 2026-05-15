# Scalar & Swagger for .NET 10 Implementation Plan

## How to Adopt in Another Solution

1. Replace all `[YourAppName]` occurrences with your root namespace.
2. Add the required NuGet packages (see Step 1).
3. Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in your API `.csproj`.
4. Copy `ApiDocumentationExtensions.cs` into your API project.
5. Call `AddPlatformOpenApi()` and `AddPlatformApiVersioning()` in `Program.cs` during service registration.
6. Call `UsePlatformApiDocumentation()` in `Program.cs` during pipeline configuration.
7. Add XML `///` comments to all public controllers and action methods.

---

## Overview

This plan configures modern API documentation for .NET 10 using:
- **Microsoft.AspNetCore.OpenApi** (built-in .NET 10 OpenAPI support)
- **Scalar.AspNetCore** (modern interactive API client)
- **Swashbuckle.AspNetCore** (legacy SwaggerUI for backward compatibility)
- **Asp.Versioning** (API versioning support)

All documentation endpoints (`/openapi/v1.json`, `/scalar`, `/swagger`) are exposed **only in Development**.

---

### 1. Add Required NuGet Packages

Add to your central package management (`Directory.Packages.props`) or `.csproj`:

```xml
<PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
<PackageVersion Include="Swashbuckle.AspNetCore" Version="10.1.7" />
<PackageVersion Include="Scalar.AspNetCore" Version="2.3.0" />
<PackageVersion Include="Asp.Versioning.Mvc.ApiExplorer" Version="10.0.0-preview.1" />
```

Then reference them in your API `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
  <PackageReference Include="Swashbuckle.AspNetCore" />
  <PackageReference Include="Scalar.AspNetCore" />
  <PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" />
</ItemGroup>
```

---

### 2. Enable XML Documentation (API `.csproj`)

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

> `1591` suppresses warnings for missing XML comments on public members. Remove the suppression if you want enforcement.

---

### 3. Create `ApiDocumentationExtensions` (API Layer)

**File:** `API/Extensions/ApiDocumentationExtensions.cs`

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace [YourAppName].API.Extensions;

public static class ApiDocumentationExtensions
{
    private const string ApiVersion = "v1";

    public static IServiceCollection AddPlatformOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(ApiVersion, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "[YourAppName] API v1",
                    Version = ApiVersion,
                    Description = "Your application API - Clean Architecture",
                    Contact = new Microsoft.OpenApi.OpenApiContact
                    {
                        Name = "Your Team",
                        Email = "support@yourapp.com"
                    }
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your JWT token"
                };

                document.Security ??= new List<OpenApiSecurityRequirement>();
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] = new List<string>()
                });

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, _, _) =>
            {
                var parameters = operation.Parameters?.ToList() ?? new List<IOpenApiParameter>();
                parameters.Add(new OpenApiParameter
                {
                    Name = "Accept-Language",
                    In = ParameterLocation.Header,
                    Description = "Language preference (ar, en). Default: ar",
                    Required = false,
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                });
                operation.Parameters = parameters;
                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static IServiceCollection AddPlatformApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    public static WebApplication UsePlatformApiDocumentation(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("[YourAppName] API");
            options.AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme);
            options.AddHttpAuthentication(JwtBearerDefaults.AuthenticationScheme, _ => { });
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/openapi/{ApiVersion}.json", "[YourAppName] API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "[YourAppName] API Documentation";
            options.DefaultModelsExpandDepth(2);
            options.EnableDeepLinking();
            options.EnablePersistAuthorization();
        });

        return app;
    }
}
```

---

### 4. Wire into `Program.cs` (API Layer)

**File:** `API/Program.cs`

```csharp
using [YourAppName].API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ... logging, auth, persistence, etc.

builder.Services
    .AddPlatformOpenApi()
    .AddPlatformApiVersioning()
    .AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UsePlatformApiDocumentation();
app.MapControllers();

app.Run();

public partial class Program;
```

> **Note:** `UsePlatformApiDocumentation()` is safe to call unconditionally — it internally checks `app.Environment.IsDevelopment()`.

---

### 5. Controller Annotation Pattern (API Layer)

Add XML `///` summaries and `ProducesResponseType` attributes to every controller action.

**File example:** `API/Controllers/AuthController.cs`

```csharp
using [YourAppName].Application.Contracts;
using [YourAppName].API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

namespace [YourAppName].API.Controllers;

/// <summary>
/// Provides authentication endpoints for login, registration, token refresh, and logout.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns JWT access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Login attempt received");
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<CreateSuccessDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Registration attempt received");
        var result = await _mediator.Send(new RegisterCommand(...), ct);
        return this.ToActionResult(result, StatusCodes.Status201Created);
    }
}
```

---

## Endpoint URLs Reference

| Environment | URL | Description |
|-------------|-----|-------------|
| Development | `http://localhost:5000/openapi/v1.json` | Raw OpenAPI JSON spec |
| Development | `http://localhost:5000/scalar` | Scalar interactive UI |
| Development | `http://localhost:5000/swagger` | SwaggerUI legacy view |

> All three are automatically hidden in non-Development environments.

---

## Versioning Behavior Reference

| Setting | Value | Behavior |
|---------|-------|----------|
| `DefaultApiVersion` | `1.0` | Requests without version default to v1 |
| `AssumeDefaultVersionWhenUnspecified` | `true` | Unversioned requests are allowed |
| `ReportApiVersions` | `true` | Response headers include `api-supported-versions` |
| `GroupNameFormat` | `'v'VVV` | Explorer groups names like `v1`, `v2` |
| `SubstituteApiVersionInUrl` | `true` | URL route tokens `{version:apiVersion}` are replaced |

---

## Security Scheme Reference

| Property | Value |
|----------|-------|
| Type | `Http` |
| Scheme | `bearer` |
| Bearer Format | `JWT` |
| Global Security Requirement | Applied to all operations |
| Scalar Integration | `AddPreferredSecuritySchemes("Bearer")` |

---

## Optional: Add API Version to Route

If you want versioned routes, use the `api-version` route constraint:

```csharp
[Route("api/v{version:apiVersion}/[controller]")]
```

Combine with `SubstituteApiVersionInUrl = true` in the API explorer options for clean Swagger/Scalar route display.
