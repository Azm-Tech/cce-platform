# Refit HTTP Client Implementation Plan

## How to Adopt in Another Solution

1. Replace all `[YourAppName]` occurrences with your root namespace.
2. Install the required NuGet packages (`Refit`, `Refit.HttpClientFactory`, `Microsoft.Extensions.Http.Resilience`).
3. Create the `ExternalApiClientAttribute` and apply it to your Refit interfaces.
4. Implement `IExternalApiConfigurationProvider` or use the database-backed provider included here.
5. Register `AddExternalApiServices()` in your Infrastructure DI module.
6. Seed at least one `ExternalApiConfiguration` row in your database (or implement a static config provider).
7. Inject the generated Refit client interfaces into handlers/controllers.

---

## Overview

This plan implements a **dynamic, database-driven Refit HTTP client factory** that:
- Discovers Refit client interfaces at startup via reflection and a custom `[ExternalApiClient]` attribute.
- Reads base URLs, timeouts, and auth settings from a runtime configuration provider.
- Supports multiple auth schemes: `None`, `ApiKey`, `Bearer`, `Basic`, `OAuth2`.
- Adds standard resilience (retry, timeout, circuit breaker) via `Microsoft.Extensions.Http.Resilience`.
- Allows hot-reload of external API configs from the database without restarting the app.

**Packages required:**
- `Refit` (v8.0.0+)
- `Refit.HttpClientFactory`
- `Microsoft.Extensions.Http.Resilience`

---

### 1. Add NuGet Packages

**File:** `Directory.Packages.props` (or `.csproj`)

```xml
<PackageVersion Include="Refit" Version="8.0.0" />
<PackageVersion Include="Refit.HttpClientFactory" Version="8.0.0" />
<PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="9.9.0" />
```

**File:** `Infrastructure.csproj` and `Application.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Refit" />
  <PackageReference Include="Refit.HttpClientFactory" />
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
</ItemGroup>
```

> **Note:** `Refit` is needed in the Application layer for the interface attributes (`[Get]`, `[Post]`, `[Query]`, etc.).

---

### 2. Create `ExternalApiClientAttribute` (Application Layer)

**File:** `Application/ExternalApis/ExternalApiClientAttribute.cs`

```csharp
namespace [YourAppName].Application.ExternalApis;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class ExternalApiClientAttribute : Attribute
{
    public string ApiName { get; }

    public ExternalApiClientAttribute(string apiName)
    {
        ApiName = apiName;
    }
}
```

> **Purpose:** Marks a Refit interface so the DI scanner knows which API name to look up in the configuration provider.

---

### 3. Create Configuration DTOs (Application Layer)

**File:** `Application/ExternalApis/DTOs/ExternalApiConfig.cs`

```csharp
namespace [YourAppName].Application.ExternalApis.DTOs;

public class ExternalApiConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public ExternalApiAuthConfig Auth { get; set; } = new();
    public int TimeoutSeconds { get; set; } = 30;
}

public class ExternalApiAuthConfig
{
    public ExternalApiAuthType Type { get; set; } = ExternalApiAuthType.None;

    // ApiKey settings
    public string KeyName { get; set; } = string.Empty;
    public string KeyLocation { get; set; } = "Header";
    public string Value { get; set; } = string.Empty;

    // Bearer token settings
    public string Token { get; set; } = string.Empty;

    // OAuth2 settings
    public string TokenUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool AutoRefresh { get; set; } = true;
}

public enum ExternalApiAuthType
{
    None,
    ApiKey,
    Bearer,
    Basic,
    OAuth2
}
```

---

### 4. Create `IExternalApiConfigurationProvider` (Application Layer)

**File:** `Application/Interfaces/IExternalApiConfigurationProvider.cs`

```csharp
using [YourAppName].Application.ExternalApis.DTOs;

namespace [YourAppName].Application.Interfaces;

public interface IExternalApiConfigurationProvider
{
    ExternalApiConfig? GetConfig(string apiName);
    IReadOnlyList<ExternalApiConfig> GetAllConfigs();
    Task ReloadAsync(CancellationToken ct = default);
}
```

