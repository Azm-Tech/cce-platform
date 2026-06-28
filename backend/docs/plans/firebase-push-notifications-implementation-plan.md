# Firebase Push Notifications — Implementation Plan

Aligned with existing notification architecture: Clean Architecture, DDD, CQRS, `Response<T>` + `MessageFactory`, `INotificationChannelHandler` plug-in model.

---

## 1. Overview & Goals

Add a **fourth notification channel (`Push = 3`)** that delivers FCM push notifications to mobile/web devices. The implementation must:

- Plug into the existing `INotificationChannelHandler` pipeline — the `NotificationGateway` already fans out across channels and logs every attempt; Push must ride that same path.
- Store device tokens as a first-class entity (`UserDeviceToken`) with upsert semantics — one row per physical device, not per token rotation.
- Deactivate stale tokens automatically when FCM returns `registration-token-not-registered`.
- Expose two authenticated endpoints (`POST /api/me/device-tokens`, `DELETE /api/me/device-tokens/{deviceId}`) that mobile clients call on login / logout.
- Remain optional — `NotificationGateway` already skips channels with no registered sender, so APIs start cleanly if Firebase is not configured.

### What is NOT changed

- `NotificationGateway.cs` dispatch loop — works as-is once `Push` is in the enum and a sender is registered.
- MassTransit / outbox pattern — `NotificationMessage` already supports any channel list.
- SignalR real-time delivery — `InAppNotificationChannelSender` is untouched.

---

## 2. Architecture Alignment Checklist

| Convention | Applied here |
|---|---|
| Read logic in query context; writes via repo + `SaveChangesAsync` | RegisterDeviceToken handler uses repo + `_db.SaveChangesAsync` |
| `Response<T>` + `MessageFactory` for all command results | All new command handlers return `Response<T>` via `_msg.*` |
| No inline anonymous classes or logic in endpoints | Endpoints only call `mediator.Send(...)` and `.ToHttpResult()` |
| `INotificationChannelHandler` for new channels | `PushNotificationChannelSender` implements the interface |
| `IFirebaseMessagingService` abstraction | Testability — unit tests can substitute without real Firebase |
| Permissions from `permissions.yaml` | Two new `Notification.DeviceToken.*` permissions added |

---

## 3. Phase 0 — NuGet & Config Foundation

### 3.1 `Directory.Packages.props`

Add one entry inside the `<ItemGroup Label="External API clients (Refit)">` block (or create a new `Firebase` group):

```xml
<!-- Firebase Admin SDK — push notifications via FCM -->
<PackageVersion Include="FirebaseAdmin" Version="3.1.0" />
```

### 3.2 `src/CCE.Infrastructure/CCE.Infrastructure.csproj`

Add the reference alongside the other `PackageReference` entries:

```xml
<PackageReference Include="FirebaseAdmin" />
```

### 3.3 `appsettings.Development.json` (both `CCE.Api.External` and `CCE.Api.Internal`)

Add a `Firebase` section. For local dev, point at a service account JSON file via user-secrets rather than committing credentials:

```json
"Firebase": {
  "ProjectId": "your-firebase-project-id",
  "ServiceAccountJson": ""
}
```

Run `dotnet user-secrets set "Firebase:ServiceAccountJson" "$(cat path/to/service-account.json)"` per API project.

### 3.4 `appsettings.Production.json` (both APIs)

```json
"Firebase": {
  "ProjectId": "",
  "ServiceAccountJson": ""
}
```

Override at deploy time via:
- `$env:Firebase__ProjectId` 
- `$env:Firebase__ServiceAccountJson` (raw JSON string, base64, or mounted file path — resolved in `FirebaseMessagingService`)

---

## 4. Phase 1 — Domain

### 4.1 Extend `NotificationChannel` enum

**File:** `src/CCE.Domain/Notifications/NotificationChannel.cs`

```csharp
namespace CCE.Domain.Notifications;
public enum NotificationChannel { Email = 0, Sms = 1, InApp = 2, Push = 3 }
```

> `Push = 3` preserves existing numeric values stored in the database for Email/SMS/InApp.

### 4.2 New entity: `UserDeviceToken`

