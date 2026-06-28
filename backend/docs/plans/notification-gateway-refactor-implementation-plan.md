# Notification Gateway Refactor Implementation Plan

## Goal

Refactor the notification implementation to match the standard CCE write pattern used by PlatformSettings:

- Repositories fetch tracked aggregates.
- Application handlers/orchestrators perform business flow.
- `ICceDbContext.SaveChangesAsync(ct)` is the unit-of-work boundary.
- Reads use `ICceDbContext` directly.
- API responses use `Response<T>` and `MessageFactory`.
- Commands, queries, DTOs, endpoint requests, and result contracts live in their own files.
- Create/update commands return only `Guid` when the ID is enough.
- Notification event handling is centralized so we do not create many almost-identical handlers that only differ by template/message code.

## Current Refactor Direction

The current implementation has useful building blocks:

- `NotificationLog`
- `UserNotificationSettings`
- `NotificationGateway`
- `INotificationChannelSender`
- email/SMS/in-app senders
- SignalR publisher
- admin log endpoints
- user settings endpoints

But it should be reshaped from service-style persistence into repository-style persistence, and from many event handlers into one generic notification message flow.

## Target Architecture

```text
Feature Workflow / Domain Event
        |
        v
NotificationMessage
        |
        v
INotificationMessageDispatcher
        |
        v
NotificationMessageHandler / Consumer
        |
        +--> INotificationTemplateRepository
        +--> IUserNotificationSettingsRepository
        +--> INotificationLogRepository
        +--> IUserNotificationRepository
        +--> ITemplateRenderer
        +--> IEnumerable<INotificationChannelHandler>
        |
        v
ICceDbContext.SaveChangesAsync(ct)
```

The shape is similar to the sample consumer, but adapted to this repository and MediatR-based solution. MassTransit can be introduced later if the system needs an external queue, but Phase 1 should keep the same in-process architecture unless the team has already approved a message broker.

## Naming

Use this terminology:

| Concept | Name |
|---|---|
| One notification request | `NotificationMessage` |
| Central processor | `NotificationMessageHandler` or `NotificationMessageConsumer` |
| Dispatch API used by feature code | `INotificationMessageDispatcher` |
| Per-channel sender | `INotificationChannelHandler` |
| Render service | `ITemplateRenderer` |
| Database fetch/persist boundary | repositories + `ICceDbContext.SaveChangesAsync` |

Avoid using `Service` for persistence APIs. Use repository names instead.

## Application Contracts

### `NotificationMessage`

File:

`src/CCE.Application/Notifications/Messages/NotificationMessage.cs`

Fields:

```csharp
public sealed record NotificationMessage(
    string TemplateCode,
    Guid? RecipientUserId,
    string? IdentityNumber,
    NotificationEventType EventType,
    IReadOnlyDictionary<string, string>? MetaData = null,
    IReadOnlyCollection<NotificationChannel>? Channels = null,
    string Locale = "en",
    string? Email = null,
    string? PhoneNumber = null,
    string? CorrelationId = null);
```

Notes:

- `RecipientUserId` is preferred inside CCE.
- `IdentityNumber` is optional and only needed if integration with identity-number-based systems is required.
- `Channels = null` means use active channels configured on the template.
- `MetaData` is the render variable bag.

### `NotificationEventType`

File:

`src/CCE.Domain/Notifications/NotificationEventType.cs`

Start with:

```csharp
public enum NotificationEventType
{
    ExpertRequestApproved = 0,
    ExpertRequestRejected = 1,
    CountryResourceApproved = 2,
    CountryResourceRejected = 3,
    NewsPublished = 4,
    ResourcePublished = 5,
    EventScheduled = 6,
    CommunityPostCreated = 7,
    AdminAccountCreated = 8
}
```

### `INotificationMessageDispatcher`

File:

`src/CCE.Application/Notifications/Messages/INotificationMessageDispatcher.cs`