> **Note:** The provider is registered as a **Singleton** so Refit clients can resolve it inside `ConfigureHttpClient` and `AddHttpMessageHandler`.

---

### 5. Create `ExternalApiConfiguration` Entity (Domain Layer)

**File:** `Domain/Entities/ExternalApis/ExternalApiConfiguration.cs`

```csharp
using [YourAppName].Domain.Entities;

namespace [YourAppName].Domain.Entities.ExternalApis;

public class ExternalApiConfiguration : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string BaseUrl { get; private set; } = string.Empty;
    public int TimeoutSeconds { get; private set; } = 30;
    public bool IsEnabled { get; private set; } = true;

    public string AuthType { get; private set; } = "None";
    public string? AuthKeyName { get; private set; }
    public string? AuthKeyLocation { get; private set; }
    public string? AuthValue { get; private set; }
    public string? AuthToken { get; private set; }
    public string? AuthTokenUrl { get; private set; }
    public string? AuthClientId { get; private set; }
    public string? AuthClientSecret { get; private set; }
    public string? AuthScope { get; private set; }
    public bool AuthAutoRefresh { get; private set; }

    public static ExternalApiConfiguration Create(
        string name,
        string baseUrl,
        int timeoutSeconds,
        string authType,
        string? authKeyName = null,
        string? authKeyLocation = null,
        string? authValue = null,
        string? authToken = null,
        string? authTokenUrl = null,
        string? authClientId = null,
        string? authClientSecret = null,
        string? authScope = null,
        bool authAutoRefresh = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL is required", nameof(baseUrl));
        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        return new ExternalApiConfiguration
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            BaseUrl = baseUrl.Trim(),
            TimeoutSeconds = timeoutSeconds,
            IsEnabled = true,
            AuthType = authType,
            AuthKeyName = authKeyName?.Trim(),
            AuthKeyLocation = authKeyLocation?.Trim(),
            AuthValue = authValue,
            AuthToken = authToken,
            AuthTokenUrl = authTokenUrl?.Trim(),
            AuthClientId = authClientId,
            AuthClientSecret = authClientSecret,
            AuthScope = authScope?.Trim(),
            AuthAutoRefresh = authAutoRefresh,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateConfig(string baseUrl, int timeoutSeconds)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL is required", nameof(baseUrl));
        if (timeoutSeconds <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(timeoutSeconds));

        BaseUrl = baseUrl.Trim();
        TimeoutSeconds = timeoutSeconds;
        MarkUpdated();
    }

    public void UpdateAuth(
        string authType,
        string? authKeyName = null,
        string? authKeyLocation = null,
        string? authValue = null,
        string? authToken = null,
        string? authTokenUrl = null,
        string? authClientId = null,
        string? authClientSecret = null,
        string? authScope = null,
        bool authAutoRefresh = true)
    {
        AuthType = authType;
        AuthKeyName = authKeyName?.Trim();
        AuthKeyLocation = authKeyLocation?.Trim();
        AuthValue = authValue;
        AuthToken = authToken;
        AuthTokenUrl = authTokenUrl?.Trim();
        AuthClientId = authClientId;
        AuthClientSecret = authClientSecret;
        AuthScope = authScope?.Trim();
        AuthAutoRefresh = authAutoRefresh;
        MarkUpdated();
    }

    public void Enable()
    {
        if (!IsEnabled)
        {
            IsEnabled = true;
            MarkUpdated();
        }
    }

    public void Disable()
    {
        if (IsEnabled)
        {
            IsEnabled = false;
            MarkUpdated();
        }
    }
}
```

---

### 6. Create `DatabaseExternalApiProvider` (Infrastructure Layer)

**File:** `Infrastructure/ExternalApis/Providers/DatabaseExternalApiProvider.cs`