**File:** `src/CCE.Domain/Notifications/UserDeviceToken.cs` *(new)*

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Notifications;

/// <summary>
/// An FCM registration token tied to a specific physical device.
/// One row per (UserId, DeviceId) — DeviceId is a stable client-generated UUID.
/// Tokens rotate; this entity is updated in-place via Refresh().
/// NOT audited — high-cardinality, managed by the device lifecycle.
/// </summary>
public sealed class UserDeviceToken : Entity<System.Guid>
{
    private UserDeviceToken(
        System.Guid id,
        System.Guid userId,
        string deviceId,
        string token,
        string platform,
        System.DateTimeOffset registeredOn) : base(id)
    {
        UserId = userId;
        DeviceId = deviceId;
        Token = token;
        Platform = platform;
        RegisteredOn = registeredOn;
        LastSeenOn = registeredOn;
        IsActive = true;
    }

    public System.Guid UserId { get; private set; }

    /// <summary>Stable UUID the client generates on first launch. Never rotates.</summary>
    public string DeviceId { get; private set; }

    /// <summary>FCM registration token. Rotates; updated via Refresh().</summary>
    public string Token { get; private set; }

    /// <summary>"ios" | "android" | "web"</summary>
    public string Platform { get; private set; }

    public System.DateTimeOffset RegisteredOn { get; private set; }
    public System.DateTimeOffset LastSeenOn { get; private set; }
    public bool IsActive { get; private set; }

    public static UserDeviceToken Register(
        System.Guid userId,
        string deviceId,
        string token,
        string platform,
        ISystemClock clock)
    {
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (string.IsNullOrWhiteSpace(deviceId)) throw new DomainException("DeviceId is required.");
        if (string.IsNullOrWhiteSpace(token)) throw new DomainException("Token is required.");
        if (platform is not ("ios" or "android" or "web"))
            throw new DomainException("Platform must be 'ios', 'android', or 'web'.");

        return new UserDeviceToken(
            System.Guid.NewGuid(), userId, deviceId, token, platform, clock.UtcNow);
    }

    /// <summary>Called when the client reports a refreshed FCM token for an existing device.</summary>
    public void Refresh(string newToken, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(newToken)) throw new DomainException("Token is required.");
        Token = newToken;
        LastSeenOn = clock.UtcNow;
        IsActive = true;
    }

    /// <summary>Called when FCM reports the token is no longer valid.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
```

---

## 5. Phase 2 — Application

### 5.1 Repository interface

**File:** `src/CCE.Application/Notifications/IUserDeviceTokenRepository.cs` *(new)*

```csharp
using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public interface IUserDeviceTokenRepository
{
    Task<IReadOnlyList<UserDeviceToken>> GetActiveByUserIdAsync(
        System.Guid userId, CancellationToken cancellationToken);

    Task<UserDeviceToken?> GetByUserAndDeviceAsync(
        System.Guid userId, string deviceId, CancellationToken cancellationToken);

    Task AddAsync(UserDeviceToken token, CancellationToken cancellationToken);

    /// <summary>
    /// Deactivates tokens matching the given FCM token values (called after FCM rejects them).
    /// </summary>
    Task DeactivateByTokensAsync(
        IReadOnlyList<string> fcmTokens, CancellationToken cancellationToken);
}
```

### 5.2 `RenderedNotification` — add MetaData

**File:** `src/CCE.Application/Notifications/RenderedNotification.cs`

The FCM data payload needs the same variable context used for template rendering (postId, communityId, etc.). Add an optional `MetaData` property:

```csharp
using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public sealed record RenderedNotification(
    string TemplateCode,
    System.Guid? RecipientUserId,
    System.Guid TemplateId,
    string Subject,
    string SubjectAr,
    string SubjectEn,
    string Body,
    NotificationChannel Channel,
    string Locale,
    string? Email = null,
    string? PhoneNumber = null,
    IReadOnlyDictionary<string, string>? MetaData = null);  // NEW
```

### 5.3 RegisterDeviceToken command

**File:** `src/CCE.Application/Notifications/Public/Commands/RegisterDeviceToken/RegisterDeviceTokenCommand.cs` *(new)*

```csharp
using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.RegisterDeviceToken;

