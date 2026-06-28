# Implementation Plan — Notify Subscribers on News / Event / Resource Publish

**Status:** Draft for review
**Date:** 2026-06-13
**Author:** (review)

## 1. Goal

When an admin **publishes News**, **publishes a Resource**, or **schedules an Event**, notify the platform's **subscribers** across two channels:

- **Email** — to the **newsletter subscriber list** (`NewsletterSubscription`).
- **In‑app** — to subscribers who are also registered users.

Email must respect the user's notification settings where a user account exists ("email only if the user's setting supports email"). The newsletter list is the source of the email audience.

## 2. Current state (verified)

| Concern | Today | File |
|---|---|---|
| News publish event | `NewsPublishedEvent(NewsId, OccurredOn)` raised by `News.Publish(clock)` | `src/CCE.Domain/Content/News.cs:107`; event `…/Content/Events/NewsPublishedEvent.cs` |
| Resource publish event | `ResourcePublishedEvent(ResourceId, CountryId?, CategoryId, OccurredOn)` raised by `Resource.Publish(clock)` | `src/CCE.Domain/Content/Resource.cs:105` |
| Event schedule event | `EventScheduledEvent(EventId, StartsOn, EndsOn, OccurredOn)` raised by `Event.Schedule(...)` (at creation) | `src/CCE.Domain/Content/Event.cs:107` |
| News handler | Notifies **author only**, in‑app only, in‑process | `…/Notifications/Handlers/NewsPublishedNotificationHandler.cs` |
| Resource handler | Notifies **uploader only**, in‑app only, in‑process | `…/Notifications/Handlers/ResourcePublishedNotificationHandler.cs` |
| Event handler | **Stub** — logs only, dispatches nothing | `…/Notifications/Handlers/EventScheduledNotificationHandler.cs` |
| Newsletter list | `NewsletterSubscription` aggregate: `Email`, `LocalePreference`, `IsConfirmed`, `ConfirmationToken`, `ConfirmedOn`, `UnsubscribedOn`. Email‑only (no user FK). Double opt‑in. **No Application/API surface yet.** | `src/CCE.Domain/Content/NewsletterSubscription.cs`; `DbSet` at `CceDbContext.cs:57` |
| Async stack | `IIntegrationEventPublisher` → EF outbox (atomic w/ `SaveChanges`) → RabbitMQ → `CCE.Worker` hosts consumers → `NotificationConsumer` fan‑out → per‑recipient `NotificationMessage` → `NotificationMessageConsumer` → `NotificationGateway` | `…/Notifications/Messaging/MessagingServiceExtensions.cs`; `…/Consumers/NotificationConsumer.cs`; `src/CCE.Worker/Program.cs` |
| Settings semantics | Gateway loads `UserNotificationSettings`, calls `ShouldSend(settings)` per channel; default when no row = **opt‑in (send)**. Email/InApp/SMS all `settings?.IsEnabled ?? true` | `NotificationGateway.cs:101‑112,169‑249`; `EmailNotificationChannelSender.cs:24` |
| Event types | `NewsPublished=4`, `ResourcePublished=5`, `EventScheduled=6` already defined | `src/CCE.Domain/Notifications/NotificationEventType.cs` |

**Implication:** the moving parts already exist. This feature is mostly *wiring* — add integration events, bus publishers, one consumer, an audience query, and email templates. **No new tables / migrations** (audience = the existing newsletter table; in‑app/email routing reuses the existing gateway).

## 3. Architecture decision — async (integration event → Worker consumer)

**Decision: use the async path.** Mirror `PostCreated`: a thin domain‑event handler publishes an integration event (captured by the EF outbox, atomic with the publish transaction); the `CCE.Worker` consumer resolves the audience and fans out.

### Why async beats the alternatives here

| Approach | How | Verdict |
|---|---|---|
| **A. In‑process MediatR handler fan‑out** (what News/Resource do now) | Domain‑event handler runs inside `DomainEventDispatcher.SavingChangesAsync`, queries subscribers, dispatches N notifications — all on the admin's publish request, pre‑commit | ❌ **Rejected for broadcast.** Fine for 1 recipient (the author); for a newsletter list of hundreds/thousands it blocks the admin HTTP request, does heavy I/O inside the save transaction, and a single failure risks the whole publish. No retry isolation. |
| **B. Async: integration event → Worker consumer fan‑out** | Handler only publishes a small integration event to the outbox (atomic, instant). Worker consumer does the fan‑out off the request thread; `NotificationMessageConsumer` retries per recipient (5s/15s/30s → error queue) | ✅ **Chosen.** Admin request returns immediately; fan‑out + provider I/O isolated in the Worker; reliable delivery via outbox + retry; **identical to the existing, proven `PostCreated` pattern** — no new infra. |
| **C. Bulk single email send** (one provider call with many recipients/BCC) | Consumer builds one bulk email instead of N per‑recipient sends | ⚠️ **Future optimization, not now.** Would diverge from the gateway/template/log model (per‑recipient logging, per‑user locale, in‑app rows). Revisit only if newsletter volume makes per‑recipient sends a cost/throughput problem (see §9). |