```csharp
using System.Collections.Concurrent;
using [YourAppName].Application.ExternalApis.DTOs;
using [YourAppName].Application.Interfaces;
using [YourAppName].Domain.Entities.ExternalApis;
using [YourAppName].Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace [YourAppName].Infrastructure.ExternalApis.Providers;

public class DatabaseExternalApiProvider : IExternalApiConfigurationProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseExternalApiProvider> _logger;
    private ConcurrentDictionary<string, ExternalApiConfig> _configs = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public DatabaseExternalApiProvider(IServiceScopeFactory scopeFactory, ILogger<DatabaseExternalApiProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public ExternalApiConfig? GetConfig(string apiName)
    {
        if (!_loaded)
        {
            _logger.LogWarning("External API configs not yet loaded, requesting sync load");
            LoadSync();
        }

        _configs.TryGetValue(apiName, out var config);
        return config;
    }

    public IReadOnlyList<ExternalApiConfig> GetAllConfigs()
    {
        if (!_loaded)
            LoadSync();

        return _configs.Values.ToList().AsReadOnly();
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<ExternalApiConfiguration>>();
        var secretProtector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();

        var entities = await repository.Query(e => e.IsEnabled && !e.IsDeleted, true).ToListAsync(ct);

        var newConfigs = new ConcurrentDictionary<string, ExternalApiConfig>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in entities)
        {
            try
            {
                newConfigs[entity.Name] = MapToConfig(entity, secretProtector);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map config for {ApiName}", entity.Name);
            }
        }

        _configs = newConfigs;
        _loaded = true;
        _logger.LogInformation("Reloaded {Count} external API configurations from database", _configs.Count);
    }

    public void LoadSync()
    {
        try
        {
            ReloadAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load external API configs synchronously");
            _loaded = true;
        }
    }

    private static ExternalApiConfig MapToConfig(ExternalApiConfiguration entity, ISecretProtector secretProtector)
    {
        var config = new ExternalApiConfig
        {
            BaseUrl = entity.BaseUrl,
            TimeoutSeconds = entity.TimeoutSeconds,
            Auth = new ExternalApiAuthConfig
            {
                Type = Enum.TryParse<ExternalApiAuthType>(entity.AuthType, out var authType) ? authType : ExternalApiAuthType.None,
                KeyName = entity.AuthKeyName ?? string.Empty,
                KeyLocation = entity.AuthKeyLocation ?? "Header",
                Value = Decrypt(entity.AuthValue, secretProtector),
                Token = Decrypt(entity.AuthToken, secretProtector),
                TokenUrl = entity.AuthTokenUrl ?? string.Empty,
                ClientId = Decrypt(entity.AuthClientId, secretProtector),
                ClientSecret = Decrypt(entity.AuthClientSecret, secretProtector),
                Scope = entity.AuthScope ?? string.Empty,
                AutoRefresh = entity.AuthAutoRefresh
            }
        };

        return config;
    }

    private static string Decrypt(string? encrypted, ISecretProtector secretProtector)
    {
        if (string.IsNullOrEmpty(encrypted))
            return string.Empty;

        try
        {
            return secretProtector.Unprotect(encrypted);
        }
        catch
        {
            return string.Empty;
        }
    }
}
```

> **Note:** `ISecretProtector` is an abstraction over ASP.NET Core Data Protection. Replace it with your own secret handling or remove `Decrypt` calls if you store secrets in plaintext (not recommended).

---

### 7. Create Authentication Handlers (Infrastructure Layer)

#### 7a. No-Op Handler (fallback)

**File:** `Infrastructure/ExternalApis/Authentication/NoOpDelegatingHandler.cs`

```csharp
namespace [YourAppName].Infrastructure.ExternalApis.Authentication;

public class NoOpDelegatingHandler : DelegatingHandler
{
}
```

#### 7b. API Key Handler

**File:** `Infrastructure/ExternalApis/Authentication/ApiKeyAuthHandler.cs`

