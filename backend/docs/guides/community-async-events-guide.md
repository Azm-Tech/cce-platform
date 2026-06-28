# Community Async Events — Domain Events, Outbox & Worker (End‑to‑End Guide)

> Scope: how a state change in the Community module turns into reliable, cross‑process side effects
> (feeds, rankings, realtime, notifications) using **domain events → MediatR bridge → MassTransit EF
> transactional outbox → RabbitMQ → CCE.Worker consumers**. Covers every wired flow, the configuration
> knobs, how to test it, and the known gaps.
>
> Companion docs: `docs/masstransit-messaging-guide.md` (bus wiring, RabbitMQ/outbox/Worker topology),
> `docs/messaging-usage-guide.md` (notification dispatcher usage).

---

## 1. The one canonical pattern

There is exactly **one** way to emit a cross‑process integration event. Command handlers mutate an
aggregate and nothing else; they **never** inject `IIntegrationEventPublisher` or `IPublishEndpoint`.

```
Command handler                      mutates aggregate, calls aggregate method
  └─ aggregate.RaiseDomainEvent(XEvent)                         [CCE.Domain]
SaveChangesAsync()
  └─ DomainEventDispatcher.SavingChangesAsync  (PRE‑commit)     [CCE.Infrastructure interceptor]
       └─ IPublisher.Publish(XEvent)  (MediatR, in‑process)
            └─ XBusPublisher : INotificationHandler<XEvent>     [CCE.Application/Notifications/Handlers]
                 └─ IIntegrationEventPublisher.PublishAsync(XIntegrationEvent)
                      └─ MassTransit EF bus‑outbox stages an `outbox_message` row
                         in the SAME CceDbContext that is mid‑save
  └─ the in‑flight SaveChanges commits aggregate state + outbox_message ATOMICALLY
BusOutboxDeliveryService (every process) polls outbox_message
  └─ relays to RabbitMQ → CCE.Worker consumer → feed / ranking / realtime / notification side effects
```