```csharp
public interface INotificationMessageDispatcher
{
    Task DispatchAsync(NotificationMessage message, CancellationToken ct);
}
```

Phase 1 implementation:

- In-process dispatcher calls `NotificationMessageHandler.HandleAsync`.

Future implementation:

- MassTransit dispatcher publishes `NotificationMessage` to a queue.
- Consumer receives it and runs the same handler logic.

## Repository Pattern

Follow PlatformSettings:

```csharp
var settings = await _repo.GetAsync(ct);
settings.Update(...);
await _db.SaveChangesAsync(ct);
return _msg.Ok(settings.Id, "SETTINGS_UPDATED");
```

### `INotificationTemplateRepository`

File:

`src/CCE.Application/Notifications/INotificationTemplateRepository.cs`

Methods:

```csharp
Task<NotificationTemplate?> GetAsync(Guid id, CancellationToken ct);
Task<NotificationTemplate?> GetActiveByCodeAndChannelAsync(
    string code,
    NotificationChannel channel,
    CancellationToken ct);
Task<IReadOnlyList<NotificationTemplate>> GetActiveByCodeAsync(
    string code,
    CancellationToken ct);
void Add(NotificationTemplate template);
```

Implementation:

`src/CCE.Infrastructure/Notifications/NotificationTemplateRepository.cs`

Rules:

- Inject concrete `CceDbContext`.
- Return tracked entities for write use cases.
- Do not call `SaveChangesAsync`.
- Replace current `INotificationTemplateService`.

### `IUserNotificationRepository`

File:

`src/CCE.Application/Notifications/Public/IUserNotificationRepository.cs`

Methods:

```csharp
Task<UserNotification?> GetAsync(Guid id, CancellationToken ct);
void Add(UserNotification notification);
Task<int> MarkAllSentAsReadAsync(Guid userId, DateTimeOffset readOn, CancellationToken ct);
```

Implementation:

`src/CCE.Infrastructure/Notifications/UserNotificationRepository.cs`

Rules:

- `GetAsync` returns tracked entity for mark-read.
- `Add` only adds to context.
- `MarkAllSentAsReadAsync` may use `ExecuteUpdateAsync` because it is intentionally a direct bulk write.
- Handler still returns through `MessageFactory`.

### `IUserNotificationSettingsRepository`

File:

`src/CCE.Application/Notifications/IUserNotificationSettingsRepository.cs`

Methods:

```csharp
Task<UserNotificationSettings?> GetAsync(
    Guid userId,
    NotificationChannel channel,
    string? eventCode,
    CancellationToken ct);

Task<IReadOnlyList<UserNotificationSettings>> ListForUserAsync(Guid userId, CancellationToken ct);

Task<bool> IsUserSuppressedAsync(Guid userId, CancellationToken ct);
Task<bool> IsIdentityNumberSuppressedAsync(string identityNumber, CancellationToken ct);

void Add(UserNotificationSettings settings);
```

Implementation:

`src/CCE.Infrastructure/Notifications/UserNotificationSettingsRepository.cs`

Rules:

- Use tracked fetch for update commands.
- No internal save.
- `IsUserSuppressedAsync` can return false in Phase 1 until account-deactivation suppression is mapped clearly.

### `INotificationLogRepository`

File:

`src/CCE.Application/Notifications/INotificationLogRepository.cs`

Methods:

```csharp
Task<NotificationLog?> GetAsync(Guid id, CancellationToken ct);
void Add(NotificationLog log);
```

Implementation:

`src/CCE.Infrastructure/Notifications/NotificationLogRepository.cs`

Rules:

- `GetAsync` returns tracked entity for retry.
- `Add` only adds to context.
- No internal save.

## Remove Persistence Services

Delete or rename these persistence-style services:

| Current | Replace With |
|---|---|
| `INotificationTemplateService` | `INotificationTemplateRepository` |
| `IUserNotificationService` | `IUserNotificationRepository` |
| `INotificationLogService` | `INotificationLogRepository` |