```csharp
using System.Net.Http.Headers;
using [YourAppName].Application.ExternalApis.DTOs;

namespace [YourAppName].Infrastructure.ExternalApis.Authentication;

public class ApiKeyAuthHandler : DelegatingHandler
{
    private readonly string _keyName;
    private readonly string _keyValue;
    private readonly string _keyLocation;

    public ApiKeyAuthHandler(string keyName, string keyValue, string keyLocation)
    {
        _keyName = keyName;
        _keyValue = keyValue;
        _keyLocation = keyLocation;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_keyLocation.Equals("Query", StringComparison.OrdinalIgnoreCase))
        {
            var uriBuilder = new UriBuilder(request.RequestUri!);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
            query[_keyName] = _keyValue;
            uriBuilder.Query = query.ToString();
            request.RequestUri = uriBuilder.Uri;
        }
        else
        {
            request.Headers.TryAddWithoutValidation(_keyName, _keyValue);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

public static class ApiKeyAuthHandlerFactory
{
    public static DelegatingHandler Create(ExternalApiAuthConfig authConfig)
    {
        return new ApiKeyAuthHandler(
            authConfig.KeyName,
            authConfig.Value,
            authConfig.KeyLocation);
    }
}
```

#### 7c. Bearer Token Handler

**File:** `Infrastructure/ExternalApis/Authentication/BearerTokenAuthHandler.cs`

```csharp
using System.Net.Http.Headers;

namespace [YourAppName].Infrastructure.ExternalApis.Authentication;

public class BearerTokenAuthHandler : DelegatingHandler
{
    private readonly string _token;

    public BearerTokenAuthHandler(string token)
    {
        _token = token;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return base.SendAsync(request, cancellationToken);
    }
}

public static class BearerTokenAuthHandlerFactory
{
    public static DelegatingHandler Create(string token)
    {
        return new BearerTokenAuthHandler(token);
    }
}
```

#### 7d. Basic Auth Handler

**File:** `Infrastructure/ExternalApis/Authentication/BasicAuthHandler.cs`

```csharp
using System.Net.Http.Headers;
using System.Text;

namespace [YourAppName].Infrastructure.ExternalApis.Authentication;

public class BasicAuthHandler : DelegatingHandler
{
    private readonly string _username;
    private readonly string _password;

    public BasicAuthHandler(string username, string password)
    {
        _username = username;
        _password = password;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return base.SendAsync(request, cancellationToken);
    }
}

public static class BasicAuthHandlerFactory
{
    public static DelegatingHandler Create(string username, string password)
    {
        return new BasicAuthHandler(username, password);
    }
}
```

#### 7e. OAuth2 Client Credentials Handler

**File:** `Infrastructure/ExternalApis/Authentication/OAuth2ClientCredentialsHandler.cs`

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace [YourAppName].Infrastructure.ExternalApis.Authentication;

public class OAuth2ClientCredentialsHandler : DelegatingHandler
{
    private readonly string _tokenUrl;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _scope;
    private readonly bool _autoRefresh;
    private readonly ILogger<OAuth2ClientCredentialsHandler> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public OAuth2ClientCredentialsHandler(
        string tokenUrl,
        string clientId,
        string clientSecret,
        string scope,
        bool autoRefresh,
        ILogger<OAuth2ClientCredentialsHandler> logger)
    {
        _tokenUrl = tokenUrl;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _scope = scope;
        _autoRefresh = autoRefresh;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_accessToken) || (_autoRefresh && DateTime.UtcNow >= _tokenExpiry.AddSeconds(-60)))
        {
            await AcquireTokenAsync(cancellationToken);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task AcquireTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = new HttpClient();
            var requestContent = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            };

            if (!string.IsNullOrEmpty(_scope))
            {
                requestContent["scope"] = _scope;
            }

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _tokenUrl)
            {
                Content = new FormUrlEncodedContent(requestContent)
            };

            var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse != null)
            {
                _accessToken = tokenResponse.AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);
                _logger.LogDebug("OAuth2 token acquired, expires at {Expiry}", _tokenExpiry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire OAuth2 token");
            throw;
        }
    }
}

