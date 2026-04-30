# Phase 07 — Notifications + Follows

> Parent: [`../2026-04-30-web-portal.md`](../2026-04-30-web-portal.md) · Spec: [`../../specs/2026-04-30-web-portal-design.md`](../../specs/2026-04-30-web-portal-design.md) §5 (`/api/me/notifications*`, `/api/me/follows*`)

**Phase goal:** Authenticated users see their notifications and manage their follow subscriptions. The header bell icon shows an unread count badge; a slide-in drawer lists notifications with mark-read + mark-all-read actions. The `/me/follows` page lists every topic/user/post the user follows. A `[cceFollow]` directive lets community pages drop a single-button follow toggle anywhere we render an entity.

**Tasks:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 06 closed (`27a1a18`).
- AuthService, authGuard, AccountApiService all in place.

---

## Endpoint coverage

| Endpoint | Method | Phase 07 surface | Auth |
|---|---|---|---|
| `/api/me/notifications` | GET (`?page=`, `?pageSize=`, `?status=`) | Task 7.2 (drawer + page) | ✗ |
| `/api/me/notifications/unread-count` | GET (returns `{ count }`) | Task 7.2 (header badge poll) | ✗ |
| `/api/me/notifications/{id}/mark-read` | POST (no body, 204) | Task 7.2 (drawer click) | ✗ |
| `/api/me/notifications/mark-all-read` | POST (returns `{ marked }`) | Task 7.2 (drawer button) | ✗ |
| `/api/me/follows` | GET (returns `MyFollowsDto`) | Task 7.4 (FollowsPage) | ✗ |
| `/api/me/follows/topics/{topicId}` | POST / DELETE | Task 7.5 (`[cceFollow]` directive) | ✗ |
| `/api/me/follows/users/{userId}` | POST / DELETE | Task 7.5 | ✗ |
| `/api/me/follows/posts/{postId}` | POST / DELETE | Task 7.5 | ✗ |

**Backend contract** (verified in `NotificationsEndpoints.cs`, `CommunityWriteEndpoints.cs`, `CommunityPublicEndpoints.cs`):

```csharp
// UserNotificationDto
public sealed record UserNotificationDto(
    Guid Id, Guid TemplateId,
    string RenderedSubjectAr, string RenderedSubjectEn,
    string RenderedBody,                 // already locale-resolved server-side
    string RenderedLocale,                // "ar" | "en"
    NotificationChannel Channel,          // enum string: "Email" | "Sms" | "InApp"
    DateTimeOffset? SentOn,
    DateTimeOffset? ReadOn,
    NotificationStatus Status);           // enum string: "Pending" | "Sent" | "Failed" | "Read"

// MyFollowsDto — three flat id lists
public sealed record MyFollowsDto(
    IReadOnlyList<Guid> TopicIds,
    IReadOnlyList<Guid> UserIds,
    IReadOnlyList<Guid> PostIds);

// Follow toggle endpoints all return Ok / NoContent with no body.
```