**Net:** Option B. It's the same shape as `PostCreatedIntegrationEvent → NotificationConsumer`, so it slots into existing registration, retry, and outbox machinery.

## 4. Audience & consent model (the key product decision)

Per the directive, the **newsletter list is the audience**. Concretely, per published item:

1. **Resolve confirmed subscribers:** `NewsletterSubscription` where `IsConfirmed == true && UnsubscribedOn == null`.
2. **Left‑join to `Users`** (active: `Status == Active && !IsDeleted`) on email → each recipient is `(Email, LocalePreference, UserId?)`.
3. **Dispatch one `NotificationMessage` per recipient:**
   - **Matched user** → `RecipientUserId = user.Id`, `Channels = [InApp, Email]`, locale = the user's `LocalePreference`. The gateway then sends in‑app and email **subject to that user's `UserNotificationSettings`** → satisfies "email only if the user's setting supports email," and the user gets an in‑app row too.
   - **Unmatched email** (newsletter‑only, no account) → `RecipientUserId = null`, `Email = sub.Email`, `Channels = [Email]`, locale = `sub.LocalePreference`. In‑app is auto‑skipped (gateway skips in‑app when recipient is null); no settings row exists → email sends (opt‑in default).
4. **Exclude the author/uploader** from the broadcast set (they're notified by the existing in‑process handler — see §5.4) to avoid a double in‑app.

`BypassSettings` stays **false** so user email settings win. 

> **Open decision (consent precedence):** if a user opted into the newsletter but disabled email in `UserNotificationSettings`, the above lets the *user setting* win (no email). If instead newsletter consent should override, set `BypassSettings = true` for the email channel of matched users (requires splitting the matched‑user dispatch into a `[InApp]` request + a `[Email]` `BypassSettings` request). **Recommend: user setting wins (default, simplest).** Confirm.

> **Known limitation:** because the newsletter list is email‑only, **in‑app reaches only subscribers who also have accounts**. Registered users who never joined the newsletter get nothing. If in‑app should go to *all* active users regardless of newsletter, that's the "broadcast" variant — out of scope for this plan; note for a later phase.

> **Prerequisite gap:** there is **no API to subscribe/confirm/unsubscribe** to the newsletter yet (domain + table only). This feature will send to whatever rows exist; if the list is empty, nothing sends. Building the subscribe flow is tracked separately (§10).

## 5. Detailed design

### 5.1 New integration events
`src/CCE.Application/Common/Messaging/IntegrationEvents/`

```csharp
public sealed record NewsPublishedIntegrationEvent(
    Guid NewsId, Guid TopicId, Guid AuthorId, DateTimeOffset PublishedOn);

public sealed record ResourcePublishedIntegrationEvent(
    Guid ResourceId, Guid CategoryId, Guid? CountryId, Guid UploadedById, DateTimeOffset PublishedOn);

public sealed record EventScheduledIntegrationEvent(
    Guid EventId, Guid TopicId, DateTimeOffset StartsOn, DateTimeOffset EndsOn, DateTimeOffset OccurredOn);
```
(IDs only — the consumer loads localized titles for template variables; keeps events small and avoids stale data.)

### 5.2 New bus publishers (domain‑event → integration event)
`src/CCE.Application/Content/EventHandlers/` (matches the `Content/EventHandlers/` + `Community/EventHandlers/` convention)

- `NewsPublishedBusPublisher : INotificationHandler<NewsPublishedEvent>` → publishes `NewsPublishedIntegrationEvent`.
- `ResourcePublishedBusPublisher : INotificationHandler<ResourcePublishedEvent>` → publishes `ResourcePublishedIntegrationEvent`.
- `EventScheduledBusPublisher : INotificationHandler<EventScheduledEvent>` → publishes `EventScheduledIntegrationEvent`.

Each is a thin handler taking `IIntegrationEventPublisher` (same shape as `PostCreatedBusPublisher`). Runs pre‑commit in `DomainEventDispatcher`, so the publish is captured by the outbox atomically with the publish transaction.

> The `News`/`Resource` domain events carry only the IDs the publisher needs. `Event` does not carry `TopicId`/`AuthorId` in `EventScheduledEvent` — the publisher will load the `Event` aggregate by `EventId` (a read; safe pre‑commit) to populate `TopicId`, **or** we extend `EventScheduledEvent` to include `TopicId`. **Recommend extending the event** (cheaper than a load). Same check for News `AuthorId`/`TopicId` (already on `NewsPublishedEvent`? No — only `NewsId`; either load News or extend the event — recommend extend).

### 5.3 New Worker consumer
`src/CCE.Infrastructure/Notifications/Messaging/Consumers/ContentNotificationConsumer.cs`
Implements `IConsumer<NewsPublishedIntegrationEvent>`, `IConsumer<ResourcePublishedIntegrationEvent>`, `IConsumer<EventScheduledIntegrationEvent>`.

Per message:
1. Load localized title(s) for variables via the audience/read service (§5.5).
2. Resolve recipients via the audience service (§4, §5.5).
3. For each recipient, `await _dispatcher.DispatchAsync(new NotificationMessage(...))` with the right `TemplateCode`, `EventType`, channels, locale, and `MetaData` (title, id/slug, and for events the start date).
4. Log dispatched count (mirror `NotificationConsumer`).

Plus a `ContentNotificationConsumerDefinition` with the same retry policy as `NotificationConsumerDefinition` (concurrency limit + 5s/15s/30s retries).

### 5.4 Existing in‑process handlers (author/uploader)
Keep `NewsPublishedNotificationHandler` and `ResourcePublishedNotificationHandler` as‑is — they notify the **author/uploader** in‑app (a distinct recipient/intent). Replace the **`EventScheduledNotificationHandler` stub** — either delete it (no author notion needed) or repoint it; the broadcast now comes from the consumer. Net: each domain event has two `INotificationHandler`s — the existing author‑notifier (in‑process) and the new bus‑publisher (async broadcast). The consumer excludes the author from the broadcast set to prevent a double in‑app.

### 5.5 Audience / read service
New `IContentAudienceReadService` (Application) + impl in Infrastructure using `CceDbContext` directly (same approach as `CommunityReadService`):

```csharp
Task<IReadOnlyList<ContentSubscriber>> GetConfirmedSubscribersAsync(Guid? excludeUserId, CancellationToken ct);
// ContentSubscriber(string Email, string Locale, Guid? UserId)

Task<ContentTitle?> GetNewsTitleAsync(Guid newsId, CancellationToken ct);     // (TitleAr, TitleEn)
Task<ContentTitle?> GetResourceTitleAsync(Guid resourceId, CancellationToken ct);
Task<ContentTitle?> GetEventTitleAsync(Guid eventId, CancellationToken ct);
```
`GetConfirmedSubscribersAsync` query: `NewsletterSubscriptions.Where(IsConfirmed && UnsubscribedOn == null)` left‑joined to `Users.Where(Active && !IsDeleted)` on normalized email; project to `(Email, COALESCE(user.LocalePreference, sub.LocalePreference), user.Id?)`, excluding `excludeUserId`.

### 5.6 Templates (seeder)
Extend `NotificationTemplateSeeder` (added previously) so each of the three codes exists for **both InApp and Email**:

- `NEWS_PUBLISHED` — add **Email** variant (InApp already seeded).
- `RESOURCE_PUBLISHED` — add **Email** variant (InApp already seeded).
- `EVENT_SCHEDULED` — add **InApp + Email** (currently none).

Use placeholders the consumer supplies, e.g. `{{Title}}` (and `{{StartsOn}}` for events). Bilingual ar/en. Idempotent via deterministic IDs (existing pattern).

### 5.7 Registration
- Register the three integration events' consumer + definition in `MessagingServiceExtensions.cs` inside the `registerConsumers` branch (alongside `NotificationConsumer`).
- Bus publishers and the read service are auto‑discovered by MediatR assembly scan / added to `DependencyInjection.cs` (read service is a normal DI registration).
- Confirm async dispatch is on in the target env (`Messaging:UseAsyncDispatcher=true`, RabbitMQ transport) so dispatch goes through the bus; otherwise the in‑process dispatcher still works but without Worker isolation.

## 6. Implementation steps (phased)

**Phase 1 — Plumbing (no behavior change)**
1. Add the 3 integration events (§5.1).
2. (If chosen) extend `NewsPublishedEvent` / `EventScheduledEvent` with `TopicId`/`AuthorId` as needed (§5.2 note).
3. Add the 3 bus publishers (§5.2).
4. Add `IContentAudienceReadService` + impl (§5.5).

**Phase 2 — Consumer & templates**
5. Add `ContentNotificationConsumer` + `ContentNotificationConsumerDefinition` (§5.3); register in `MessagingServiceExtensions` (§5.7).
6. Extend `NotificationTemplateSeeder` with Email variants + Event templates (§5.6); run seeder.
7. Replace the `EventScheduledNotificationHandler` stub (§5.4).

**Phase 3 — Verify & roll out**
8. Tests (§7).
9. Enable async dispatch + RabbitMQ in staging; publish sample content; confirm fan‑out, logs, and per‑user email gating.

## 7. Testing

- **Domain/unit:** bus publishers publish the correct integration event with correct fields (mock `IIntegrationEventPublisher`).
- **Consumer unit:** given a fake subscriber set (mix of matched users + newsletter‑only emails, plus the author), asserts: one dispatch per recipient; matched users get `[InApp, Email]`; newsletter‑only get `[Email]` with `RecipientUserId == null`; author excluded; correct locale and `MetaData`.
- **Audience query:** integration test for `GetConfirmedSubscribersAsync` — excludes unconfirmed/unsubscribed, dedups, joins users by email, applies active filter.
- **Settings gating:** matched user with email disabled in `UserNotificationSettings` → email skipped, in‑app still sent (verifies the §4 consent rule).
- **Template coverage:** assert every dispatched `TemplateCode × Channel` (`NEWS_PUBLISHED`/`RESOURCE_PUBLISHED`/`EVENT_SCHEDULED` × InApp+Email) has a seeded active template (this is the guard test recommended earlier — extend it here).
- **Outbox/atomicity:** publishing content that then rolls back does not emit the integration event (publish captured in the same transaction).

## 8. Files to add / change

**Add**
- `src/CCE.Application/Common/Messaging/IntegrationEvents/NewsPublishedIntegrationEvent.cs`
- `…/ResourcePublishedIntegrationEvent.cs`, `…/EventScheduledIntegrationEvent.cs`
- `src/CCE.Application/Content/EventHandlers/NewsPublishedBusPublisher.cs` (+ Resource, + Event)
- `src/CCE.Application/Content/IContentAudienceReadService.cs`
- `src/CCE.Infrastructure/Content/ContentAudienceReadService.cs`
- `src/CCE.Infrastructure/Notifications/Messaging/Consumers/ContentNotificationConsumer.cs`
- `…/Consumers/ContentNotificationConsumerDefinition.cs`

**Change**
- `src/CCE.Infrastructure/Notifications/Messaging/MessagingServiceExtensions.cs` — register consumer + definition.
- `src/CCE.Infrastructure/DependencyInjection.cs` — register `IContentAudienceReadService`.
- `src/CCE.Seeder/Seeders/NotificationTemplateSeeder.cs` — Email variants + Event templates.
- `src/CCE.Application/Notifications/Handlers/EventScheduledNotificationHandler.cs` — remove/replace stub.
- (Optional) `src/CCE.Domain/Content/Events/NewsPublishedEvent.cs`, `EventScheduledEvent.cs` — add `TopicId`/`AuthorId`.

**No migration required** (audience uses the existing `newsletter_subscriptions` table; no schema change).

## 9. Edge cases & scaling notes

- **Large lists:** N per‑recipient messages per publish. Matches the existing pattern and gives per‑recipient retry/logging, but at high volume consider (a) chunked dispatch, or (b) Option C bulk email (§3). Log the recipient count; **do not silently cap**.
- **Duplicate emails / re‑subscribes:** dedup by normalized email in the audience query.
- **Email↔user match:** join on normalized email; a newsletter email that matches an inactive/deleted user → treat as newsletter‑only (email, no in‑app).
- **Unpublish/edit:** only `Publish`/`Schedule` triggers; edits do not re‑notify (by design).
- **Idempotency:** if a publish event is delivered twice (bus at‑least‑once), recipients could be notified twice. The outbox + consumer is at‑least‑once; acceptable for notifications, or add a dedup key per `(contentId, recipient)` if duplicates must be prevented.

## 10. Out of scope / follow‑ups

- Newsletter **subscribe / confirm / unsubscribe** API + endpoints (domain exists, no surface yet) — required for the list to actually populate.
- In‑app broadcast to **all** active users (not just newsletter subscribers).
- Interest/topic‑targeted audiences (use `TopicFollow` / `UserInterestTopic`) — a more granular phase‑2 audience model.
- Bulk‑email transport optimization (Option C).

## 11. Open decisions (need confirmation)

1. **Consent precedence** (§4): user email setting wins (recommended) vs newsletter consent overrides.
2. **Event/News domain‑event enrichment** (§5.2): extend the domain events with `TopicId`/`AuthorId` (recommended) vs load aggregate in the publisher.
3. **In‑app scope** (§4 limitation): accept "in‑app only for newsletter subscribers with accounts," or expand to all active users in a later phase.