public class OAuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } = 3600;
    public string? Scope { get; set; }
}

public static class OAuth2ClientCredentialsHandlerFactory
{
    public static DelegatingHandler Create(
        string tokenUrl,
        string clientId,
        string clientSecret,
        string scope,
        bool autoRefresh,
        ILoggerFactory loggerFactory)
    {
        return new OAuth2ClientCredentialsHandler(
            tokenUrl,
            clientId,
            clientSecret,
            scope,
            autoRefresh,
            loggerFactory.CreateLogger<OAuth2ClientCredentialsHandler>());
    }
}
```

#### 7f. Auth Handler Factory

**File:** `Infrastructure/ExternalApis/Authentication/ExternalApiAuthHandlerFactory.cs`

```csharp
using [YourAppName].Application.ExternalApis.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace [YourAppName].Infrastructure.ExternalApis.Authentication;

public static class ExternalApiAuthHandlerFactory
{
    public static DelegatingHandler? Create(ExternalApiAuthConfig authConfig, ILoggerFactory? loggerFactory = null)
    {
        if (authConfig == null || authConfig.Type == ExternalApiAuthType.None)
        {
            return null;
        }

        var logger = loggerFactory ?? NullLoggerFactory.Instance;

        return authConfig.Type switch
        {
            ExternalApiAuthType.ApiKey => ApiKeyAuthHandlerFactory.Create(authConfig),
            ExternalApiAuthType.Bearer => BearerTokenAuthHandlerFactory.Create(authConfig.Token),
            ExternalApiAuthType.Basic => BasicAuthHandlerFactory.Create(authConfig.ClientId, authConfig.ClientSecret),
            ExternalApiAuthType.OAuth2 => OAuth2ClientCredentialsHandlerFactory.Create(
                authConfig.TokenUrl,
                authConfig.ClientId,
                authConfig.ClientSecret,
                authConfig.Scope,
                authConfig.AutoRefresh,
                logger),
            _ => null
        };
    }
}
```

---

### 8. Create DI Registration with Reflection Discovery (Infrastructure Layer)

**File:** `Infrastructure/ExternalApis/ExternalApiServiceCollectionExtensions.cs`

```csharp
using System.Reflection;
using [YourAppName].Application.ExternalApis;
using [YourAppName].Application.ExternalApis.DTOs;
using [YourAppName].Application.Interfaces;
using [YourAppName].Infrastructure.ExternalApis.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Refit;

namespace [YourAppName].Infrastructure.ExternalApis;

public static class ExternalApiServiceCollectionExtensions
{
    public static IServiceCollection AddExternalRefitClient<TClient>(
        this IServiceCollection services,
        string apiName,
        ILoggerFactory? loggerFactory = null)
        where TClient : class
    {
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
        };

        var builder = services.AddRefitClient<TClient>(refitSettings)
            .ConfigureHttpClient((sp, client) =>
            {
                var provider = sp.GetRequiredService<IExternalApiConfigurationProvider>();
                var config = provider.GetConfig(apiName);
                if (config != null)
                {
                    client.BaseAddress = new Uri(config.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 30);
                }
            })
            .AddHttpMessageHandler(sp =>
            {
                var provider = sp.GetRequiredService<IExternalApiConfigurationProvider>();
                var config = provider.GetConfig(apiName);
                if (config?.Auth != null && config.Auth.Type != ExternalApiAuthType.None)
                {
                    var handler = ExternalApiAuthHandlerFactory.Create(config.Auth, sp.GetService<ILoggerFactory>());
                    if (handler != null)
                        return handler;
                }

                return new NoOpDelegatingHandler();
            });

        builder.AddStandardResilienceHandler();

