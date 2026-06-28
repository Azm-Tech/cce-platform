# Plan: RabbitMQ + MassTransit for reliable async event handling

## Context

Today the solution dispatches **domain events in-process and synchronously**. `DomainEventDispatcher`
(`src/CCE.Infrastructure/Persistence/Interceptors/DomainEventDispatcher.cs`) drains domain events in
EF's **`SavedChangesAsync` (post-commit)** and pushes them straight through MediatR's `IPublisher`.
The only thing that ever reaches a message bus is `NotificationMessage`, and even that runs on the
**InMemory** transport — there is no real broker anywhere (`Messaging:Transport = "InMemory"` in every
appsettings).

Two consequences:
- **No durability / dual-write risk.** Because the bus publish happens *after* the DB transaction
  commits and *off* that transaction, a crash between commit and publish silently loses the message.
- **Only notifications are async.** There is no general way to react to a domain event in the
  background or in another process.

This plan, per the chosen direction, will: **(1) stand up a real RabbitMQ broker and activate the
RabbitMQ transport; (2) generalize the bus to carry arbitrary _integration events_, not just
notifications; (3) move all consumers into a new dedicated `CCE.Worker` service so the APIs only
publish; and (4) add the MassTransit EF Core transactional outbox so a message is staged in the same
SQL transaction as the aggregate and relayed reliably afterward.**

The existing pieces are kept and extended — `AddCceMessaging`, `MessagingOptions`,
`MassTransitNotificationMessageDispatcher`, `NotificationMessageConsumer(+Definition)` all stay; the
InMemory transport remains the default for dev/test.

---

## Architecture (target)

```
API (External / Internal)                         CCE.Worker (NEW)
─────────────────────────                         ─────────────────────────
Command handler mutates aggregate                 Hosts ALL consumers:
   → domain event raised                            • NotificationMessageConsumer
DomainEventDispatcher (SavingChangesAsync, PRE-commit)  • <future integration-event consumers>
   → in-process MediatR handlers                  Runs MassTransit BusOutboxDeliveryService
   → handler calls IIntegrationEventPublisher       → reads OutboxMessage table
        → MassTransit bus-outbox captures msg        → publishes to RabbitMQ
        → staged as OutboxMessage row              RabbitMQ delivers → consumer → INotificationGateway etc.
SaveChanges commits aggregate + outbox row ATOMICALLY
```

Key rule: **APIs publish only; the Worker consumes.** Both enable the outbox; only the Worker runs the
delivery service + receive endpoints.

---

## Work items

### 1. Packages (`Directory.Packages.props`)
- Add `MassTransit.EntityFrameworkCore` (pin **8.3.7**, matching the existing MassTransit pins at lines 113–119).
- Add `AspNetCore.HealthChecks.Rabbitmq` (for the broker health check; pick the version aligned with the existing HealthChecks packages).
- No new references needed for `MassTransit` / `MassTransit.RabbitMQ` — already referenced by `CCE.Infrastructure.csproj`.

### 2. Integration-event contracts + publisher abstraction (`CCE.Application`)
- New folder `src/CCE.Application/Common/Messaging/`:
  - `IIntegrationEventPublisher` — thin interface `Task PublishAsync<T>(T evt, CancellationToken ct) where T : class`. Keeps MassTransit out of Application (mirrors how `INotificationMessageDispatcher` already abstracts the bus).
  - `IntegrationEvents/` — POCO `record` contracts (no MassTransit attributes), one per async event we want to carry. Seed it with the first real one migrated off the in-process-only path; `NotificationMessage` (already in `CCE.Application.Notifications.Messages`) stays where it is.
- **Architecture-test safety:** contracts/interface are plain POCOs, so `CCE.Application` gains **no** dependency on MassTransit — keeps the NetArchTest rules green.

