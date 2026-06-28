# Review — Notification System

> Format: each item is a **Bug** (what's wrong + where) followed by a **Fix** (what to do).
> Severity legend: 🔴 confirmed bug · 🟠 inconsistency / gap · 🟡 hardening.

The core infrastructure (gateway, channel senders, template renderer, repositories, SignalR publisher, MassTransit dispatch/consumer) is architecturally sound. The gaps below are about completeness and consistency, not the core design.

> **Status (2026-06-13):** #3 (template seeding), #4 (move bus publishers), and #6 (channel exception isolation) are **FIXED**. #5 (rename) is **declined** — `MetaData` is the team's preferred name. #1/#2 (audiences) remain open as documented below.

---

## 1. 🟠 Event types defined but never dispatched

**Bug**
`NotificationEventType` declares ~17 values, but dispatch handlers exist for only ~6. There is **no dispatch path** for:
`EventScheduled`, `CommunityPostCreated/Replied/Voted`, `TopicNewPost`, `CommunityNewPost`, `UserMentioned`, `CommunityJoinApproved`, `AdminAccountCreated`, `CountryContentSubmitted`.
Today, scheduling an event or performing these community actions produces **no notification**.

**Fix**
Decide intent per value:
- If planned-but-not-built → keep, but mark clearly (XML doc / `// TODO`) and track.
- If not needed → remove from the enum to avoid implying coverage.
Then implement handlers for the ones that are in-scope.

---

## 2. 🟠 `EventScheduledNotificationHandler` is a stub

**Bug**
`Application/Notifications/Handlers/EventScheduledNotificationHandler.cs` only logs ("audience notifications require explicit audience definition") and never calls the dispatcher. The event type exists, but no notification is ever sent.

**Fix**
Either implement audience resolution and dispatch, or remove the handler + enum value until the feature is scoped. Don't leave a silent no-op wired into MediatR.

---

## 3. ✅ FIXED — No notification template seed data

**Bug**
Handlers/consumers/services dispatch template codes but no seeder/migration created the corresponding `NotificationTemplate` rows. A missing template makes the gateway log "No active template found for channel X" and skip delivery.

**Fix (done)**
Added `src/CCE.Seeder/Seeders/NotificationTemplateSeeder.cs` (Order 45, registered in `Program.cs`) — idempotent via deterministic IDs (`notif_template:{code}:{channel}`), bilingual ar/en content. Covers **every** dispatched code × channel found in the codebase:
`EXPERT_REQUEST_APPROVED` (InApp+Email), `EXPERT_REQUEST_REJECTED` (InApp+Email), `COUNTRY_CONTENT_REQUEST_APPROVED` (InApp+Email), `COUNTRY_CONTENT_REQUEST_REJECTED` (InApp+Email), `COUNTRY_CONTENT_SUBMITTED` (InApp+Email), `NEWS_PUBLISHED` (InApp), `RESOURCE_PUBLISHED` (InApp), `COMMUNITY_POST_CREATED` (InApp), `POST_REPLIED` (InApp), `COMMUNITY_JOIN_REQUESTED` (InApp), `COMMUNITY_MENTION` (InApp), `OTP_VERIFICATION` (Email+Sms), `PASSWORD_RESET` (Email).

`VariableSchemaJson` is `"{}"` (no required vars) so a missing variable degrades to the literal placeholder rather than throwing. Copy is plain and meant to be edited. *Still open:* a test asserting every dispatched code has a seeded template.

---

## 4. ✅ FIXED — Bus publishers misplaced in the Notifications folder

**Bug**
`PostCreatedBusPublisher`, `ReplyCreatedBusPublisher`, `PostVotedBusPublisher`, `CommunityJoinRequestedBusPublisher` lived in `Application/Notifications/Handlers/`, but they are **integration-event bridges** (publish Community domain events to MassTransit), not user-notification senders.

**Fix (done)**
`git mv`d all four to `Application/Community/EventHandlers/` (matching the existing `Content/EventHandlers/` convention) and updated their namespace to `CCE.Application.Community.EventHandlers`. Also renamed `PostCreatedIntegrationEventHandler.cs` → `PostCreatedBusPublisher.cs` so the filename matches its class. No external references needed updating — MediatR discovers them via `RegisterServicesFromAssembly`. `Notifications/Handlers/` now holds only genuine notification handlers.

---

## 5. ⛔ DECLINED — Naming drift: `MetaData` vs `Variables`

**Bug**
`NotificationMessage.MetaData` and `NotificationDispatchRequest.Variables` both feed the same template-render dictionary. Two names for one concept.

**Decision**
Team prefers `MetaData` as the name on `NotificationMessage`. Left as-is. (If the drift is ever resolved, the direction is `Variables → MetaData`, not the reverse.)

---

## 6. ✅ FIXED — No exception isolation around channel handlers

**Bug**
In `NotificationGateway.DispatchChannelAsync`, a missing handler was logged and skipped, but if a registered handler **threw**, there was no try/catch — the exception bubbled up and could fail the entire multi-channel dispatch (e.g. an SMS gateway error killing in-app + email for the same message).

**Fix (done)**
Wrapped the `sender.SendAsync` call in try/catch (`NotificationGateway.cs`). On a non-cancellation exception it logs the error, marks the log `Failed`, and returns a `Failed` channel result so the loop continues with the remaining channels. `OperationCanceledException` is intentionally allowed to propagate. Localized `#pragma warning disable CA1031` with justification, matching the project's existing convention for deliberate broad catches.

---

## 7. 🟡 SignalR publish failure is fire-and-forget

**Bug**
`NotificationGateway` publishes to SignalR **after** `SaveChangesAsync`. If the publish fails, it's logged but the in-app row is already committed — the user has a persisted notification that never pushed in real time, with no retry/alert.

**Fix**
Acceptable as-is for now (the row is persisted and the client can poll), but consider a retry or a "needs-push" flag for reliability if real-time delivery is a hard requirement.

---

## Not a bug (verified, leaving as-is)

- **`UserNotificationRepository.MarkAllSentAsReadAsync` calling `SaveChangesAsync` internally** was flagged during exploration as a repository-pattern violation. It is **explicitly sanctioned** by `docs/plans/notification-gateway-refactor-implementation-plan.md:213` ("intentionally a direct bulk write"). Not a defect. (Minor: the plan suggested `ExecuteUpdateAsync`; the impl loads-then-iterates — cosmetic.)
- **`INotificationChannelHandler` taking `RenderedNotification`** instead of the plan's `NotificationContext` is a deliberate simplification, not a breaking divergence.
- **DI registration** of all channel handlers (multi-register) and dispatchers is correct.
