# MassTransit Messaging — How It Fits CCE & Developer Guide

## 1. What Was Added and Why

CCE notifications were previously **synchronous and blocking**: when a domain event
fired (e.g. "Resource Published"), the handler called `INotificationGateway.SendAsync`
**inline**, meaning the HTTP request thread waited for:

1. DB template lookup
2. DB user-settings lookup
3. External SMS / Email gateway HTTP call
4. DB `NotificationLog` insert + `SaveChanges`

With MassTransit, **fire-and-forget domain-event notifications** are published onto a
message bus and handled by `NotificationMessageConsumer` asynchronously.
The HTTP thread returns as soon as the message is published (~1 ms).

```
BEFORE (synchronous)
─────────────────────────────────────────────────────────────────
HTTP Request → Handler → INotificationGateway → SMS/Email → DB
                                  ↑ blocks entire request thread

AFTER (async via MassTransit)
─────────────────────────────────────────────────────────────────
HTTP Request → Handler → IPublishEndpoint.Publish() → returns 200
                                        ↓ (bus queue)
                              NotificationMessageConsumer
                                        ↓
                              INotificationGateway → SMS/Email → DB
```

**OTP and password-reset are NOT affected.** They call `INotificationGateway` directly
and intentionally remain synchronous — the user needs immediate delivery confirmation.