### 3. Infrastructure messaging wiring (`src/CCE.Infrastructure/Notifications/Messaging/`)
- New `MassTransitIntegrationEventPublisher : IIntegrationEventPublisher` wrapping `IPublishEndpoint` (sibling of the existing `MassTransitNotificationMessageDispatcher`). Register in `DependencyInjection.cs`.
- Rework `MessagingServiceExtensions.AddCceMessaging` (currently registers the consumer unconditionally):
  - Add overload/param `bool registerConsumers` (default `false`). **APIs call with `false`** (publish-only); **Worker calls with `true`**.
  - Add the EF outbox inside `AddMassTransit(x => …)`:
    ```csharp
    x.AddEntityFrameworkOutbox<CceDbContext>(o =>
    {
        o.UseSqlServer();
        o.UseBusOutbox();           // capture Publish/Send into OutboxMessage, relay after SaveChanges
    });
    ```
  - Only when `registerConsumers`: `x.AddConsumer<NotificationMessageConsumer, NotificationMessageConsumerDefinition>();` (+ future consumers) and let `ConfigureEndpoints` build receive endpoints. (The `BusOutboxDeliveryService` is hosted automatically by `UseBusOutbox`; it must run where SQL is reachable — fine in both API and Worker, but receive endpoints only exist in the Worker.)
  - RabbitMQ block: keep credentials out of the URI — add `RabbitMqUsername`/`RabbitMqPassword` to `MessagingOptions` and set them in `cfg.Host(host, vhost, h => { h.Username(...); h.Password(...); })`. Add a kebab-case `SetKebabCaseEndpointNameFormatter()` and a global `UseMessageRetry`/circuit-breaker (the per-consumer retry in `NotificationMessageConsumerDefinition` stays).
  - Keep the existing InMemory branch as the default; the `UseAsyncDispatcher` swap logic stays unchanged.

### 4. Make domain-event dispatch transactional (`DomainEventDispatcher.cs`)
- **Move the dispatch loop from `SavedChangesAsync` (post-commit) to `SavingChangesAsync` (pre-commit).** This is the linchpin of outbox correctness: when an in-process handler calls `IIntegrationEventPublisher.PublishAsync`, the bus-outbox adds an `OutboxMessage` entity to the tracked `CceDbContext`, and that row is then persisted by the **same** `SaveChanges` that commits the aggregate — atomic, no dual write.
- Behavioral note to validate: handlers now run before the INSERT/UPDATE SQL (entities are already tracked, so reads of the mutated aggregate are fine). The doc comment referencing "Outbox is sub-project 8 work" gets updated.

### 5. EF migration for outbox tables
- In `CceDbContext.OnModelCreating`, add `modelBuilder.AddInboxStateEntity(); AddOutboxStateEntity(); AddOutboxMessageEntity();` (snake_case naming convention will name the columns).
- Generate `dotnet ef migrations add AddMassTransitOutbox --project src/CCE.Infrastructure --startup-project src/CCE.Infrastructure`. The **`CCE.Seeder`** continues to be the canonical migration applier (no change to seed order).

### 6. New `CCE.Worker` project (hosts consumers)
- `src/CCE.Worker/CCE.Worker.csproj` — references `CCE.Application`, `CCE.Domain`, `CCE.Infrastructure`, and **`CCE.Api.Common`** (to reuse Serilog, `AddCceOpenTelemetry`, `AddCceHealthChecks`). Use `WebApplication` as the host (not bare Worker SDK) so it can reuse those ASP.NET-based extensions and expose `/health` — it maps **no business endpoints**, only health + the MassTransit hosted services.
- `Program.cs`: `AddInfrastructure(config)` → then `AddCceMessaging(config, registerConsumers: true)` (or have the Worker pass the flag). Add Serilog + `AddCceOpenTelemetry(config, "CCE.Worker")` + `AddCceHealthChecks`.
- Add `appsettings.json` / `appsettings.Development.json` mirroring the API `Infrastructure` + `Messaging` sections. Dev defaults to `Transport: InMemory` (so the Worker is a no-op locally unless RabbitMQ is on); Production sets `RabbitMQ`.
- `Dockerfile` modeled on `src/CCE.Api.External/Dockerfile`.
- Add the project to `CCE.sln`.
- Since the Worker now owns consumers, the APIs' `AddCceMessaging(..., registerConsumers: false)` means `NotificationMessageConsumer` no longer runs in-process there — confirm the API still **publishes** notifications via the outbox (it does: dispatcher → `IPublishEndpoint` → outbox).