        return services;
    }

    public static TClient GetExternalApiClient<TClient>(this IServiceProvider services)
        where TClient : class
    {
        return services.GetRequiredService<TClient>();
    }

    public static IServiceCollection AddExternalApiServices(
        this IServiceCollection services,
        IEnumerable<Assembly>? assemblies = null,
        ILoggerFactory? loggerFactory = null)
    {
        assemblies ??= GetExternalApiAssemblies();

        var clientInterfaces = DiscoverExternalApiClients(assemblies);

        foreach (var (interfaceType, apiName) in clientInterfaces)
        {
            RegisterRefitClient(services, interfaceType, apiName, loggerFactory);
        }

        return services;
    }

    private static IEnumerable<Assembly> GetExternalApiAssemblies()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        return loadedAssemblies.Where(a =>
            a.FullName?.Contains("[YourAppName]") == true &&
            !a.FullName.Contains("test", StringComparison.OrdinalIgnoreCase));
    }

    private static List<(Type interfaceType, string apiName)> DiscoverExternalApiClients(IEnumerable<Assembly> assemblies)
    {
        var clients = new List<(Type, string)>();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsInterface &&
                               t.GetCustomAttribute<ExternalApiClientAttribute>() != null);

                foreach (var type in types)
                {
                    var attr = type.GetCustomAttribute<ExternalApiClientAttribute>();
                    if (attr != null)
                    {
                        clients.Add((type, attr.ApiName));
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
            }
        }

        return clients;
    }

    private static IServiceCollection RegisterRefitClient(
        IServiceCollection services,
        Type clientInterface,
        string apiName,
        ILoggerFactory? loggerFactory)
    {
        var method = typeof(ExternalApiServiceCollectionExtensions)
            .GetMethod(nameof(AddExternalRefitClientGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(clientInterface);

        return (IServiceCollection)method.Invoke(null,
            new object[] { services, apiName, loggerFactory })!;
    }

    private static IServiceCollection AddExternalRefitClientGeneric<TClient>(
        IServiceCollection services,
        string apiName,
        ILoggerFactory? loggerFactory)
        where TClient : class
    {
        return services.AddExternalRefitClient<TClient>(apiName, loggerFactory);
    }
}
```

---

### 9. Register in DI (Infrastructure Layer)

**File:** `Infrastructure/ServiceCollectionExtensions.cs`

```csharp
using [YourAppName].Application.Interfaces;
using [YourAppName].Infrastructure.ExternalApis;
using [YourAppName].Infrastructure.ExternalApis.Providers;

namespace [YourAppName].Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ... other registrations

        services.AddSingleton<IExternalApiConfigurationProvider, DatabaseExternalApiProvider>();
        services.AddExternalApiServices();

        return services;
    }
}
```

---

### 10. Seed Configs at Startup (API Layer)

**File:** `API/Extensions/WebApplicationExtensions.cs`

```csharp
public static async Task UsePlatformDataSeedingAsync(this WebApplication app)
{
    using var scope = app.Services.CreateScope();

    var provider = scope.ServiceProvider.GetRequiredService<IExternalApiConfigurationProvider>();
    await provider.ReloadAsync();
    Log.Information("External API configuration provider cache loaded");
}
```

> **Important:** Call this **after** building the app but **before** `app.Run()`. It ensures the singleton provider has loaded configs before the first HTTP request arrives.

---

### 11. Create Refit Client Interfaces (Application Layer)

**File:** `Application/ExternalApis/Clients/IPlaceholderClient.cs`

```csharp
using Refit;

namespace [YourAppName].Application.ExternalApis.Clients;

[ExternalApiClient("PlaceholderApi")]
public interface IPlaceholderClient
{
    [Get("/posts")]
    Task<List<PlaceholderPostDto>> GetPostsAsync(CancellationToken cancellationToken = default);

    [Get("/posts/{id}")]
    Task<PlaceholderPostDto> GetPostByIdAsync(int id, CancellationToken cancellationToken = default);

    [Get("/posts/{id}/comments")]
    Task<List<PlaceholderCommentDto>> GetCommentsAsync(int id, CancellationToken cancellationToken = default);
}

public class PlaceholderPostDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class PlaceholderCommentDto
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
```

**File:** `Application/ExternalApis/Clients/IWeatherClient.cs`

```csharp
using Refit;