`/api/me/follows` returns three flat **id lists**, not hydrated entity DTOs — the FE must hydrate topics/posts/users via existing list endpoints (or use Phase 8's CommunityApiService). For the v0.1.0 FollowsPage we render id-bearing chips with a "View" link to the canonical detail route, plus an unfollow button per chip; full hydration (showing topic name, post title, user displayName) is a Phase 9 polish task.

The `mark-read` endpoint returns 204 (no body) on success and is **idempotent** — re-marking an already-read notification is a no-op server-side. Mark-all-read returns `{ marked: number }` where `marked` is the count actually transitioned.

## Hand-defined DTOs

```ts
// frontend/apps/web-portal/src/app/features/notifications/notification.types.ts
import type { PagedResult } from '../knowledge-center/shared.types';

export type NotificationStatus = 'Pending' | 'Sent' | 'Failed' | 'Read';
export type NotificationChannel = 'Email' | 'Sms' | 'InApp';

export interface UserNotification {
  id: string;
  templateId: string;
  renderedSubjectAr: string;
  renderedSubjectEn: string;
  renderedBody: string;
  renderedLocale: string;
  channel: NotificationChannel;
  sentOn: string | null;
  readOn: string | null;
  status: NotificationStatus;
}

export type { PagedResult };

// frontend/apps/web-portal/src/app/features/follows/follows.types.ts
export interface MyFollows {
  topicIds: string[];
  userIds: string[];
  postIds: string[];
}

export type FollowEntityType = 'topic' | 'user' | 'post';
```

## Folder structure

```
apps/web-portal/src/app/features/
├── notifications/
│   ├── notification.types.ts
│   ├── notifications-api.service.{ts,spec.ts}      # Task 7.1
│   ├── notification-row.component.{ts,scss}        # presentation card (inline template)
│   ├── notifications-drawer.component.{ts,html,scss,spec.ts}  # Task 7.2 (mat-sidenav)
│   └── routes.ts                                    # /me/notifications page (re-uses drawer body)
└── follows/
    ├── follows.types.ts
    ├── follows-api.service.{ts,spec.ts}            # Task 7.3
    ├── follows.page.{ts,html,scss,spec.ts}         # Task 7.4
    ├── follow.directive.{ts,spec.ts}               # Task 7.5 (`[cceFollow]`)
    └── routes.ts                                    # mounted at /me/follows
```

The bell icon + unread-count signal live in HeaderComponent (modified in Task 7.6).

---

## Task 7.1: NotificationsApiService + types

**Files (all new):**
- `features/notifications/notification.types.ts`
- `features/notifications/notifications-api.service.{ts,spec.ts}`

NotificationsApiService methods:
- `list({ page?, pageSize?, status? })` → `Result<PagedResult<UserNotification>>` (GET `/api/me/notifications`).
- `getUnreadCount()` → `Result<number>` (GET `/api/me/notifications/unread-count`, unwrap the `{ count }` envelope).
- `markRead(id)` → `Result<void>` (POST `/api/me/notifications/{id}/mark-read`, 204 → `{ ok: true, value: undefined }`).
- `markAllRead()` → `Result<number>` (POST `/api/me/notifications/mark-all-read`, returns `{ marked }`).

**Tests (~5):**
1. `list({ page: 2 })` GETs with `?page=2`.
2. `list({ status: 'Sent' })` adds `status=Sent`.
3. `getUnreadCount()` unwraps `{ count: 7 }` to `{ ok: true, value: 7 }`.
4. `markRead('n1')` POSTs `/api/me/notifications/n1/mark-read` with empty body.
5. `markAllRead()` POSTs and returns the `marked` count.

Commit: `feat(web-portal): NotificationsApiService + DTOs (Phase 7.1)`

---

## Task 7.2: Header bell + NotificationsDrawerComponent

**Files:**
- `features/notifications/notification-row.component.{ts,scss}`
- `features/notifications/notifications-drawer.component.{ts,html,scss,spec.ts}`
- Modify: `core/layout/header.component.{ts,html,scss}` — add the bell-icon button beside the user menu (only when authenticated) with an `unreadCount` signal badge; clicking opens the drawer.

NotificationRowComponent: presentation-only card. `input.required<UserNotification>`, `input<'ar'|'en'>('en')`. Renders subject (locale-driven), body (HTML-stripped, 200-char excerpt), sentOn date, channel badge (Email / Sms / InApp), unread dot when `status !== 'Read'`. Click emits a `(markRead)` output with the notification id (drawer uses this to call markRead and refresh).

NotificationsDrawerComponent: a Material `mat-sidenav` opened from the right (LTR) / left (RTL). Built with `MatSidenavModule` + `MatSidenavContent`. Drawer body:
- "Mark all read" button at the top (calls `markAllRead`, refreshes list + count).
- List of NotificationRowComponent.
- Pagination via `mat-paginator` (page size 10, single column layout).
- Empty / loading / error states.
- Refresh on drawer open.

Header bell: `mat-icon-button` with `matBadge`, `matBadgeColor="warn"`. The unread-count signal is initialized from `getUnreadCount()` on AuthService.refresh completion. Badge is hidden when count is 0. Polls every 60s while authenticated (interval canceled on signOut).

Tests (~6):
1. Drawer open triggers a fresh `list()` + `getUnreadCount()` call.
2. Clicking a row in unread state calls `markRead(id)` and updates the row to `Read`.
3. Mark-all-read calls `markAllRead()`, sets local rows to all `Read`, and zero-out the unread count.
4. Pagination change re-fires `list({ page })`.
5. Error path renders error banner.
6. Header badge hides when unreadCount() === 0.

Commit: `feat(web-portal): notifications drawer + header bell badge (Phase 7.2)`

---

## Task 7.3: FollowsApiService + types

**Files (all new):**
- `features/follows/follows.types.ts`
- `features/follows/follows-api.service.{ts,spec.ts}`

FollowsApiService methods:
- `getMyFollows()` → `Result<MyFollows>` (GET `/api/me/follows`).
- `follow(entityType, id)` → `Result<void>` (POST `/api/me/follows/{plural}/{id}`).
- `unfollow(entityType, id)` → `Result<void>` (DELETE `/api/me/follows/{plural}/{id}`).

`{plural}` map: `topic→topics`, `user→users`, `post→posts`.

**Tests (~5):**
1. `getMyFollows()` GETs `/api/me/follows`.
2. `follow('topic', 't1')` POSTs `/api/me/follows/topics/t1`.
3. `follow('user', 'u1')` POSTs `/api/me/follows/users/u1`.
4. `follow('post', 'p1')` POSTs `/api/me/follows/posts/p1`.
5. `unfollow('topic', 't1')` DELETEs `/api/me/follows/topics/t1`.

Commit: `feat(web-portal): FollowsApiService + DTOs (Phase 7.3)`

---

## Task 7.4: /me/follows page (id-bearing chips)

**Files:**
- `features/follows/follows.page.{ts,html,scss,spec.ts}`
- `features/follows/routes.ts`

FollowsPage:
- Guarded by `authGuard` (registered in Task 7.6 under `/me/follows`).
- On init: `follows.getMyFollows()`.
- Renders three sections (Topics, Users, Posts), each as a list of chips. Each chip: id text + a "View" link (`/community/topics/{id}`, `/community/posts/{id}`, `/community/users/{id}`) + an "Unfollow" button.
- "Unfollow" optimistically removes the id from the local list, calls `unfollow(type, id)`, on error re-inserts.
- Empty per-section state ("You don't follow any topics yet.").

The chip IDs are not hydrated to entity names in v0.1.0 — that's Phase 9 polish (would require parallel calls to topics/posts/users list endpoints, then a registry-style lookup).

Tests (~5):
1. Init load renders sections with chip count matching the response.
2. Empty path renders all three "no follows yet" messages.
3. Unfollow click optimistically removes chip + calls `unfollow(type, id)`.
4. On unfollow error, the chip is re-inserted + error toast fired.
5. Locale toggle switches section headings.

Commit: `feat(web-portal): /me/follows page with id-bearing chips (Phase 7.4)`

---

## Task 7.5: `[cceFollow]` directive — single-button toggle

**Files:**
- `features/follows/follow.directive.{ts,spec.ts}`

Pattern: a structural-flavor attribute directive that any community page can use to drop a "Follow / Unfollow" mat-button beside an entity. Usage:

```html
<button mat-button cceFollow type="topic" entityId="t1">…</button>
```

Directive responsibilities:
- Reads inputs `type: FollowEntityType` and `entityId: string`.
- Reads the global followed-set from a small `FollowsRegistryService` (described below) — a simple signal-backed cache populated lazily on first directive instantiation per session via `getMyFollows()`.
- Computes `isFollowing()` from the registry signal.
- Sets the host button's text to "Follow" / "Unfollow" via internal i18n key bindings.
- On `(click)`, calls `follow(type, id)` or `unfollow(type, id)` and updates the registry signal optimistically.

A new `FollowsRegistryService` (singleton, `providedIn: 'root'`):
- `state = signal<MyFollows | null>(null)`.
- `ensureLoaded()` — fetches once on first access; idempotent.
- `isFollowing(type, id) -> boolean` — derives from the loaded state.
- `setFollowing(type, id, value)` — optimistically updates the cached state (used by both the directive and FollowsPage).

For Task 7.4, refactor FollowsPage to use the registry instead of a local copy (drives consistency between the page and the directive).

Tests for directive (~3):
1. Initial render: directive sets button text to "Follow" when entity is not in registry.
2. Click toggles registry + calls `follow(type, id)`.
3. Click while following calls `unfollow(type, id)` and updates registry to false.

Tests for registry (~2):
1. `ensureLoaded()` calls `getMyFollows()` exactly once across N invocations.
2. `setFollowing('topic', 't1', true)` mutates state to include 't1' in topicIds.

Commit: `feat(web-portal): cceFollow directive + FollowsRegistryService (Phase 7.5)`

---

## Task 7.6: Routes + i18n + E2E nav smoke

**Files:**
- Modify: `features/account/routes.ts` — add `'follows'` and `'notifications'` children:
  ```ts
  { path: 'follows', loadComponent: () => import('../follows/follows.page').then(...) },
  { path: 'notifications', loadComponent: () => import('../notifications/notifications-page.page').then(...) },
  ```
  (`/me/follows` and `/me/notifications` are children of `/me` so they inherit `authGuard` from Phase 6.8.)
- New: `features/notifications/notifications-page.page.{ts,html,scss,spec.ts}` — full-page version that re-uses NotificationsDrawerComponent's body (extract a `<cce-notifications-list>` sub-component to share between drawer and page).
- Modify: `core/layout/header.component.html` — add bell icon next to user menu.
- Modify: `libs/i18n/src/lib/i18n/{en,ar}.json` — extend with `notifications.*` and `follows.*` blocks.
- New: `apps/web-portal-e2e/src/notifications-follows.spec.ts`.

i18n additions:

`notifications`:
- `notifications.title` — "Notifications"
- `notifications.empty` — "You have no notifications."
- `notifications.markAllRead` — "Mark all read"
- `notifications.markedToast` — "Marked {{n}} as read."
- `notifications.unreadBadge` — "Unread"
- `notifications.channel.{Email,Sms,InApp}` — channel labels.
- `notifications.bellLabel` — "Open notifications" (aria-label for the bell icon button).

`follows`:
- `follows.title` — "My follows"
- `follows.section.topics` — "Topics"
- `follows.section.users` — "Experts I follow"
- `follows.section.posts` — "Posts"
- `follows.empty.topics`, `follows.empty.users`, `follows.empty.posts`.
- `follows.unfollow` — "Unfollow"
- `follows.followButton` — "Follow"
- `follows.followingButton` — "Following"
- `follows.unfollowToast` — "Unfollowed."
- `follows.errorToast` — "Couldn't update follow status. Try again."

E2E nav smoke at `apps/web-portal-e2e/src/notifications-follows.spec.ts`:
- `/me/follows` does not mount for anonymous users.
- `/me/notifications` does not mount for anonymous users.
- Header bell button is NOT visible when anonymous.

Commit: `feat(web-portal): /me/notifications + /me/follows routes + i18n + E2E (Phase 7.6)`

---

## Phase 07 — completion checklist

- [ ] Task 7.1 — NotificationsApiService + DTOs (~5 tests).
- [ ] Task 7.2 — Notifications drawer + header bell badge (~6 tests).
- [ ] Task 7.3 — FollowsApiService + DTOs (~5 tests).
- [ ] Task 7.4 — /me/follows page with id-bearing chips (~5 tests).
- [ ] Task 7.5 — `[cceFollow]` directive + FollowsRegistryService (~5 tests).
- [ ] Task 7.6 — Routes + i18n + E2E.
- [ ] All Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.

**If all boxes ticked, Phase 07 complete. Proceed to Phase 08 (Community).**

---

## Phase 9 polish backlog notes

- **Hydrate follow chips** — Phase 9 task: parallel calls to community/users/posts/topics endpoints to render real titles/displayNames instead of GUIDs.
- **Notification real-time push** — current design polls `getUnreadCount()` every 60s. Phase 9 (or later) may add SignalR-style push if backend grows it.
- **Drawer-vs-page sharing** — the page version is just a fixed-width drawer body; if the two diverge significantly, extract a shared `<cce-notifications-list>` sub-component (already proposed above).