### 7. Config + secrets
- Extend `MessagingOptions` with `RabbitMqUsername`, `RabbitMqPassword` (nullable; required only when `Transport=RabbitMQ`).
- `appsettings.Production.json` (both APIs + Worker): `Transport: "RabbitMQ"`, `RabbitMqHost`, `RabbitMqVirtualHost: "/cce-prod"`. Real credentials supplied via env vars (`Messaging__RabbitMqUsername`, `Messaging__RabbitMqPassword`) — never committed.
- Dev/test stay `InMemory`; integration tests keep `UseAsyncDispatcher=false` per the existing guide §6.
- Dev sets `FallbackToInMemoryIfUnavailable: true` (see item 12); production leaves it `false`.

### 8. Local broker — `backend/docker-compose.yml`
- No compose file exists today (only Dockerfiles). Add one that brings up at least **`rabbitmq:3-management`** (ports 5672 + 15672, with a default `cce` user/pass), so devs can flip `Transport=RabbitMQ` locally and watch the management UI. Optionally fold in sql/redis/meilisearch/the-worker for a one-command stack.

### 9. Observability + health
- `OpenTelemetryExtensions.cs`: add `.AddSource("MassTransit")` to the tracing builder so publish/consume spans flow to Seq (MassTransit ships its own `ActivitySource`).
- `CceHealthChecksRegistration.cs`: when `Messaging:Transport == "RabbitMQ"`, add `.AddRabbitMQ(...)` tagged `ready`.

### 10. Tests
- New unit test in `tests/CCE.Infrastructure.Tests` (or a messaging test project) using `MassTransit.Testing` `InMemoryTestHarness` (`MassTransit.Testing.Helpers` is already pinned): assert publishing an integration event is consumed by its consumer.
- Re-run architecture tests to confirm `CCE.Application` still has no MassTransit dependency.
- Validate the `SavingChangesAsync` relocation against the domain tests + a build (note: `CCE.Application.Tests` is pre-existingly broken — rely on `CCE.Domain.Tests` + green build).

### 11. Docs
- Update `docs/masstransit-messaging-guide.md`: new Worker topology, outbox flow, integration-event contract pattern, and the "consumers run only in the Worker" rule.

### 12. Dev fallback — InMemory when RabbitMQ is unavailable
**Why:** the current dev/server environment has no RabbitMQ installed, so requesting `Transport=RabbitMQ` there must not break startup or message handling.

**Important framing:** MassTransit chooses its transport **once, when the bus is built** — there is no built-in runtime failover from RabbitMQ→InMemory. Also note that with the outbox in place, a *transient* broker outage in production does **not** need a fallback: the host still starts, MassTransit auto-reconnects in the background, and messages sit durably in `outbox_message` until the broker returns. So the fallback below is a **dev-only convenience for environments where the broker is entirely absent**, not a production resilience mechanism.

- Add `MessagingOptions.FallbackToInMemoryIfUnavailable` (default **`false`**). Set **`true`** only in `appsettings.Development.json` (both APIs + Worker).
- In `AddCceMessaging`, when `Transport=RabbitMQ` **and** the flag is `true`, run a **fast startup connectivity probe** (open an AMQP connection / TCP connect to the host with a short ~2s timeout). On failure: `log.LogWarning(...)` and **transparently take the existing InMemory branch** instead of `UsingRabbitMq`.
- **Consumer placement under fallback:** an InMemory bus is per-process, so the API's in-memory bus can't reach the Worker's consumers. When the fallback engages, **force `registerConsumers = true` in the falling-back host** so messages are consumed in-process (restores today's single-process dev behavior). This only applies to the InMemory fallback path; the real RabbitMQ path keeps publish-only APIs + Worker-only consumers.
- The **bus outbox stays enabled** on the InMemory path too (it works fine and keeps the code path identical) — messages flow `outbox_message` → in-memory bus → in-process consumer.
- **Production stays `false`** so a broker problem is never silently masked; durability is provided by the outbox + auto-reconnect, and `/health/ready` (item 9) surfaces a real RabbitMQ outage.