> **Update (RabbitMQ + Outbox + Worker).** The bus now runs on a real **RabbitMQ** broker in
> staging/production, publishes are made **crash-safe** by the MassTransit **EF Core transactional
> outbox**, and all consumers run in a dedicated **`CCE.Worker`** service. The APIs are publish-only.
> See [§9](#9-rabbitmq-outbox--the-cceworker-service) for the full picture; the notification-handler
> code below is unchanged.

---

## 1a. The canonical integration-event pattern (READ THIS BEFORE ADDING EVENTS)

There is **one** way to emit a cross-process integration event. Command handlers **never** inject
`IIntegrationEventPublisher` or call `IPublishEndpoint`. Instead:

```
Command handler mutates an aggregate
   → aggregate.RaiseDomainEvent(SomethingHappenedEvent)        (Domain)
SaveChanges →
   DomainEventDispatcher.SavingChangesAsync (PRE-commit)       (Infrastructure interceptor)
      → MediatR publishes the domain event in-process
         → XxxBusPublisher bridge handler                      (Application/Notifications/Handlers)
            → IIntegrationEventPublisher.PublishAsync(integrationEvent)
               → MassTransit EF bus-outbox stages outbox_message in the SAME DbContext
   → the in-flight SaveChanges commits aggregate + outbox_message ATOMICALLY
BusOutboxDeliveryService relays outbox_message → RabbitMQ → CCE.Worker consumer
```

**Why this and not an inline `PublishAsync` in the handler?**
- The bus-outbox only persists a staged `outbox_message` if a `SaveChanges` runs **after** the publish.
  Publishing **after** `SaveChanges` (a real bug we fixed) silently loses the message. Raising the event
  on the aggregate guarantees the publish happens inside the dispatcher, *during* the save — always atomic.
- It keeps `CCE.Application` command handlers free of bus plumbing (Clean Architecture).

**Constraint:** domain events are only collected from tracked **`AggregateRoot<Guid>`** instances. In
Community only `Post` and `Community` are aggregate roots, so raise events on the aggregate the handler
already loads (e.g. `Post.RegisterVote`, `Post.RegisterReply`, `Community.RegisterJoinRequest`).

**To add a new async event:** (1) add a domain-event record under `Domain/.../Events/`; (2) raise it from
an aggregate method; (3) add an integration-event POCO under
`Application/Common/Messaging/IntegrationEvents/`; (4) add a one-line `XxxBusPublisher` bridge handler;
(5) add an `IConsumer<XxxIntegrationEvent>` in `CCE.Worker`. **Do not add an integration event with no
consumer** — it is dead weight (we removed `UserFollowed`/`UserUnfollowed`/`ResourcePublished` for this).

### Realtime (SignalR) is hybrid — never double-push

- **Instant actor feedback** (the user who voted/replied) is pushed **directly** from the API command
  handler via `ICommunityRealtimePublisher`.
- **Fan-out to other viewers** of a post/community/topic is pushed by a **Worker consumer**
  (`SignalRConsumer` for new posts) off the integration event.
- A single logical signal is owned by **exactly one** side. `VoteConsumer` therefore does **not** push
  `VoteChanged` (the command handler already does); it only keeps the Redis hot counters warm.

---

## 2. Architecture Map

```
CCE.Application
  └─ INotificationMessageDispatcher          ← single abstraction all handlers use
  └─ NotificationMessage (record)            ← the message contract

CCE.Infrastructure
  └─ Notifications/Messaging/
      ├─ MessagingOptions.cs                 ← config: Transport, RabbitMqHost, UseAsyncDispatcher
      ├─ MessagingServiceExtensions.cs       ← AddCceMessaging() DI extension
      ├─ MassTransitNotificationMessageDispatcher.cs  ← publishes to bus
      ├─ NotificationMessageConsumer.cs      ← picks from bus → INotificationGateway
      └─ NotificationMessageConsumerDefinition.cs     ← retry policy, concurrency
  └─ InProcessNotificationMessageDispatcher.cs        ← legacy sync path (kept, still works)
```

The single line that controls sync vs async:

```json
// appsettings.json
"Messaging": {
  "Transport": "InMemory",       // or "RabbitMQ"
  "UseAsyncDispatcher": true     // false → falls back to InProcess
}
```

---

## 3. Transport Options

| Transport | Config value | When to use |
|---|---|---|
| **InMemory** | `"InMemory"` | Local dev, all tests. No broker needed. Messages live in-process — same reliability as before. |
| **RabbitMQ** | `"RabbitMQ"` | Staging and production. Requires a running broker. |

### RabbitMQ in production (`appsettings.Production.json`)

```json
"Messaging": {
  "Transport": "RabbitMQ",
  "RabbitMqHost": "rabbitmq",
  "RabbitMqVirtualHost": "/cce-prod",
  "UseAsyncDispatcher": true,
  "FallbackToInMemoryIfUnavailable": false
}
```

**Credentials are never committed.** Supply them via env vars (the host URI carries no password):

```
Messaging__RabbitMqUsername=cce
Messaging__RabbitMqPassword=<secret>
```

**Dev fallback.** In `appsettings.Development.json` the flag `FallbackToInMemoryIfUnavailable: true` is
set. When the broker can't be reached at startup, `AddCceMessaging` runs a ~2 s TCP probe, logs a warning,
and transparently drops to the **InMemory** transport with consumers running **in-process** — so a dev box
with no RabbitMQ still works end-to-end in a single process. Leave the flag **`false`** in production: the
outbox already makes a transient broker outage safe (messages wait durably in `outbox_message` and
MassTransit auto-reconnects), and a real outage should surface on `/health/ready` rather than be masked.

RabbitMQ is free (Apache 2.0). No license needed.

---

## 4. How Developers Use It

### 4.1 Sending a notification from a domain event handler (existing pattern — unchanged)

All existing handlers already inject `INotificationMessageDispatcher`.
**Nothing changes in how you write a handler.** You call `DispatchAsync` exactly as before:

```csharp
// Any domain event handler in CCE.Application
public sealed class ResourcePublishedNotificationHandler
    : INotificationHandler<ResourcePublishedEvent>
{
    private readonly INotificationMessageDispatcher _dispatcher;

    public async Task Handle(ResourcePublishedEvent notification, CancellationToken ct)
    {
        await _dispatcher.DispatchAsync(new NotificationMessage(
            TemplateCode:    "RESOURCE_PUBLISHED",
            RecipientUserId: resource.UploadedById,
            EventType:       NotificationEventType.ResourcePublished,
            Channels:        [NotificationChannel.InApp],
            Locale:          "en"), ct);
        // returns immediately — bus handles delivery asynchronously
    }
}
```

When `UseAsyncDispatcher=true` the call above **publishes to the bus**.
When `UseAsyncDispatcher=false` it **calls the gateway inline** — identical to pre-MassTransit.

### 4.2 Adding a new notification type

1. Add a `NotificationTemplate` row (SMS or Email or InApp) with your new `TemplateCode`.
2. Create a domain event (e.g. `ExpertApprovedEvent`) in `CCE.Domain`.
3. Create a handler in `CCE.Application.Notifications.Handlers` that calls `_dispatcher.DispatchAsync(...)`.
4. **Done.** MassTransit picks up the message automatically — no changes needed in Infrastructure.

### 4.3 Sending a notification directly (bypassing the bus — OTP / password reset style)

Inject `INotificationGateway` and call `SendAsync` directly.
This path is always synchronous and unaffected by `Messaging` config.
**Use this only for transactional, user-blocking flows (OTP, password reset, email confirmation).**

```csharp
// Handler that needs immediate delivery (e.g. OTP)
private readonly INotificationGateway _gateway;

await _gateway.SendAsync(new NotificationDispatchRequest(
    TemplateCode:    "OTP_VERIFICATION",
    RecipientUserId: null,
    Channels:        [NotificationChannel.Sms],
    Variables:       new Dictionary<string, string> { ["Code"] = code },
    PhoneNumber:     phoneNumber,
    BypassSettings:  true), ct);
```

---

## 5. Retry Behaviour

`NotificationMessageConsumerDefinition` configures three automatic retries
with exponential back-off (5 s → 15 s → 30 s).

If all retries fail, MassTransit moves the message to a `_error` queue
(RabbitMQ: `cce-notification-message-consumer_error`).
No message is silently dropped.

```
Attempt 1  ─ fails  ─►  wait 5 s
Attempt 2  ─ fails  ─►  wait 15 s
Attempt 3  ─ fails  ─►  wait 30 s
Attempt 4  ─ fails  ─►  moves to _error queue  ← inspect in RabbitMQ management UI
```

For manual recovery use the existing **Retry Notification Log** admin endpoint
(`POST /admin/notifications/logs/{id}/retry`) — it calls `INotificationGateway`
directly and bypasses the bus.

---

## 6. Testing

### Unit tests — use `UseAsyncDispatcher: false`

Integration tests in `CceTestWebApplicationFactory` set `UseAsyncDispatcher=false`
in the test settings so the dispatcher calls the gateway inline and you can verify
delivery without running a broker:

```csharp
// In CceTestWebApplicationFactory
builder.ConfigureAppConfiguration((_, cfg) =>
    cfg.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Messaging:Transport"]          = "InMemory",
        ["Messaging:UseAsyncDispatcher"] = "false",  // sync — easy to assert
    }));
```

### Unit tests — assert publish with MassTransit TestHarness

If you want to assert a message was published (not just that the gateway was called),
use `MassTransit.Testing`:

```csharp
// In test project (add MassTransit.Testing.Helpers package)
var harness = new InMemoryTestHarness();
var consumer = harness.Consumer<NotificationMessageConsumer>();

await harness.Start();
await harness.Bus.Publish(new NotificationMessage(...));
Assert.True(await consumer.Consumed.Any<NotificationMessage>());
await harness.Stop();
```

---

## 7. Decision Table — Which Path to Use

| Scenario | Use |
|---|---|
| Domain event → notify users (resource published, expert approved, post created, etc.) | `INotificationMessageDispatcher.DispatchAsync()` → goes via bus |
| OTP verification code | `INotificationGateway.SendAsync()` direct (synchronous) |
| Password reset email | `INotificationGateway.SendAsync()` direct (synchronous) |
| High-volume broadcast (future) | `INotificationMessageDispatcher.DispatchAsync()` → bus handles fan-out |
| Admin retry of a failed log | Existing retry endpoint → `INotificationGateway` direct |

---

## 8. Files Changed Summary

| File | Change |
|---|---|
| `Directory.Packages.props` | Added `MassTransit` + `MassTransit.RabbitMQ` version pins |
| `CCE.Infrastructure.csproj` | Added `PackageReference` for both packages |
| `Notifications/Messaging/MessagingOptions.cs` | New — config POCO |
| `Notifications/Messaging/MessagingServiceExtensions.cs` | New — `AddCceMessaging()` DI extension |
| `Notifications/Messaging/MassTransitNotificationMessageDispatcher.cs` | New — async dispatcher |
| `Notifications/Messaging/NotificationMessageConsumer.cs` | New — bus consumer |
| `Notifications/Messaging/NotificationMessageConsumerDefinition.cs` | New — retry policy |
| `DependencyInjection.cs` | Added `services.AddCceMessaging(configuration)` call |
| `appsettings.Development.json` (both APIs) | Added `"Messaging": { "Transport": "InMemory" }` |

**Application layer: zero changes.** All existing handlers continue to work without modification.

---

## 9. RabbitMQ, Outbox & the CCE.Worker service

This section documents the move from "InMemory, in-API consumer" to a durable, broker-backed topology.

### 9.1 Topology — APIs publish, the Worker consumes

```
API (External / Internal) — publish-only          CCE.Worker — consume-only
─────────────────────────────────────             ──────────────────────────────
Command handler mutates aggregate                  Hosts the consumers:
  → raises a domain event                            • NotificationMessageConsumer
DomainEventDispatcher.SavingChangesAsync (PRE-commit)• (future integration-event consumers)
  → in-process MediatR handlers                     Runs the BusOutboxDeliveryService:
  → IIntegrationEventPublisher /                       • polls outbox_message
    INotificationMessageDispatcher → IPublishEndpoint  • relays rows to RabbitMQ
  → bus outbox stages an outbox_message row         RabbitMQ → consumer → INotificationGateway → …
SaveChanges commits aggregate + outbox row ATOMICALLY
```

`AddCceMessaging(configuration, registerConsumers)` controls who runs receive endpoints: the APIs and the
Seeder call it with `false` (publish-only); `CCE.Worker` calls it with `true`. The
`BusOutboxDeliveryService` runs in every process and relays staged rows to the bus.

### 9.2 Why dispatch moved to `SavingChangesAsync` (pre-commit)

The bus outbox captures a publish by **adding an `outbox_message` row to the `CceDbContext` change
tracker during the `Publish()` call**. That row is only persisted by a subsequent `SaveChanges`. The old
dispatcher published in `SavedChangesAsync` (**post**-commit) — there was no save after it, so an outbox
row would never persist. Dispatching in `SavingChangesAsync` (**pre**-commit) means the handlers' publishes
are staged and committed by the **same** `SaveChanges` as the aggregate → atomic, no dual-write / lost
message. The notification handlers only read + dispatch (none call `SaveChanges`), so there's no
re-entrant save.

### 9.3 Outbox tables

`CceDbContext.OnModelCreating` adds the MassTransit entities (isolated in
`OutboxModelBuilderExtensions` so `using MassTransit;` doesn't collide with domain types like `Event`):
`inbox_state`, `outbox_state`, `outbox_message`. They are created by the `AddMassTransitOutbox` migration;
`CCE.Seeder --migrate` remains the canonical applier.

### 9.4 Adding a general (non-notification) integration event

1. Add a POCO `record` contract under `CCE.Application.Common.Messaging.IntegrationEvents` (no MassTransit
   attributes — keeps `CCE.Application` free of MassTransit).
2. In the relevant MediatR domain-event handler, inject `IIntegrationEventPublisher` and call
   `PublishAsync(contract, ct)`. The outbox makes it durable automatically.
3. Add a consumer (`IConsumer<TContract>` + a `ConsumerDefinition` for retry) in
   `CCE.Infrastructure`, and register it in `AddCceMessaging`'s `if (registerConsumers)` block so the
   **Worker** picks it up.

### 9.5 Running locally

```powershell
docker compose up -d rabbitmq                 # broker + mgmt UI at http://localhost:15672 (cce/cce)
# set Messaging__Transport=RabbitMQ for the API(s), then:
dotnet run --project src/CCE.Worker           # hosts the consumers
```

With the default dev settings (`Transport: InMemory`, `FallbackToInMemoryIfUnavailable: true`) you don't
need RabbitMQ or the Worker at all — the API consumes in-process.

### 9.6 Known follow-up — SignalR backplane

The Worker calls `AddSignalR()` so the notification consumer's `IHubContext<NotificationsHub>` dependency
resolves, but realtime **push** from the Worker won't reach clients connected to the APIs without a SignalR
**Redis backplane**. Until that is added, in-app notifications are still persisted by the gateway (clients
see them on next fetch) — only the live push is missed. Consumer-side **inbox** (idempotent consume) is
also deferred: the `inbox_state` table exists, but `UseInbox` is not yet enabled per-consumer.