Keep real infrastructure services only when they represent external effects:

- `ITemplateRenderer`
- `ISignalRNotificationPublisher`
- email/SMS gateway clients
- channel handlers

## Central Consumer / Handler

### `NotificationMessageHandler`

File:

`src/CCE.Application/Notifications/Messages/NotificationMessageHandler.cs`

Dependencies:

```csharp
INotificationTemplateRepository _templates;
IUserNotificationSettingsRepository _settings;
INotificationLogRepository _logs;
IUserNotificationRepository _inbox;
ITemplateRenderer _renderer;
IEnumerable<INotificationChannelHandler> _channelHandlers;
ICceDbContext _db;
ILogger<NotificationMessageHandler> _logger;
```

Algorithm:

1. Log template/event/recipient.
2. Suppression check:
   - if `RecipientUserId` exists, check user suppression.
   - if `IdentityNumber` exists, check identity suppression.
3. Load active templates by code.
4. If no templates, log warning and return.
5. Resolve or create notification settings.
6. Build handler map:

```csharp
var handlerMap = _channelHandlers.ToDictionary(h => h.Channel);
```

7. Resolve channels:
   - if message channels supplied, use them.
   - otherwise use active template channels.
8. For each channel:
   - find template for channel.
   - find handler.
   - check handler `ShouldSend(settings)`.
   - render channel content.
   - create `NotificationContext`.
   - call handler.
   - create `NotificationLog` for non-in-app channels, or for all channels if audit requires it.
9. Save once:

```csharp
await _db.SaveChangesAsync(ct).ConfigureAwait(false);
```

10. Publish SignalR after save for in-app notifications.

Important:

- Expected channel failures should be logged and produce `NotificationLog` failed rows.
- Do not throw for normal gateway send failures.
- Throw only for retry-worthy prerequisites, such as "phone missing for newly created admin account" if that is a real business requirement.

## Channel Handler Contract

Replace `INotificationChannelSender` with a richer handler:

File:

`src/CCE.Application/Notifications/INotificationChannelHandler.cs`

```csharp
public interface INotificationChannelHandler
{
    NotificationChannel Channel { get; }

    bool ShouldSend(UserNotificationSettings settings);

    Task<NotificationChannelResult> SendAsync(
        NotificationContext context,
        CancellationToken ct);
}
```

### `NotificationContext`

File:

`src/CCE.Application/Notifications/NotificationContext.cs`

Fields:

```csharp
public sealed record NotificationContext(
    Guid? RecipientUserId,
    string? IdentityNumber,
    string TemplateCode,
    NotificationEventType EventType,
    RenderedNotification Rendered,
    UserNotificationSettings Settings,
    IReadOnlyDictionary<string, string> MetaData);
```

### `NotificationChannelResult`

File:

`src/CCE.Application/Notifications/NotificationChannelResult.cs`

Fields:

```csharp
public sealed record NotificationChannelResult(
    bool Success,
    string? ExternalMessageId = null,
    string? ErrorMessage = null,
    Guid? UserNotificationId = null,
    UserNotification? UserNotification = null);
```

## Channel Implementations

### In-App Handler

File:

`src/CCE.Infrastructure/Notifications/InAppNotificationChannelHandler.cs`

Rules:

- Create `UserNotification`.
- Add through `IUserNotificationRepository.Add`.
- Return the entity in `NotificationChannelResult`.
- Do not query it back before save.
- SignalR publishing happens after `SaveChangesAsync`.

### Email Handler

File:

`src/CCE.Infrastructure/Notifications/EmailNotificationChannelHandler.cs`

Rules:

- Use `ICommunicationGatewayClient.SendEmailAsync`.
- Determine recipient from settings first, then message override if allowed.
- Return external message ID.
- No database save.

### SMS Handler

File:

`src/CCE.Infrastructure/Notifications/SmsNotificationChannelHandler.cs`

Rules:

- Use `ICommunicationGatewayClient.SendSmsAsync`.
- Determine recipient from settings first, then message override if allowed.
- Return external message ID.
- No database save.

## Reducing Duplicate Domain Event Handlers

Instead of one handler per event with repeated code, use a small mapping table.

### `NotificationEventMap`

File:

`src/CCE.Application/Notifications/Messages/NotificationEventMap.cs`

Example:

```csharp
public static class NotificationEventMap
{
    public static NotificationMessage From(ExpertRegistrationApprovedEvent ev) => new(
        TemplateCode: "EXPERT_REQUEST_APPROVED",
        RecipientUserId: ev.UserId,
        IdentityNumber: null,
        EventType: NotificationEventType.ExpertRequestApproved,
        MetaData: new Dictionary<string, string>
        {
            ["FullName"] = ev.FullName
        });
}
```

### Generic Event Handlers

Keep very thin handlers only where domain event types differ:

```csharp
public sealed class ExpertRegistrationApprovedNotificationHandler
    : INotificationHandler<ExpertRegistrationApprovedEvent>
{
    private readonly INotificationMessageDispatcher _dispatcher;

    public Task Handle(ExpertRegistrationApprovedEvent ev, CancellationToken ct)
        => _dispatcher.DispatchAsync(NotificationEventMap.From(ev), ct);
}
```

These handlers should contain no channel logic, no template rendering, and no gateway calls.

If the repetition still feels too high, introduce a generic adapter later:

```csharp
public interface INotificationEventMapper<in TEvent>
{
    NotificationMessage Map(TEvent ev);
}
```

Then one reusable handler can dispatch mapped events.

## Optional MassTransit Phase

Do not add MassTransit in Phase 1 unless the architecture decision is approved.

If approved:

1. Add `MassTransit` package versions to `Directory.Packages.props`.
2. Create shared contract:
   - `src/CCE.Application/Notifications/Messages/NotificationMessage.cs`, or a separate contracts project if cross-service.
3. Implement:

```csharp
public sealed class MassTransitNotificationMessageDispatcher : INotificationMessageDispatcher
{
    private readonly IPublishEndpoint _publish;

    public Task DispatchAsync(NotificationMessage message, CancellationToken ct)
        => _publish.Publish(message, ct);
}
```

4. Implement consumer:

```csharp
public sealed class NotificationMessageConsumer : IConsumer<NotificationMessage>
{
    private readonly NotificationMessageHandler _handler;

    public Task Consume(ConsumeContext<NotificationMessage> context)
        => _handler.HandleAsync(context.Message, context.CancellationToken);
}
```

5. Keep all real processing in `NotificationMessageHandler` so in-process and queued modes share the same code.

## Command / Query File Rules

Every request/response type gets its own file:

| Type | Location |
|---|---|
| Command | Same command folder, `XCommand.cs` |
| Command handler | Same command folder, `XCommandHandler.cs` |
| Validator | Same command folder, `XCommandValidator.cs` |
| Query | Same query folder, `XQuery.cs` |
| Query handler | Same query folder, `XQueryHandler.cs` |
| DTO | `Dtos` folder or query folder when admin-specific |
| Endpoint request | API endpoint folder, separate `XRequest.cs` |

Do not inline records at the top or bottom of handler files.

## Response Rules

Commands:

- Return `Response<Guid>` for create/update when the ID is enough.
- Return `Response<VoidData>` for no-content operations.
- Use `MessageFactory`.

Queries:

- Public/admin API queries should return `Response<T>` if this API area follows the unified envelope.
- Use `_msg.Ok(data, "ITEMS_LISTED")`.
- Use `_msg.XNotFound<T>()` for not found.

Endpoints:

- Use `ToHttpResult()`.
- Use `ToCreatedHttpResult()` for create commands.
- Do not manually branch into `Results.BadRequest`, `Results.NotFound`, `Results.Ok` for `Response<T>`.

## Refactor Steps

### Phase 1 - Rename Services to Repositories