**Why pre‑commit (`SavingChangesAsync`, not `SavedChangesAsync`)?** The MassTransit bus‑outbox captures a
publish by *adding* an `outbox_message` entity to the DbContext change tracker. That row is only
persisted by a `SaveChanges` that runs **after** the publish. Publishing post‑commit (or after the
handler's own `SaveChanges`) adds the row with no following save → it is **never persisted → message
silently lost**. Raising the event on the aggregate guarantees the publish happens inside the dispatcher,
*during* the save, so the message and the state change are always committed together (no dual‑write).

**Hard constraint:** `DomainEventDispatcher` only collects events from tracked **`AggregateRoot<Guid>`**
instances. In Community only **`Post`** and **`Community`** are aggregate roots — `PostReply`, `Poll`,
`PostVote`, `ReplyVote`, `UserFollow`, `CommunityMembership/JoinRequest/Follow` are plain entities. So an
event must be raised on the `Post`/`Community` the handler already loads (and that entity must be
**tracked** — the repos use tracking `FirstOrDefaultAsync`, so it is).

---

## 2. Moving parts (by layer)

| Layer | Type | Responsibility |
|---|---|---|
| `CCE.Domain` | `AggregateRoot<Guid>` (`Common/AggregateRoot.cs`) | Holds `DomainEvents`, `RaiseDomainEvent`, `ClearDomainEvents` |
| `CCE.Domain` | `Community/Events/*.cs` | Domain‑event records (`IDomainEvent`) |
| `CCE.Domain` | `Post` / `Community` methods | Mutate state **and** raise the event |
| `CCE.Infrastructure` | `Persistence/Interceptors/DomainEventDispatcher.cs` | Pre‑commit drain → `IPublisher.Publish` |
| `CCE.Application` | `Common/Messaging/IIntegrationEventPublisher.cs` | Bus abstraction (no MassTransit leak) |
| `CCE.Application` | `Common/Messaging/IntegrationEvents/*.cs` | POCO contracts carried on the bus |
| `CCE.Application` | `Notifications/Handlers/*BusPublisher.cs` | Bridge: domain event → integration event |
| `CCE.Infrastructure` | `Notifications/Messaging/MassTransitIntegrationEventPublisher.cs` | `IIntegrationEventPublisher` → `IPublishEndpoint` (outbox‑aware) |
| `CCE.Infrastructure` | `Notifications/Messaging/Consumers/*.cs` | Consume integration events in the Worker |
| `CCE.Worker` | `Program.cs` | Hosts consumers + the outbox delivery loop |

The DI entry point is `AddCceMessaging(config, registerConsumers)` in
`Infrastructure/Notifications/Messaging/MessagingServiceExtensions.cs`, called from
`DependencyInjection.AddInfrastructure`. **APIs/Seeder pass `registerConsumers: false` (publish‑only);
`CCE.Worker` passes `true`.** Both run the `BusOutboxDeliveryService`; only the Worker runs receive
endpoints.

---

## 3. Every wired flow

### Domain events → bridge → integration event → consumers

| Aggregate method (Domain) | Domain event | Bridge handler (Application) | Integration event | Consumers (Worker) |
|---|---|---|---|---|
| `Post.Publish` | `PostCreatedEvent` | `PostCreatedBusPublisher`¹ | `PostCreatedIntegrationEvent` | `FeedConsumer`, `RankingConsumer`, `SignalRConsumer`, `NotificationConsumer` |
| `Post.RegisterVote` | `PostVotedEvent` | `PostVotedBusPublisher` | `VoteCreatedIntegrationEvent` | `VoteConsumer` |
| `Post.RegisterReply` | `ReplyCreatedEvent` | `ReplyCreatedBusPublisher` | `ReplyCreatedIntegrationEvent` | `NotificationConsumer` |
| `Community.RegisterJoinRequest` | `CommunityJoinRequestedEvent` | `CommunityJoinRequestedBusPublisher` | `CommunityJoinRequestedIntegrationEvent` | `NotificationConsumer` |

¹ `PostCreatedBusPublisher` is the class inside `Notifications/Handlers/PostCreatedIntegrationEventHandler.cs`.

### What each consumer does

| Consumer | On message | Side effect |
|---|---|---|
| `FeedConsumer` | `PostCreatedIntegrationEvent` | Hybrid fan‑out: celebrity/expert authors skipped (merged at read time); normal authors pushed into each follower's `feed:user:{id}`; always updates `feed:community:{id}` + hot leaderboard |
| `RankingConsumer` | `PostCreatedIntegrationEvent` | Rebuilds `hot:{communityId}` sorted set from SQL `score` (top 1000); concurrency = 1 |
| `SignalRConsumer` | `PostCreatedIntegrationEvent` | Pushes `NewPost` to `community:{id}` + `topic:{id}` groups (realtime fan‑out) |
| `NotificationConsumer` | `PostCreatedIntegrationEvent` | Notifies topic+community followers (`COMMUNITY_POST_CREATED`, InApp) — **runs in the Worker, off the API thread** |
| `NotificationConsumer` | `ReplyCreatedIntegrationEvent` | Notifies post followers + post author + parent‑reply author (`POST_REPLIED`) |
| `NotificationConsumer` | `CommunityJoinRequestedIntegrationEvent` | Notifies community moderators (`COMMUNITY_JOIN_REQUESTED`) |
| `VoteConsumer` | `VoteCreatedIntegrationEvent` | Updates Redis hot counters only — **no SignalR push** (see realtime rule) |
| `NotificationMessageConsumer` | `NotificationMessage` | Renders + delivers a notification via `INotificationGateway` (email/SMS/InApp + log) |

`NotificationMessage` is the notification‑specific contract published by `INotificationMessageDispatcher`
(`MassTransitNotificationMessageDispatcher` when `Messaging:UseAsyncDispatcher=true`, else the in‑process
dispatcher). The notification consumers above **dispatch** `NotificationMessage`s, which then ride the bus
again to `NotificationMessageConsumer`.

### Realtime (SignalR) — hybrid, never double‑push

| Action | Endpoint | Instant push (API command handler, `ICommunityRealtimePublisher`) | Async fan‑out (Worker consumer) |
|---|---|---|---|
| Create/publish post | `POST /posts` (`saveAsDraft:false`), `POST /posts/{id}/publish` | — (author needs no echo) | `SignalRConsumer` → `NewPost` |
| Vote on post | `POST /posts/{id}/vote` | `VoteChanged` to `post:{id}` | — (`VoteConsumer` does Redis only) |
| Vote on reply | `POST /replies/{id}/vote` | `VoteChanged` to `post:{id}` | — |
| Reply | `POST /posts/{id}/replies` | `NewReply` to `post:{id}` | — |
| Poll vote | `POST /polls/{id}/vote` | `PollResultsChanged` to `post:{id}` | — |

Rule: **one logical signal is owned by exactly one side.** Cross‑instance delivery for the direct pushes
is handled by the SignalR **Redis backplane**, so a push from any API instance reaches all connected
clients. `ICommunityRealtimePublisher` → `CommunityRealtimePublisher`/`SignalRNotificationPublisher`.

---

## 4. The outbox

- Configured in `MessagingServiceExtensions.AddCceMessaging`:
  ```csharp
  x.AddEntityFrameworkOutbox<CceDbContext>(o =>
  {
      o.QueryDelay = TimeSpan.FromSeconds(1);  // delivery poll interval
      o.UseSqlServer();
      o.UseBusOutbox();                        // capture Publish into outbox_message, relay after SaveChanges
  });
  ```
- Tables (snake_case): **`outbox_message`** (staged messages), **`outbox_state`** (delivery cursor),
  **`inbox_state`** (reserved for idempotent consume — not yet enabled). Created by migration
  `20260608082540_AddMassTransitOutbox`.
- Behavior: a publish during the save inserts an `outbox_message` row in the same transaction; the
  `BusOutboxDeliveryService` relays it to the broker ~`QueryDelay` later and deletes it. If the broker is
  down, the row **persists** and is relayed when the broker returns — this is the crash‑safety guarantee.
- Interceptor wiring (critical): `AuditingInterceptor`, `DomainEventDispatcher`, **and** MassTransit's
  outbox interceptor must all be attached to `CceDbContext`. They are registered as `IInterceptor` and
  attached via `opts.AddInterceptors(sp.GetServices<IInterceptor>())` in
  `DependencyInjection.AddInfrastructure`. If the custom interceptors are registered only as their
  concrete type, `GetServices<IInterceptor>()` won't return them and **domain‑event dispatch silently
  stops** (this was a real regression — see §7).

---

## 5. Configuration

`Messaging` section (bind: `MessagingOptions`):

| Key | Default | Meaning |
|---|---|---|
| `Transport` | `InMemory` | `InMemory` (dev/test, per‑process bus) or `RabbitMQ` (staging/prod) |
| `RabbitMqHost` / `RabbitMqVirtualHost` | — | Broker host + vhost |
| `RabbitMqUsername` / `RabbitMqPassword` | — | Credentials via env (`Messaging__RabbitMqUsername`…), never committed |
| `UseAsyncDispatcher` | `true` | Swap `INotificationMessageDispatcher` to the bus publisher |
| `FallbackToInMemoryIfUnavailable` | `false` | **Dev‑only**: if `RabbitMQ` is unreachable at startup, fall back to InMemory + in‑process consumers |

Topology rule: **APIs/Seeder publish‑only (`registerConsumers:false`), `CCE.Worker` consumes
(`registerConsumers:true`)**. With `Transport=InMemory` the bus is per‑process, so to exercise the real
async path you need `RabbitMQ` + a running Worker (or the dev fallback, which forces in‑process consumers
in the single process).

---

## 6. How to test it end‑to‑end

### 6.1 Stand up the stack
```powershell
docker run -d --name cce-rabbit -p 5672:5672 -p 15672:15672 `
  -e RABBITMQ_DEFAULT_USER=cce -e RABBITMQ_DEFAULT_PASS=cce rabbitmq:3-management

$env:Messaging__Transport="RabbitMQ"; $env:Messaging__RabbitMqHost="localhost"
$env:Messaging__RabbitMqUsername="cce"; $env:Messaging__RabbitMqPassword="cce"

$env:CCE_DESIGN_SQL_CONN="Server=db52197.public.databaseasp.net;Database=db52197;User Id=db52197;Password=3Mm!x5#Y?rR9;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
dotnet run --project src/CCE.Seeder -- --migrate     # creates outbox_message/outbox_state/inbox_state

dotnet run --project src/CCE.Api.External --urls "http://localhost:5001"   # terminal 1 (publishes)
dotnet run --project src/CCE.Worker                                         # terminal 2 (consumes)
```
Auth: `POST http://localhost:5001/dev/sign-in` body `{"email":"admin@cce.test","role":"cce-admin"}` →
use `accessToken` as `Authorization: Bearer <token>` (requires `Auth:DevMode=true`).

### 6.2 Trigger → verify (base `http://localhost:5001/api/community`)

| Trigger | Body | State tables | Async proof |
|---|---|---|---|
| `POST /posts` `saveAsDraft:false` | community/topic/title/content | `posts` (`status=Published`) | `outbox_message` spikes→drains; Worker logs Feed/Ranking/SignalR/Notification; `user_notifications` for followers |
| `POST /posts/{id}/vote` | `{"direction":1}` | `post_votes`, `posts.upvote_count`/`score` | Worker log `VoteConsumer` (Redis only, no SignalR) |
| `POST /posts/{id}/replies` | `{"content":"hi","locale":"en"}` | `post_replies` | Worker log `NotificationConsumer: ReplyCreated`; `user_notifications` |
| `POST /communities/{id}/join` (private) | — | `community_join_requests` | Worker log `NotificationConsumer: JoinRequested` — **RequestId == community_join_requests.id** |

### 6.3 SQL to watch (DB `db52197`)
```sql
SELECT COUNT(*) AS pending FROM outbox_message;                 -- spikes then drains (~QueryDelay)
SELECT TOP 20 * FROM outbox_message ORDER BY sequence_number DESC;
SELECT TOP 5 id,status,upvote_count,score,published_on FROM posts ORDER BY created_on DESC;
SELECT TOP 5 * FROM post_votes              ORDER BY created_on DESC;
SELECT TOP 5 * FROM community_join_requests ORDER BY created_on DESC;
SELECT TOP 20 * FROM user_notifications     ORDER BY created_on DESC;   -- downstream consumer output
```

### 6.4 Crash‑safety (the headline guarantee)
1. `docker stop cce-rabbit`
2. `POST /posts/{id}/vote` → API still returns **200**.
3. `SELECT COUNT(*) FROM outbox_message;` → row **persists** (broker down). *(0 here ⇒ interceptor/outbox wiring broken.)*
4. `docker start cce-rabbit` → row drains within ~1–2s; RabbitMQ UI shows traffic; Worker logs `VoteConsumer`.

### 6.5 No‑duplicate realtime
RabbitMQ UI (`localhost:15672`, cce/cce) → **Queues**: one per consumer (`feed`, `ranking`, `signal-r`,
`vote`, `notification`). Cast a vote → `vote` queue +1, and a SignalR client on group `post:{id}` receives
**exactly one** `VoteChanged`.

### 6.6 Automated (broker‑free) coverage
- `tests/CCE.Infrastructure.Tests/Messaging/CommunityIntegrationEventConsumerHarnessTests.cs` — in‑memory
  MassTransit harness: VoteCreated→VoteConsumer (Redis only), PostCreated→NotificationConsumer (follower
  fan‑out + `Locale` round‑trip), PostCreated→SignalRConsumer (NewPost to community+topic).
- `NotificationMessageConsumerHarnessTests.cs` — NotificationMessage→gateway round‑trip.
- `dotnet test tests/CCE.Domain.Tests` — aggregate methods + event raising.
- `dotnet test tests/CCE.ArchitectureTests` — `Application_does_not_depend_on_Infrastructure` (no bus leak).

---

## 7. The journey — bugs found & fixed

1. **Domain‑event dispatch silently detached (blocker).** `AddInterceptors(sp.GetServices<IInterceptor>())`
   combined with concrete‑only registration meant `DomainEventDispatcher` never attached → *no* domain
   events fired and audit columns stopped writing. **Fix:** also register the interceptors as `IInterceptor`.
2. **Integration events published after `SaveChanges`** in `JoinCommunity`/`FollowUser`/`UnfollowUser` →
   outbox row never persisted → lost messages. **Fix:** raise the event on the aggregate (pre‑commit).
3. **Three competing emission patterns** (clean bridge for PostCreated; inline‑pre‑save for Vote/Reply;
   broken inline‑post‑save for the rest). **Fix:** unified on the bridge pattern; command handlers no
   longer inject `IIntegrationEventPublisher`.
4. **Duplicate realtime**: votes pushed by both the API and `VoteConsumer`. **Fix:** API owns the push;
   `VoteConsumer` keeps only the Redis counter.
5. **Dead code**: `UserFollowed`/`UserUnfollowed`/`ResourcePublished` integration events had no consumers;
   `CommunityJoinRequested` used a random `Guid` instead of the real request id; PostCreated notification
   fan‑out ran on the API thread. **Fix:** removed dead contracts; carry the real id; moved fan‑out to the
   Worker (`NotificationConsumer`); deleted `PostCreatedNotificationHandler`.
6. **`MassTransitIntegrationEventPublisher` referenced a non‑existent `IScopedBusContextProvider<>`** —
   the branch didn't compile. **Fix:** inject `IPublishEndpoint` (outbox‑aware in scope), matching
   `MassTransitNotificationMessageDispatcher`.

---

## 8. Gaps & follow‑ups

- **Idempotent consume not enabled.** Delivery is at‑least‑once; a redelivery can double‑process (e.g. a
  duplicate notification). `inbox_state` exists — enable `UseInbox` per consumer, or make consumers
  idempotent (dedupe on a natural key).
- **Live E2E not yet run.** §6.4 (crash‑safety) and §6.5 (no‑dup realtime) need a real RabbitMQ + SQL
  Server pass; only the broker‑free harness tests run in CI today.
- **`CCE.Application.Tests` is broken (pre‑existing).** It references `Post.Create` and stale handler
  constructors that no longer exist and cannot compile; command‑handler unit coverage is currently absent.
- **Arch rule `Application_does_not_depend_on_EntityFrameworkCore` fails (pre‑existing).** ~20 content/query
  handlers (and the Follow/Unfollow handlers) use EF directly in the Application layer.
- **No `docker-compose` for the broker yet.** §6.1 uses a raw `docker run`; a compose service would make it
  one command.
- **Follow/unfollow emit no events** (removed as dead). If "new post from someone you follow" or
  follow‑driven feed work is needed, re‑add via the bridge pattern **with a real consumer** — don't add a
  contract that nothing consumes.
- **Notification locale = post locale**, not the recipient's preference (pre‑existing behavior); revisit if
  per‑recipient localization is required.
- **`PostCreatedIntegrationEvent.IsExpert` is hardcoded `false`** at publish; `FeedConsumer` resolves the
  real expert/celebrity status from `ExpertProfile`/`FollowerCount` at consume time.
- **`VoteConsumer` debounce removed.** It previously coalesced SignalR pushes with a per‑process static
  dictionary; since it no longer pushes, that's moot — but if viral‑vote SignalR storms become an issue on
  the API push side, add debouncing there (ideally Redis‑backed for multi‑instance correctness).

---

## 9. Adding a new async event (checklist)

1. Add a domain‑event record under `CCE.Domain/<Module>/Events/` implementing `IDomainEvent`.
2. Raise it from a method on a tracked **aggregate root** (`AggregateRoot<Guid>`).
3. Add a POCO integration‑event contract under `CCE.Application/Common/Messaging/IntegrationEvents/`.
4. Add a one‑line `XBusPublisher : INotificationHandler<XEvent>` bridge in
   `CCE.Application/Notifications/Handlers/`.
5. Add an `IConsumer<XIntegrationEvent>` in `CCE.Infrastructure/Notifications/Messaging/Consumers/` and
   register it behind `registerConsumers` in `MessagingServiceExtensions`.
6. **Do not** ship an integration event without a consumer.
7. Add a harness test (`AddMassTransitTestHarness`) asserting the consumer receives it.
