# Using the Messaging System (RabbitMQ + MassTransit + Outbox)

A practical, step-by-step guide for working with async events in this solution. For the *why* and the
architecture, see [`masstransit-messaging-guide.md`](./masstransit-messaging-guide.md) §9.

---

## 0. Mental model (read this first)

There are **two ways** to do async work, and a clear rule for which to use:

| You want to… | Use | Runs where |
|---|---|---|
| React to something that happened inside the domain (post created, resource published) | **Domain event** → MediatR handler | in-process, pre-commit |
| Send a notification (email/SMS/in-app) as fire-and-forget | `INotificationMessageDispatcher` | published to bus → **Worker** |
| Hand work to another process / future service | `IIntegrationEventPublisher` + a contract | published to bus → **Worker** |
| Do something the user must see *immediately* (OTP, password reset) | `INotificationGateway` **directly** | in-process, synchronous |

The golden flow for anything that goes on the bus:

```
HTTP request → command handler mutates aggregate → SaveChanges
                                   │
   DomainEventDispatcher (PRE-commit) fires MediatR domain-event handlers
                                   │
        handler calls IIntegrationEventPublisher / INotificationMessageDispatcher
                                   │
            → row written to `outbox_message` in the SAME transaction
                                   │     (commit) 
        BusOutboxDeliveryService relays the row → RabbitMQ → CCE.Worker consumer
```

**You never touch the outbox, the bus, or RabbitMQ in handler code.** You call an interface; durability is automatic.

---

## 1. Run it locally

### Option A — no broker (default, simplest)
Dev config ships with `Transport: InMemory` and `FallbackToInMemoryIfUnavailable: true`, so the API
consumes in-process. Just run the API:

```powershell
dotnet run --project src/CCE.Api.Internal --urls "http://localhost:5002"
```
Notifications/events are handled inside the same process. No RabbitMQ, no Worker needed.

### Option B — real broker + Worker (production-like)
```powershell
# 1. start the broker (management UI at http://localhost:15672, login cce / cce)
docker compose up -d rabbitmq

# 2. point the API at RabbitMQ (env var overrides appsettings)
$env:Messaging__Transport = "RabbitMQ"
$env:Messaging__RabbitMqHost = "localhost"
$env:Messaging__RabbitMqUsername = "cce"
$env:Messaging__RabbitMqPassword = "cce"
dotnet run --project src/CCE.Api.Internal --urls "http://localhost:5002"

# 3. in another terminal, run the consumer host
$env:Messaging__Transport = "RabbitMQ"
$env:Messaging__RabbitMqHost = "localhost"
$env:Messaging__RabbitMqUsername = "cce"
$env:Messaging__RabbitMqPassword = "cce"
dotnet run --project src/CCE.Worker
```
Apply the outbox migration once before first run (creates `outbox_message` etc.):
```powershell
$env:CCE_DESIGN_SQL_CONN = "<your sql conn>"
dotnet ef database update --project src/CCE.Infrastructure --startup-project src/CCE.Infrastructure
# or: dotnet run --project src/CCE.Seeder -- --migrate
```

---

## 2. Send a notification from a handler  *(most common case)*

Nothing changed here — this is the existing pattern, now durable for free.

**Step 1 — make sure a domain event exists** on your aggregate (e.g. `PostCreatedEvent` in `CCE.Domain`).
Aggregates raise it via `RaiseDomainEvent(...)`; the `DomainEventDispatcher` publishes it through MediatR.

**Step 2 — write/extend a notification handler** in `CCE.Application/Notifications/Handlers/`:

```csharp
using CCE.Application.Notifications.Messages;
using CCE.Domain.Community.Events;
using CCE.Domain.Notifications;
using MediatR;

public sealed class PostCreatedNotificationHandler
    : INotificationHandler<PostCreatedEvent>
{
    private readonly INotificationMessageDispatcher _dispatcher;

    public PostCreatedNotificationHandler(INotificationMessageDispatcher dispatcher)
        => _dispatcher = dispatcher;

    public async Task Handle(PostCreatedEvent e, CancellationToken ct)
    {
        await _dispatcher.DispatchAsync(new NotificationMessage(
            TemplateCode:    "COMMUNITY_POST_CREATED",
            RecipientUserId: e.AuthorId,
            EventType:       NotificationEventType.CommunityPostCreated,
            Channels:        new[] { NotificationChannel.InApp },
            Locale:          "en"), ct);
        // returns immediately — the message is staged in the outbox and delivered by the Worker.
    }
}
```

**Step 3 — there is no step 3.** MediatR auto-discovers `INotificationHandler<T>`; the dispatcher is
already registered; the `NotificationMessageConsumer` in the Worker already handles `NotificationMessage`.
Add the `COMMUNITY_POST_CREATED` template row and you're done.

> **Need immediate, blocking delivery (OTP, password reset)?** Inject `INotificationGateway` and call
> `SendAsync(...)` directly instead — that path is synchronous and never touches the bus.

---

## 3. Publish a general integration event (new cross-process work)

Use this when the reaction isn't a notification — e.g. "rebuild a projection", "notify an external
system", "kick off a long job in the Worker".