- [ ] Create `INotificationTemplateRepository`.
- [ ] Create `IUserNotificationRepository`.
- [ ] Create `IUserNotificationSettingsRepository`.
- [ ] Create `INotificationLogRepository`.
- [ ] Implement each in Infrastructure with concrete `CceDbContext`.
- [ ] Remove internal `SaveChangesAsync` calls from notification persistence methods.
- [ ] Register repositories in `DependencyInjection.cs`.
- [ ] Delete old notification persistence service registrations.

### Phase 2 - Central Message Handler

- [ ] Add `NotificationMessage`.
- [ ] Add `NotificationEventType`.
- [ ] Add `INotificationMessageDispatcher`.
- [ ] Add in-process dispatcher.
- [ ] Add `NotificationMessageHandler`.
- [ ] Move template/settings/render/channel loop into this handler.
- [ ] Keep one `SaveChangesAsync` at the end.

### Phase 3 - Channel Handlers

- [ ] Replace `INotificationChannelSender` with `INotificationChannelHandler`.
- [ ] Add `NotificationContext`.
- [ ] Add `NotificationChannelResult`.
- [ ] Refactor in-app sender into handler using `IUserNotificationRepository`.
- [ ] Refactor email sender into handler using integration gateway.
- [ ] Refactor SMS sender into handler using integration gateway.
- [ ] Publish SignalR after save for in-app results.

### Phase 4 - Thin Domain Event Adapters

- [ ] Add `NotificationEventMap`.
- [ ] Replace duplicated handler logic with one-line dispatchers.
- [ ] Keep one handler per domain event only when required by MediatR.
- [ ] Ensure handlers do not render templates or choose delivery mechanics.

### Phase 5 - Commands / Queries / DTO Cleanup

- [ ] Split all inlined command records into `Command.cs`.
- [ ] Split all inlined query records into `Query.cs`.
- [ ] Split DTOs into dedicated DTO files.
- [ ] Split endpoint request records into API request files.
- [ ] Make create/update return `Response<Guid>`.
- [ ] Make query handlers return `Response<T>` where this API area expects unified response envelopes.

### Phase 6 - Tests

- [ ] Repository tests verify tracked fetch and no internal save.
- [ ] Message handler tests cover disabled settings, inactive template, no handler, send success, send failure.
- [ ] In-app handler test verifies it returns the created entity without querying before save.
- [ ] Email/SMS handler tests verify integration gateway calls.
- [ ] Domain event adapter tests verify mapped `NotificationMessage`.
- [ ] Endpoint tests verify `Response<T>` envelope and permissions.

## Target DI

```csharp
services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
services.AddScoped<IUserNotificationSettingsRepository, UserNotificationSettingsRepository>();
services.AddScoped<INotificationLogRepository, NotificationLogRepository>();

services.AddScoped<ITemplateRenderer, NotificationTemplateRenderer>();
services.AddScoped<INotificationMessageDispatcher, InProcessNotificationMessageDispatcher>();
services.AddScoped<NotificationMessageHandler>();

services.AddScoped<INotificationChannelHandler, EmailNotificationChannelHandler>();
services.AddScoped<INotificationChannelHandler, SmsNotificationChannelHandler>();
services.AddScoped<INotificationChannelHandler, InAppNotificationChannelHandler>();
services.AddScoped<ISignalRNotificationPublisher, SignalRNotificationPublisher>();
```

## Acceptance Criteria

- Notification write handlers follow the PlatformSettings pattern.
- No notification repository calls `SaveChangesAsync`.
- No feature/domain-event handler directly calls email/SMS/SignalR.
- One central notification message handler owns channel processing.
- In-app SignalR publish uses the created entity, not a pre-save database query.
- Create/update commands return IDs only.
- All new API command/query responses use `Response<T>` and `MessageFactory`.
- Commands, queries, DTOs, and endpoint requests are in separate files.
- `dotnet build CCE.sln` passes with zero warnings.