namespace [YourAppName].Application.ExternalApis.Clients;

[ExternalApiClient("WeatherApi")]
public interface IWeatherClient
{
    [Get("/weather")]
    Task<WeatherApiResponse> GetCurrentWeatherAsync(
        [Query] string city,
        [Query] string units = "metric",
        CancellationToken cancellationToken = default);

    [Get("/forecast")]
    Task<WeatherApiForecastResponse> GetForecastAsync(
        [Query] string city,
        [Query] int cnt = 5,
        [Query] string units = "metric",
        CancellationToken cancellationToken = default);
}

public class WeatherApiResponse
{
    public string Name { get; set; } = string.Empty;
    public WeatherApiMain Main { get; set; } = new();
    public WeatherApiWind Wind { get; set; } = new();
    public List<WeatherApiDescription> Weather { get; set; } = new();
}

public class WeatherApiMain
{
    public double Temp { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public double TempMin { get; set; }
    public double TempMax { get; set; }
}

public class WeatherApiWind
{
    public double Speed { get; set; }
}

public class WeatherApiDescription
{
    public string Main { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class WeatherApiForecastResponse
{
    public List<WeatherApiForecastItem> List { get; set; } = new();
}

public class WeatherApiForecastItem
{
    public DateTime Dt { get; set; }
    public WeatherApiForecastMain Main { get; set; } = new();
    public List<WeatherApiDescription> Weather { get; set; } = new();
}

public class WeatherApiForecastMain
{
    public double Temp { get; set; }
    public double TempMin { get; set; }
    public double TempMax { get; set; }
    public int Humidity { get; set; }
}
```

---

### 12. Handler Usage Pattern (Application Layer)

**File:** `Application/ExternalApis/Queries/GetPosts/GetPostsQuery.cs`

```csharp
using [YourAppName].Application.Contracts;
using [YourAppName].Application.ExternalApis.Clients;
using [YourAppName].Application.ExternalApis.DTOs;
using MediatR;

namespace [YourAppName].Application.ExternalApis.Queries.GetPosts;

public record GetPostsQuery : IQuery<Result<List<PostDto>>>;

public class GetPostsQueryHandler : IQueryHandler<GetPostsQuery, Result<List<PostDto>>>
{
    private readonly IPlaceholderClient _placeholderClient;

    public GetPostsQueryHandler(IPlaceholderClient placeholderClient)
    {
        _placeholderClient = placeholderClient;
    }

    public async Task<Result<List<PostDto>>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        var posts = await _placeholderClient.GetPostsAsync(ct);
        var mapped = posts.Select(p => new PostDto
        {
            Id = p.Id,
            UserId = p.UserId,
            Title = p.Title,
            Body = p.Body
        }).ToList();
        return Result<List<PostDto>>.Success(mapped);
    }
}
```

**File:** `Application/ExternalApis/Queries/GetWeather/GetWeatherQuery.cs`

```csharp
using [YourAppName].Application.Contracts;
using [YourAppName].Application.Errors;
using [YourAppName].Application.ExternalApis.Clients;
using [YourAppName].Application.ExternalApis.DTOs;
using [YourAppName].Application.Localization;
using [YourAppName].Domain.Common;
using MediatR;

namespace [YourAppName].Application.ExternalApis.Queries.GetWeather;

public record GetWeatherQuery(string City = "London") : IQuery<Result<WeatherDto>>;

public class GetWeatherQueryHandler : IQueryHandler<GetWeatherQuery, Result<WeatherDto>>
{
    private readonly IWeatherClient? _weatherClient;
    private readonly ILocalizationService _localizationService;

    public GetWeatherQueryHandler(IWeatherClient? weatherClient, ILocalizationService localizationService)
    {
        _weatherClient = weatherClient;
        _localizationService = localizationService;
    }