### Step 1 — define the contract (Application layer, POCO, no MassTransit)
`src/CCE.Application/Common/Messaging/IntegrationEvents/PostPublishedIntegrationEvent.cs`:
```csharp
namespace CCE.Application.Common.Messaging.IntegrationEvents;

public sealed record PostPublishedIntegrationEvent(
    System.Guid PostId,
    System.Guid AuthorId,
    System.DateTimeOffset OccurredOn);
```

### Step 2 — publish it from a domain-event handler (Application layer)
```csharp
using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Domain.Community.Events;
using MediatR;

public sealed class PostPublishedIntegrationHandler
    : INotificationHandler<PostCreatedEvent>
{
    private readonly IIntegrationEventPublisher _publisher;

    public PostPublishedIntegrationHandler(IIntegrationEventPublisher publisher)
        => _publisher = publisher;

    public Task Handle(PostCreatedEvent e, CancellationToken ct)
        => _publisher.PublishAsync(
            new PostPublishedIntegrationEvent(e.PostId, e.AuthorId, e.OccurredOn), ct);
}
```
Because this runs pre-commit, the publish is captured into `outbox_message` and committed atomically
with the post.

### Step 3 — write the consumer (Infrastructure layer)
`src/CCE.Infrastructure/<Area>/Messaging/PostPublishedConsumer.cs`:
```csharp
using CCE.Application.Common.Messaging.IntegrationEvents;
using MassTransit;
using Microsoft.Extensions.Logging;

public sealed class PostPublishedConsumer : IConsumer<PostPublishedIntegrationEvent>
{
    private readonly ILogger<PostPublishedConsumer> _logger;
    public PostPublishedConsumer(ILogger<PostPublishedConsumer> logger) => _logger = logger;

    public async Task Consume(ConsumeContext<PostPublishedIntegrationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Handling PostPublished {PostId}", msg.PostId);
        // … do the async work: call a service, update a projection, hit an external API …
        await Task.CompletedTask;
    }
}
```
Optional retry/concurrency policy (mirrors `NotificationMessageConsumerDefinition`):
```csharp
public sealed class PostPublishedConsumerDefinition : ConsumerDefinition<PostPublishedConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpoint,
        IConsumerConfigurator<PostPublishedConsumer> consumer,
        IRegistrationContext context)
        => endpoint.UseMessageRetry(r => r.Intervals(
               TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)));
}
```

### Step 4 — register the consumer so the Worker runs it
In `src/CCE.Infrastructure/Notifications/Messaging/MessagingServiceExtensions.cs`, add it inside the
**`if (registerConsumers)`** block (right next to the notification consumer):
```csharp
if (registerConsumers)
{
    x.AddConsumer<NotificationMessageConsumer, NotificationMessageConsumerDefinition>();
    x.AddConsumer<PostPublishedConsumer, PostPublishedConsumerDefinition>(); // ← add this
}
```
That's it. The APIs publish (publish-only), the **Worker** consumes. No endpoint wiring needed —
`ConfigureEndpoints` builds the queue from the consumer definition (kebab-cased to
`post-published-integration-event`).

---

## 4. Production configuration

`appsettings.Production.json` (already set for both APIs + Worker):
```json
"Messaging": {
  "Transport": "RabbitMQ",
  "RabbitMqHost": "rabbitmq",
  "RabbitMqVirtualHost": "/cce-prod",
  "UseAsyncDispatcher": true,
  "FallbackToInMemoryIfUnavailable": false
}
```
Credentials come from env vars only (never commit them):
```
Messaging__RabbitMqUsername=cce
Messaging__RabbitMqPassword=<secret>
```
Deploy the Worker alongside the APIs — it's the `worker` service in `docker-compose.prod.yml`
(`depends_on` the migrator completing + RabbitMQ healthy).

---

## 5. Verify it's working

| Check | How |
|---|---|
| Message hit the broker | RabbitMQ mgmt UI → `http://localhost:15672` → Queues |
| Outbox staged & drained | `SELECT * FROM outbox_message` — a row appears, then disappears after relay |
| Consumer ran | Worker logs: `Consuming NotificationMessage …` / your consumer's log line |
| Crash-safety | stop RabbitMQ, trigger the action → API still returns 200, `outbox_message` row **persists**; restart broker → row drains |
| Broker health | `GET /health/ready` reports `rabbitmq` (only when `Transport=RabbitMQ`) |

---

## 6. Do / Don't

- ✅ **Do** publish from a MediatR domain-event handler (pre-commit) so the outbox captures it.
- ✅ **Do** keep integration-event contracts as plain `record`s in `CCE.Application` (no MassTransit attrs).
- ✅ **Do** add new consumers to the `if (registerConsumers)` block — only the Worker should consume.
- ❌ **Don't** inject `IPublishEndpoint` / `IBus` directly in handlers — use `IIntegrationEventPublisher`
  (keeps Application MassTransit-free and routes through the outbox).
- ❌ **Don't** call `_dbContext.SaveChanges()` inside a domain-event handler — dispatch runs inside the
  in-flight save; a nested save breaks the outbox guarantee.
- ❌ **Don't** put blocking, user-facing delivery (OTP) on the bus — use `INotificationGateway` directly.