public sealed record RegisterDeviceTokenCommand(
    System.Guid UserId,
    string Token,
    string Platform,
    string DeviceId
) : IRequest<Response<VoidData>>;
```

**File:** `src/CCE.Application/Notifications/Public/Commands/RegisterDeviceToken/RegisterDeviceTokenCommandValidator.cs` *(new)*

```csharp
using FluentValidation;

namespace CCE.Application.Notifications.Public.Commands.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandValidator
    : AbstractValidator<RegisterDeviceTokenCommand>
{
    public RegisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.DeviceId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Platform).NotEmpty().Must(p => p is "ios" or "android" or "web")
            .WithMessage("Platform must be 'ios', 'android', or 'web'.");
    }
}
```

**File:** `src/CCE.Application/Notifications/Public/Commands/RegisterDeviceToken/RegisterDeviceTokenCommandHandler.cs` *(new)*

```csharp
using CCE.Application.Common;
using CCE.Application.Messages;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandHandler
    : IRequestHandler<RegisterDeviceTokenCommand, Response<VoidData>>
{
    private readonly IUserDeviceTokenRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ISystemClock _clock;

    public RegisterDeviceTokenCommandHandler(
        IUserDeviceTokenRepository repo,
        ICceDbContext db,
        MessageFactory msg,
        ISystemClock clock)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
        _clock = clock;
    }

    public async Task<Response<VoidData>> Handle(
        RegisterDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _repo
            .GetByUserAndDeviceAsync(request.UserId, request.DeviceId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Refresh(request.Token, _clock);
        }
        else
        {
            var token = UserDeviceToken.Register(
                request.UserId,
                request.DeviceId,
                request.Token,
                request.Platform,
                _clock);
            await _repo.AddAsync(token, cancellationToken).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok<VoidData>();
    }
}
```

### 5.4 UnregisterDeviceToken command

**File:** `src/CCE.Application/Notifications/Public/Commands/UnregisterDeviceToken/UnregisterDeviceTokenCommand.cs` *(new)*

```csharp
using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.UnregisterDeviceToken;

public sealed record UnregisterDeviceTokenCommand(
    System.Guid UserId,
    string DeviceId
) : IRequest<Response<VoidData>>;
```

**File:** `src/CCE.Application/Notifications/Public/Commands/UnregisterDeviceToken/UnregisterDeviceTokenCommandHandler.cs` *(new)*

```csharp
using CCE.Application.Common;
using CCE.Application.Messages;
using CCE.Application.Common.Interfaces;
using MediatR;

namespace CCE.Application.Notifications.Public.Commands.UnregisterDeviceToken;

public sealed class UnregisterDeviceTokenCommandHandler
    : IRequestHandler<UnregisterDeviceTokenCommand, Response<VoidData>>
{
    private readonly IUserDeviceTokenRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UnregisterDeviceTokenCommandHandler(
        IUserDeviceTokenRepository repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        UnregisterDeviceTokenCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _repo
            .GetByUserAndDeviceAsync(request.UserId, request.DeviceId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null || existing.UserId != request.UserId)
            return _msg.NotFound<VoidData>("Device token not found.");

        existing.Deactivate();
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok<VoidData>();
    }
}
```

---

## 6. Phase 3 — Infrastructure

### 6.1 Firebase options & DI helpers

**File:** `src/CCE.Infrastructure/Firebase/FirebaseOptions.cs` *(new)*

```csharp
namespace CCE.Infrastructure.Firebase;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";
    public string ProjectId { get; init; } = string.Empty;
    /// <summary>Raw service-account JSON string. Injected via env var or user-secrets.</summary>
    public string ServiceAccountJson { get; init; } = string.Empty;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ProjectId)
                             && !string.IsNullOrWhiteSpace(ServiceAccountJson);
}
```

### 6.2 `IFirebaseMessagingService` abstraction

**File:** `src/CCE.Infrastructure/Firebase/IFirebaseMessagingService.cs` *(new)*

```csharp
using FirebaseAdmin.Messaging;

namespace CCE.Infrastructure.Firebase;