    public async Task<Result<WeatherDto>> Handle(GetWeatherQuery request, CancellationToken ct)
    {
        if (_weatherClient == null)
        {
            var localized = _localizationService.GetLocalizedMessage(ApplicationErrors.ExternalApi.NOT_CONFIGURED);
            return Result<WeatherDto>.Failure(new Error(
                ApplicationErrors.ExternalApi.NOT_CONFIGURED,
                localized.Ar,
                localized.En,
                ErrorType.Internal));
        }

        try
        {
            var weather = await _weatherClient.GetCurrentWeatherAsync(request.City, "metric", ct);
            var mapped = new WeatherDto
            {
                Name = weather.Name,
                Main = new WeatherMainDto
                {
                    Temp = weather.Main.Temp,
                    FeelsLike = weather.Main.FeelsLike,
                    Humidity = weather.Main.Humidity,
                    TempMin = weather.Main.TempMin,
                    TempMax = weather.Main.TempMax
                },
                Wind = new WeatherWindDto { Speed = weather.Wind.Speed },
                Weather = weather.Weather.Select(w => new WeatherDescriptionDto
                {
                    Main = w.Main,
                    Description = w.Description,
                    Icon = w.Icon
                }).ToList()
            };
            return Result<WeatherDto>.Success(mapped);
        }
        catch (Exception ex)
        {
            var localized = _localizationService.GetLocalizedMessage(ApplicationErrors.General.INTERNAL_ERROR);
            return Result<WeatherDto>.Failure(new Error(
                ApplicationErrors.General.INTERNAL_ERROR,
                localized.Ar,
                localized.En,
                ErrorType.Internal,
                new Dictionary<string, string[]> { { "technicalErrors", new[] { ex.Message } } }));
        }
    }
}
```

> **Pattern:** If the Refit client is optional (config may not exist), make the constructor parameter nullable (`IWeatherClient?`). If it's mandatory, use non-nullable.

---

## Database Seed Example

Insert a row into `ExternalApiConfigurations` so the provider can resolve it:

```sql
INSERT INTO ExternalApiConfigurations (
    Id, Name, BaseUrl, TimeoutSeconds, IsEnabled,
    AuthType, AuthKeyName, AuthKeyLocation, AuthValue,
    CreatedAt
) VALUES (
    NEWID(), 'PlaceholderApi', 'https://jsonplaceholder.typicode.com', 30, 1,
    'None', NULL, NULL, NULL,
    GETUTCDATE()
);
```

For an API key-protected API:

```sql
INSERT INTO ExternalApiConfigurations (
    Id, Name, BaseUrl, TimeoutSeconds, IsEnabled,
    AuthType, AuthKeyName, AuthKeyLocation, AuthValue,
    CreatedAt
) VALUES (
    NEWID(), 'WeatherApi', 'https://api.openweathermap.org/data/2.5', 30, 1,
    'ApiKey', 'appid', 'Query', 'YOUR_ENCRYPTED_API_KEY',
    GETUTCDATE()
);
```

---

## Auth Type Mapping Reference

| `AuthType` | Required Fields | Handler Behavior |
|------------|-----------------|----------------|
| `None` | — | NoOpDelegatingHandler (pass-through) |
| `ApiKey` | `KeyName`, `KeyLocation`, `Value` | Adds header or query parameter |
| `Bearer` | `Token` | Sets `Authorization: Bearer <token>` |
| `Basic` | `ClientId` (username), `ClientSecret` (password) | Sets `Authorization: Basic <base64>` |
| `OAuth2` | `TokenUrl`, `ClientId`, `ClientSecret`, `Scope` | Acquires token via client_credentials, caches, auto-refreshes |

---

## Resilience Behavior Reference

`AddStandardResilienceHandler()` adds the following policies automatically:

| Policy | Default Behavior |
|--------|------------------|
| Retry | 3 retries with exponential backoff |
| Circuit Breaker | Opens after 5 consecutive failures, reopens after 30s |
| Timeout | Matches `HttpClient.Timeout` |
| Hedging | Disabled by default |

> **Note:** You can customize these via `AddStandardResilienceHandler(options => { ... })` if needed.