Decision summary:

| Env | `Transport` | `FallbackToInMemoryIfUnavailable` | Effective behavior |
|---|---|---|---|
| Dev (no broker) | `RabbitMQ` (or `InMemory`) | `true` | Probe fails → InMemory + in-process consumers. One process, no broker needed. |
| Dev (broker via compose) | `RabbitMQ` | `true` | Probe succeeds → real RabbitMQ + Worker consumers. |
| Production | `RabbitMQ` | `false` | Always RabbitMQ; outbox retains messages through outages; health check reports broker state. |

---

## Files touched (representative)

| Area | Path |
|---|---|
| Packages | `Directory.Packages.props` |
| Contracts/abstraction | `src/CCE.Application/Common/Messaging/IIntegrationEventPublisher.cs`, `.../IntegrationEvents/*.cs` |
| Bus wiring | `src/CCE.Infrastructure/Notifications/Messaging/MessagingServiceExtensions.cs`, `MessagingOptions.cs`, new `MassTransitIntegrationEventPublisher.cs` |
| DI | `src/CCE.Infrastructure/DependencyInjection.cs` |
| Transactional dispatch | `src/CCE.Infrastructure/Persistence/Interceptors/DomainEventDispatcher.cs` |
| DbContext + migration | `src/CCE.Infrastructure/Persistence/CceDbContext.cs` + new `Migrations/*_AddMassTransitOutbox.cs` |
| New service | `src/CCE.Worker/**`, `CCE.sln` |
| Observability/health | `src/CCE.Api.Common/Observability/OpenTelemetryExtensions.cs`, `src/CCE.Api.Common/Health/CceHealthChecksRegistration.cs` |
| Config | `appsettings*.json` for both APIs + Worker; new `backend/docker-compose.yml` |
| Docs | `docs/masstransit-messaging-guide.md` |

---

## Verification (end-to-end)

1. **Build (gate):** `dotnet build CCE.sln` — must pass with warnings-as-errors.
2. **Migration:** set `$env:CCE_DESIGN_SQL_CONN`, run `dotnet ef database update …`; confirm `outbox_message`, `outbox_state`, `inbox_state` tables exist.
3. **Broker up:** `docker compose -f backend/docker-compose.yml up -d rabbitmq`; open management UI at `http://localhost:15672` (cce/cce).
4. **Run with RabbitMQ:** set `Messaging__Transport=RabbitMQ` (+ host/creds) and launch an API plus `dotnet run --project src/CCE.Worker`.
5. **Trigger an event:** perform an action that raises a domain event whose handler publishes a notification (e.g. publish a resource via the Internal API). Observe:
   - a row briefly appears in `outbox_message` then drains,
   - a message flows through the RabbitMQ queue (visible in the mgmt UI),
   - the Worker logs `Consuming NotificationMessage …` and the gateway is invoked.
6. **Crash-safety spot check:** stop RabbitMQ, trigger the action — the API still returns 200 and the `outbox_message` row persists; restart RabbitMQ and confirm the delivery service relays it.
7. **Dev fallback check:** with **no broker running**, set `Messaging__Transport=RabbitMQ` and `Messaging__FallbackToInMemoryIfUnavailable=true`, then start an API alone (no Worker). Confirm: startup logs the "RabbitMQ unavailable — falling back to InMemory" warning, the host starts cleanly, and triggering an event is consumed **in-process** (notification handled). Then set the flag to `false` and confirm the broker outage instead surfaces via `/health/ready`.
8. **Tests:** `dotnet test tests/CCE.Domain.Tests` and the new MassTransit harness test; run `CCE.ArchitectureTests`.

## Open / low-risk follow-ups (not in this plan)
- Consumer-side **inbox** (idempotent consume) — the tables are added now; enabling `UseInbox` per-consumer can come later.
- Migrating additional in-process handlers to integration events as needs arise.