public interface IFirebaseMessagingService
{
    Task<BatchResponse> SendMulticastAsync(
        MulticastMessage message, CancellationToken cancellationToken);
}
```

### 6.3 `FirebaseMessagingService` implementation

**File:** `src/CCE.Infrastructure/Firebase/FirebaseMessagingService.cs` *(new)*

```csharp
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Firebase;

public sealed class FirebaseMessagingService : IFirebaseMessagingService
{
    private readonly FirebaseMessaging _messaging;
    private readonly ILogger<FirebaseMessagingService> _logger;

    public FirebaseMessagingService(
        IOptions<FirebaseOptions> options,
        ILogger<FirebaseMessagingService> logger)
    {
        _logger = logger;
        var opts = options.Value;

        // FirebaseApp is a process-wide singleton. Guard against double-init on hot-reload.
        var app = FirebaseApp.GetInstance("[DEFAULT]") ?? FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential
                .FromJson(opts.ServiceAccountJson)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging"),
            ProjectId = opts.ProjectId
        });

        _messaging = FirebaseMessaging.GetMessaging(app);
    }

    public async Task<BatchResponse> SendMulticastAsync(
        MulticastMessage message, CancellationToken cancellationToken)
    {
        // FCM SDK does not natively accept CancellationToken on SendEachForMulticastAsync.
        // Register the token so we throw OperationCanceledException on cancellation.
        cancellationToken.ThrowIfCancellationRequested();
        var response = await _messaging
            .SendEachForMulticastAsync(message)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "FCM multicast: {SuccessCount} sent, {FailureCount} failed.",
            response.SuccessCount, response.FailureCount);

        return response;
    }
}
```

> **Note:** `FirebaseApp.GetInstance("[DEFAULT]")` returns `null` if the app has not been created yet (the SDK does not throw). Use `?.` or null-check before calling `Create`.

### 6.4 `PushNotificationChannelSender`

**File:** `src/CCE.Infrastructure/Notifications/PushNotificationChannelSender.cs` *(new)*

```csharp
using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Firebase;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications;

public sealed class PushNotificationChannelSender : INotificationChannelHandler
{
    // FCM error codes that mean the token is permanently invalid.
    private static readonly HashSet<string> _staleTokenCodes = new(StringComparer.Ordinal)
    {
        "messaging/registration-token-not-registered",
        "messaging/invalid-registration-token",
        "messaging/mismatched-credential"
    };

    private readonly IUserDeviceTokenRepository _tokenRepo;
    private readonly IFirebaseMessagingService _firebase;
    private readonly ILogger<PushNotificationChannelSender> _logger;

    public PushNotificationChannelSender(
        IUserDeviceTokenRepository tokenRepo,
        IFirebaseMessagingService firebase,
        ILogger<PushNotificationChannelSender> logger)
    {
        _tokenRepo = tokenRepo;
        _firebase = firebase;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Push;

    public bool ShouldSend(UserNotificationSettings? settings) => settings?.IsEnabled ?? true;

    public async Task<ChannelSendResult> SendAsync(
        RenderedNotification notification,
        CancellationToken cancellationToken)
    {
        if (notification.RecipientUserId is null)
            return new ChannelSendResult(false, Error: "Push requires a recipient user ID.");

        var deviceTokens = await _tokenRepo
            .GetActiveByUserIdAsync(notification.RecipientUserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (deviceTokens.Count == 0)
        {
            // Not an error — user simply has no registered devices.
            _logger.LogDebug(
                "No active device tokens for user {UserId}; skipping push for {TemplateCode}.",
                notification.RecipientUserId, notification.TemplateCode);
            return new ChannelSendResult(true, ProviderMessageId: "no-devices");
        }

        var rawTokens = deviceTokens.Select(t => t.Token).ToList();

        // Build FCM data payload from MetaData + templateCode for deep-link routing.
        var data = new Dictionary<string, string>
        {
            ["templateCode"] = notification.TemplateCode,
            ["locale"] = notification.Locale
        };

        if (notification.MetaData is not null)
        {
            foreach (var (k, v) in notification.MetaData)
                data[k] = v;
        }

        var message = new MulticastMessage
        {
            Tokens = rawTokens,
            Notification = new Notification
            {
                Title = notification.Subject,
                Body = notification.Body
            },
            Data = data,
            Apns = new ApnsConfig
            {
                Aps = new Aps { Sound = "default" }
            },
            Android = new AndroidConfig
            {
                Priority = Priority.High
            }
        };

        var batchResponse = await _firebase
            .SendMulticastAsync(message, cancellationToken)
            .ConfigureAwait(false);

        // Collect stale tokens to deactivate.
        var staleTokens = new List<string>();
        for (var i = 0; i < batchResponse.Responses.Count; i++)
        {
            var r = batchResponse.Responses[i];
            if (!r.IsSuccess && r.Exception is not null
                && _staleTokenCodes.Contains(r.Exception.MessagingErrorCode.ToString()))
            {
                staleTokens.Add(rawTokens[i]);
            }
        }

        if (staleTokens.Count > 0)
        {
            _logger.LogInformation(
                "Deactivating {Count} stale FCM tokens for user {UserId}.",
                staleTokens.Count, notification.RecipientUserId);
            await _tokenRepo
                .DeactivateByTokensAsync(staleTokens, cancellationToken)
                .ConfigureAwait(false);
        }

        var success = batchResponse.SuccessCount > 0 || deviceTokens.Count == 0;
        var error = success ? null
            : $"All {batchResponse.FailureCount} FCM sends failed.";

        return new ChannelSendResult(success, Error: error);
    }
}
```

### 6.5 `UserDeviceTokenRepository`

**File:** `src/CCE.Infrastructure/Notifications/UserDeviceTokenRepository.cs` *(new)*

```csharp
using CCE.Application.Common.Pagination;
using CCE.Application.Notifications;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Notifications;

public sealed class UserDeviceTokenRepository : IUserDeviceTokenRepository
{
    private readonly ICceDbContext _db;

    public UserDeviceTokenRepository(ICceDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserDeviceToken>> GetActiveByUserIdAsync(
        System.Guid userId, CancellationToken cancellationToken)
    {
        return await _db.UserDeviceTokens
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<UserDeviceToken?> GetByUserAndDeviceAsync(
        System.Guid userId, string deviceId, CancellationToken cancellationToken)
    {
        return (await _db.UserDeviceTokens
            .Where(t => t.UserId == userId && t.DeviceId == deviceId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .FirstOrDefault();
    }

    public async Task AddAsync(UserDeviceToken token, CancellationToken cancellationToken)
    {
        await _db.UserDeviceTokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeactivateByTokensAsync(
        IReadOnlyList<string> fcmTokens, CancellationToken cancellationToken)
    {
        var tokens = await _db.UserDeviceTokens
            .Where(t => fcmTokens.Contains(t.Token) && t.IsActive)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        foreach (var t in tokens)
            t.Deactivate();
    }
}
```

> **`ICceDbContext` change required:** Add `DbSet<UserDeviceToken> UserDeviceTokens { get; }` to the interface and the `CceDbContext` class.

### 6.6 EF Configuration

**File:** `src/CCE.Infrastructure/Persistence/Configurations/Notifications/UserDeviceTokenConfiguration.cs` *(new)*

```csharp
using CCE.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Notifications;

public sealed class UserDeviceTokenConfiguration : IEntityTypeConfiguration<UserDeviceToken>
{
    public void Configure(EntityTypeBuilder<UserDeviceToken> builder)
    {
        builder.ToTable("user_device_token");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.DeviceId).IsRequired().HasMaxLength(128);
        builder.Property(t => t.Token).IsRequired().HasMaxLength(512);
        builder.Property(t => t.Platform).IsRequired().HasMaxLength(16);
        builder.Property(t => t.RegisteredOn).IsRequired();
        builder.Property(t => t.LastSeenOn).IsRequired();
        builder.Property(t => t.IsActive).IsRequired();

        // One row per physical device per user — prevents duplicate registrations.
        builder.HasIndex(t => new { t.UserId, t.DeviceId }).IsUnique();

        // Fast fetch of all active tokens for a user (called on every push send).
        builder.HasIndex(t => new { t.UserId, t.IsActive });

        // Fast deactivation lookup after FCM rejects a token.
        builder.HasIndex(t => t.Token);
    }
}
```

### 6.7 `DependencyInjection.cs` — wire everything

In `src/CCE.Infrastructure/DependencyInjection.cs`, add inside the notification block (after line 235):

```csharp
// Firebase push channel (registered only when Firebase is configured)
services.Configure<FirebaseOptions>(configuration.GetSection(FirebaseOptions.SectionName));
var firebaseOptions = configuration
    .GetSection(FirebaseOptions.SectionName)
    .Get<FirebaseOptions>();

if (firebaseOptions?.IsConfigured == true)
{
    services.AddSingleton<IFirebaseMessagingService, FirebaseMessagingService>();
    services.AddScoped<INotificationChannelHandler, PushNotificationChannelSender>();
}

// Device token repository
services.AddScoped<IUserDeviceTokenRepository, UserDeviceTokenRepository>();
```

> Registering `IFirebaseMessagingService` as **singleton** is correct — `FirebaseApp` is a process-wide singleton; wrapping it in scoped services would cause multiple-initialization issues.

---

## 7. Phase 4 — API Endpoints & Permissions

### 7.1 `permissions.yaml`

Add inside the existing `Notification:` group:

```yaml
  Notification:
    DeviceToken:
      Register:
        description: Register or refresh a device push token for the authenticated user
        roles: [cce-super-admin, cce-admin, cce-content-manager, cce-state-representative, cce-reviewer, cce-expert, cce-user]
      Delete:
        description: Unregister a device push token for the authenticated user (on logout)
        roles: [cce-super-admin, cce-admin, cce-content-manager, cce-state-representative, cce-reviewer, cce-expert, cce-user]
```

Rebuild `CCE.Domain` after editing — the source generator emits `Permissions.Notification_DeviceToken_Register` and `Permissions.Notification_DeviceToken_Delete`.

### 7.2 New endpoints

**File:** `src/CCE.Api.External/Endpoints/DeviceTokenEndpoints.cs` *(new)*

```csharp
using CCE.Api.Common.Extensions;
using CCE.Api.Common.Results;
using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications.Public.Commands.RegisterDeviceToken;
using CCE.Application.Notifications.Public.Commands.UnregisterDeviceToken;
using CCE.Domain.Permissions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class DeviceTokenEndpoints
{
    public static IEndpointRouteBuilder MapDeviceTokenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/me/device-tokens")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapPost("", async (
            RegisterDeviceTokenRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var cmd = new RegisterDeviceTokenCommand(userId, body.Token, body.Platform, body.DeviceId);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("RegisterDeviceToken")
        .RequireAuthorization(Permissions.Notification_DeviceToken_Register);

        group.MapDelete("/{deviceId}", async (
            string deviceId,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return EnvelopeResults.Unauthorized();
            var cmd = new UnregisterDeviceTokenCommand(userId, deviceId);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("UnregisterDeviceToken")
        .RequireAuthorization(Permissions.Notification_DeviceToken_Delete);

        return app;
    }

    public sealed record RegisterDeviceTokenRequest(
        string Token,
        string Platform,
        string DeviceId);
}
```

### 7.3 Register in `Program.cs`

In `src/CCE.Api.External/Program.cs`, add alongside the other `MapXxxEndpoints()` calls:

```csharp
app.MapDeviceTokenEndpoints();
```

---

## 8. Phase 5 — Thread MetaData Through the Gateway

**File:** `src/CCE.Infrastructure/Notifications/NotificationGateway.cs` (line ~205)

In `DispatchChannelAsync`, when constructing `RenderedNotification`, add the `MetaData` positional argument:

```csharp
var rendered = new RenderedNotification(
    request.TemplateCode,
    request.RecipientUserId,
    template.Id,
    subject,
    subjectAr,
    subjectEn,
    body,
    channel,
    locale,
    email,
    phone,
    MetaData: request.Variables);   // <-- add this line
```

No other changes to the gateway are needed. All existing senders (`Email`, `SMS`, `InApp`) ignore `MetaData`; only `PushNotificationChannelSender` reads it.

---

## 9. Phase 6 — Add Push to Existing Notification Handlers

For each domain event handler in `src/CCE.Application/Notifications/Handlers/`, add `NotificationChannel.Push` to the Channels list where push is appropriate:

| Handler | Recommended channels |
|---|---|
| `ExpertRegistrationApprovedNotificationHandler` | Email + InApp + **Push** |
| `ExpertRegistrationRejectedNotificationHandler` | Email + InApp + **Push** |
| `NewsPublishedNotificationHandler` | InApp + **Push** |
| `ResourcePublishedNotificationHandler` | InApp + **Push** |
| `CountryContentRequestApprovedNotificationHandler` | Email + InApp + **Push** |
| `CountryContentRequestRejectedNotificationHandler` | Email + InApp + **Push** |

Example diff in any handler:

```csharp
// Before
Channels = [NotificationChannel.InApp]

// After
Channels = [NotificationChannel.InApp, NotificationChannel.Push]
```

The `NotificationGateway` skips channels with no template; if no `Push` template exists yet, the gateway logs a Skipped result — no error is thrown.

---

## 10. Phase 7 — Seeder: Push Notification Templates

In `src/CCE.Seeder`, add Push-channel templates alongside existing InApp/Email templates. Follow the same pattern as existing `NotificationTemplateSeeder` entries:

```csharp
// Example: push template for ExpertRegistrationApproved
NotificationTemplate.Define(
    code:      "EXPERT_REQUEST_APPROVED",
    channel:   NotificationChannel.Push,
    subjectAr: "تمت الموافقة على طلبك",
    subjectEn: "Your Expert Request Was Approved",
    bodyAr:    "تهانينا! تمت الموافقة على طلب التسجيل كخبير.",
    bodyEn:    "Congratulations! Your expert registration request has been approved.",
    variableSchemaJson: null)
```

Repeat for every event type where Push is desired. The seeder is idempotent — running it twice does not create duplicates.

---

## 11. Phase 8 — EF Migration

After all code changes are in place:

```powershell
$env:CCE_DESIGN_SQL_CONN = "Server=...;Database=...;..."
dotnet ef migrations add AddUserDeviceToken `
    --project src/CCE.Infrastructure `
    --startup-project src/CCE.Infrastructure

dotnet ef database update `
    --project src/CCE.Infrastructure `
    --startup-project src/CCE.Infrastructure
```

The migration should produce one new table: `user_device_token` with columns:

| Column | Type | Notes |
|---|---|---|
| `id` | `uniqueidentifier` | PK |
| `user_id` | `uniqueidentifier` | NOT NULL |
| `device_id` | `nvarchar(128)` | NOT NULL |
| `token` | `nvarchar(512)` | NOT NULL |
| `platform` | `nvarchar(16)` | NOT NULL |
| `registered_on` | `datetimeoffset` | NOT NULL |
| `last_seen_on` | `datetimeoffset` | NOT NULL |
| `is_active` | `bit` | NOT NULL |

Indexes:

| Name | Columns | Unique |
|---|---|---|
| `ix_user_device_token_user_id_device_id` | `(user_id, device_id)` | YES |
| `ix_user_device_token_user_id_is_active` | `(user_id, is_active)` | NO |
| `ix_user_device_token_token` | `(token)` | NO |

---

## 12. Testing Strategy

### Unit tests (no real FCM)

Create `src/CCE.Application.Tests/Notifications/RegisterDeviceTokenCommandHandlerTests.cs`:

- Substitute `IUserDeviceTokenRepository` + `ICceDbContext` via NSubstitute (matches existing pattern)
- Test: new device → `AddAsync` called once
- Test: existing device → `Refresh` called, `AddAsync` not called
- Test: invalid platform → validation error before handler runs

Create `src/CCE.Infrastructure.Tests/Notifications/PushNotificationChannelSenderTests.cs`:

- Substitute `IFirebaseMessagingService` — return mock `BatchResponse`
- Test: no active tokens → returns `ChannelSendResult(true, "no-devices")`
- Test: all tokens succeed → returns `ChannelSendResult(true)`
- Test: FCM returns `registration-token-not-registered` → `DeactivateByTokensAsync` called

### Integration tests

Extend `CceTestWebApplicationFactory` to substitute `IFirebaseMessagingService` with a test double that records calls. Then test:

- `POST /api/me/device-tokens` with valid body → 200 OK, token stored
- `POST /api/me/device-tokens` same DeviceId, new token → 200 OK, token updated (not duplicated)
- `DELETE /api/me/device-tokens/{deviceId}` → 200 OK, `is_active = false`

---

## 13. Deployment Considerations

### Firebase service account

- **Never commit `service-account.json`** to the repository.
- Dev: inject via `dotnet user-secrets`.
- Production: set `$env:Firebase__ServiceAccountJson` as a JSON string in your deployment environment (Azure App Service Application Settings, Kubernetes Secret, etc.).
- If `Firebase:IsConfigured` is false, the Push channel is simply not registered — both APIs start cleanly.

### Multiple API instances

`FirebaseApp` is a process-wide singleton, safe for multi-instance deployments. Each process creates its own app instance independently.

### FCM token TTL

Android tokens can expire after ~2 months of app inactivity. The stale-token cleanup in `PushNotificationChannelSender` handles this automatically — tokens are deactivated after the first failed send.

---

## 14. Complete File Change Index

| File | Action | Layer |
|---|---|---|
| `Directory.Packages.props` | Add `FirebaseAdmin 3.1.0` | Root |
| `src/CCE.Infrastructure/CCE.Infrastructure.csproj` | Add `<PackageReference Include="FirebaseAdmin" />` | Infrastructure |
| `appsettings.Development.json` (both APIs) | Add `Firebase` section | Config |
| `appsettings.Production.json` (both APIs) | Add `Firebase` section | Config |
| `permissions.yaml` | Add `Notification.DeviceToken.Register/Delete` | Root |
| `src/CCE.Domain/Notifications/NotificationChannel.cs` | Add `Push = 3` | Domain |
| `src/CCE.Domain/Notifications/UserDeviceToken.cs` | **NEW** | Domain |
| `src/CCE.Application/Notifications/IUserDeviceTokenRepository.cs` | **NEW** | Application |
| `src/CCE.Application/Notifications/RenderedNotification.cs` | Add `MetaData` field | Application |
| `src/CCE.Application/Notifications/Public/Commands/RegisterDeviceToken/*.cs` | **NEW** (3 files) | Application |
| `src/CCE.Application/Notifications/Public/Commands/UnregisterDeviceToken/*.cs` | **NEW** (2 files) | Application |
| `src/CCE.Infrastructure/Firebase/FirebaseOptions.cs` | **NEW** | Infrastructure |
| `src/CCE.Infrastructure/Firebase/IFirebaseMessagingService.cs` | **NEW** | Infrastructure |
| `src/CCE.Infrastructure/Firebase/FirebaseMessagingService.cs` | **NEW** | Infrastructure |
| `src/CCE.Infrastructure/Notifications/PushNotificationChannelSender.cs` | **NEW** | Infrastructure |
| `src/CCE.Infrastructure/Notifications/UserDeviceTokenRepository.cs` | **NEW** | Infrastructure |
| `src/CCE.Infrastructure/Persistence/Configurations/Notifications/UserDeviceTokenConfiguration.cs` | **NEW** | Infrastructure |
| `src/CCE.Infrastructure/Notifications/NotificationGateway.cs` | Add `MetaData` to `RenderedNotification` constructor call | Infrastructure |
| `src/CCE.Infrastructure/DependencyInjection.cs` | Register Firebase services + device token repo | Infrastructure |
| `src/CCE.Application/Common/Interfaces/ICceDbContext.cs` | Add `DbSet<UserDeviceToken> UserDeviceTokens` | Application |
| `src/CCE.Infrastructure/Persistence/CceDbContext.cs` | Add `DbSet<UserDeviceToken> UserDeviceTokens` | Infrastructure |
| `src/CCE.Api.External/Endpoints/DeviceTokenEndpoints.cs` | **NEW** | API |
| `src/CCE.Api.External/Program.cs` | Call `MapDeviceTokenEndpoints()` | API |
| `src/CCE.Application/Notifications/Handlers/*.cs` | Add `NotificationChannel.Push` to relevant Channels | Application |
| `src/CCE.Seeder` | Add Push templates for each event type | Seeder |
| EF Migration | `AddUserDeviceToken` migration | Infrastructure |
