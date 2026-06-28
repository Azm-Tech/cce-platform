# Phase 3 — Content Foundation (Read APIs + Admin CRUD)

> Sprint goal: ship the **read surface** (public + admin) and the **admin CRUD** for News, Events, and Resources at the highest performance the existing stack allows, while strictly following the project's established read/write pattern.
>
> **In scope (8 stories):** US047, US044, US046, US043, US003, US010, US048, US045.
> **Deferred:** US006 (Knowledge Maps view), US008 (Interactive City view) — handled in a later phase.

---

## 1. Architecture Pattern (the law, restated)

This phase strictly follows the read/write split codified in `docs/plans/read-write-architecture-implementation-plan.md` and already wired into the codebase.

### 1.1 Reads — `ICceDbContext` directly, no repository

```
Endpoint  →  IMediator.Send(Query)  →  QueryHandler
                                         ├─ injects ICceDbContext
                                         ├─ .AsNoTracking() is implicit (explicit-interface impl in CceDbContext)
                                         ├─ WhereIf(...) for optional filters
                                         ├─ .Select(...) → DTO projection (server-side, narrow columns)
                                         ├─ .ToPagedResultAsync(page, pageSize, ct)
                                         └─ returns PagedResult<TDto> or TDto
Endpoint wraps the result in Response<T> via MessageFactory.Ok(...).
```

**Why this is the fastest read path we can build today:**
- `ICceDbContext` already returns `AsNoTracking()` queryables — no change-tracking overhead.
- `.Select(...)` ships only the columns needed (List-card vs. Detail are different DTOs → different `.Select()`s → different SQL).
- `WhereIf` keeps the SQL plan stable for filter-less requests.
- `ToPagedResultAsync` runs `COUNT(*) OVER()` style pagination in a single round trip via the `PaginationExtensions` helpers.
- Output caching (`CCE.Api.Common`) already covers anonymous public reads — see §6.

### 1.2 Writes — generic repository fetch + domain methods + `ICceDbContext` as UoW

```
Endpoint  →  IMediator.Send(Command)
                ├─ FluentValidation pipeline behavior runs first (400 on validation fail)
                ↓
            CommandHandler
                ├─ injects IRepository<TAggregate, Guid>  ← fetch only
                ├─ injects ICceDbContext                  ← UoW (SaveChangesAsync)
                ├─ injects ICurrentUserAccessor, ISystemClock, MessageFactory
                ├─ repo.GetByIdAsync(id, ct)              ← for update/delete
                ├─ aggregate.<DomainMethod>(...)          ← state change lives on the aggregate
                ├─ for "Create": var entity = TAggregate.Factory(...); await repo.AddAsync(entity, ct);
                ├─ for "Delete": repo.Delete(entity);     ← (BC001: permanent + irreversible per US045/US048)
                ├─ await db.SaveChangesAsync(ct);         ← UoW commit, fires AuditingInterceptor + DomainEventDispatcher
                └─ return MessageFactory.Ok(dto, "CONTENT_CREATED" | "CONTENT_UPDATED" | "CONTENT_DELETED")
```

**Why this pattern:**
- Single `SaveChangesAsync` per command = one transaction, audit columns set in one place, domain events dispatched once.
- The generic `IRepository<T, TId>` (`CCE.Application.Common.Interfaces.IRepository`) is enough for **fetch + add + delete**; complex queries belong in handlers reading via `ICceDbContext`, not in bespoke repository methods. This stops repository interfaces from drifting into "god interfaces" (the violation called out in the read/write plan).
- Domain methods (`News.Draft`, `News.UpdateContent`, `Resource.Publish`, `Event.Reschedule`, …) already exist and enforce invariants — handlers MUST call them, never mutate properties directly.
- Concurrency: where the aggregate has a `RowVersion`, the existing `ICceDbContext.SetExpectedRowVersion(entity, expected)` is the canonical optimistic-lock hook. Use it on Update; skip it on hard Delete (BC001 says deletion is permanent, no merge needed).

### 1.3 Response envelope — `Response<T>` via `MessageFactory`, always

Every endpoint — read AND write — returns `Response<T>` (or `Response<VoidData>` for sinks). Codes come from `SystemCodeMap` (CON0xx / ERR0xx / VAL0xx), messages are resolved by `ILocalizationService` using the request's `Accept-Language` header. **No raw `Results.Ok(dto)`.**

Use cases mapped to `MessageFactory` calls in this phase:

| Story | Success | Failure |
|---|---|---|
| US047 Upload Resource | `Ok(dto, "RESOURCE_CREATED")` → CON025 | `AssetNotFound`, `AssetNotClean`, validation → VAL0xx |
| US044 Upload News / Event | `Ok(dto, "CONTENT_CREATED")` → CON020 | validation → VAL0xx |
| US046 / US043 View (Admin) list+detail | `Ok(paged, "ITEMS_LISTED")` / `Ok(dto, "SUCCESS_OPERATION")` | `NewsNotFound`/`EventNotFound`/`NOT_FOUND` |
| US003 / US010 View (Public) list+detail | same as admin but the dto is the **public** dto | `NotFound<T>("NEWS_NOT_FOUND")` etc. |
| US048 Delete Resource | `Ok("RESOURCE_DELETED")` → CON027 | `Conflict` if referenced (see §3.6) |
| US045 Delete News / Event | `Ok("CONTENT_DELETED")` → CON022 | same |

---

## 2. What is already in the codebase (and what we keep)

A surprising amount of Phase 3 is **already implemented** — the work below is mostly **shape-correction and gap-closing**, not greenfield.

| Story | Status | Files |
|---|---|---|
| US047 Upload Resource | Exists; needs `Response<T>` envelope + asset-MIME-type validation per BRD (PDF/Word/link). | `CreateResourceCommand`, `ResourceEndpoints.cs` |
| US044 Upload News / Event | News + Event commands exist. Need to align validators with BRD field caps (255 / 2000) and add **News-vs-Event branching** at the endpoint (admin sends one form). | `CreateNewsCommand`, `CreateEventCommand` |
| US046 View Resources (Admin) | List exists (`ListResourcesQuery`). **Detail-by-id is missing** for admin — only public has it. | needs new `GetResourceByIdQuery` (admin variant) |
| US043 View News/Events (Admin) | List for News exists (`ListNewsQuery`). Detail exists (`GetNewsById`). Events: `ListEvents` + `GetEventById` exist. Just needs `Response<T>` wrap + admin tag in Swagger. | existing |
| US003 View Resources (Public) | Fully exists (`ListPublicResources`, `GetPublicResourceById`). Wrap in `Response<T>` and add OutputCache. | existing |
| US010 View News/Events (Public) | Exists (`ListPublicNews`, `GetPublicNewsBySlug`, `ListPublicEvents`, `GetPublicEventById`). Wrap + cache. | existing |
| US048 Delete Resource | **Missing.** Add `DeleteResourceCommand` + admin endpoint. BRD: permanent + irreversible. | NEW |
| US045 Delete News / Event | `DeleteNewsCommand` exists (soft-delete via `news.SoftDelete`). BRD says permanent → see §3.6. Event delete: **missing**. | partial |

> **Read the BRD again, carefully:** BC001 on US045 / US048 says **"Deletion must be permanent and irreversible."** This contradicts the current `SoftDelete` flow on News. See §3.6 for how we reconcile this. Do not "fix" it silently — confirm with the product owner before swapping to hard-delete.

---

## 3. Story-by-story implementation

> All file paths are relative to repo root unless noted. New files marked **(NEW)**. Existing files marked **(EDIT)**.

### 3.1 US047 — Upload Resources (Admin)

**Goal:** `POST /api/admin/resources` accepts the BRD form, validates it, scans the file, creates a `Resource` draft, and returns `Response<ResourceDto>` with `CON025` (`RESOURCE_CREATED`).

**Code paths**
- `src/CCE.Application/Content/Commands/CreateResource/CreateResourceCommand.cs` **(EDIT)** — change return type from `ResourceDto` to `Response<ResourceDto>`.
- `src/CCE.Application/Content/Commands/CreateResource/CreateResourceCommandHandler.cs` **(EDIT)**:
  - Replace ad-hoc `throw new KeyNotFoundException` with `MessageFactory.AssetNotFound<ResourceDto>()`.
  - Replace `throw new DomainException(...)` for unclean asset with `MessageFactory.AssetNotClean<ResourceDto>()`.
  - On success: `return _messages.Ok(dto, "RESOURCE_CREATED");`.
  - **Keep using `IResourceRepository.SaveAsync`** as is — it's the existing repo over the same `CceDbContext`, so it acts as our UoW boundary.
- `src/CCE.Application/Content/Commands/CreateResource/CreateResourceCommandValidator.cs` **(EDIT)** — enforce BRD field caps: `TitleAr/En` ≤255, `DescriptionAr/En` ≤500, `ResourceType` in enum, `CategoryId/AssetFileId` not empty.
- `src/CCE.Api.Internal/Endpoints/ResourceEndpoints.cs` **(EDIT)** — return `Results.Ok(response)` where `response` is the `Response<ResourceDto>` from the handler. Set HTTP status from `response.Type` via the existing `ResponseStatusMapper` (or whatever helper the codebase already uses — search before adding a new one).

**Acceptance checks**
- AC4 (BC001 — validate before upload): FluentValidation pipeline runs **before** the handler.
- AC5 (CON021 success): BRD says "CON021" but our SystemCodeMap uses CON025 = `RESOURCE_CREATED` (CON021 is generic content-updated). Use **`RESOURCE_CREATED` (CON025)** — it's the more specific code, and the AR localization string should match the BRD copy. Update `Resources.yaml` if needed.
- AC6 (ERR013 missing required): handled by FluentValidation → `Response.Fail(MessageType.Validation, ...)`.
- AC7 (ERR029 upload failure): wraps as `MessageFactory.BusinessRule("RESOURCE_UPLOAD_FAILED")` (add domain key + ERR0xx mapping if not present).

**Open question — Multi-select Covered Countries:** The BRD lists "Covered Countries (multi-select)" but the current `Resource` aggregate has a single `CountryId?`. Two options:
- **(a)** Treat the existing `CountryId` as the **owning** country (state-rep uploaded vs. center-managed) and add a new `ResourceCoveredCountries` join table for the "topical coverage" list. This is the correct domain modeling.
- **(b)** Ship Phase 3 with single-country and defer multi-coverage to Phase 4.
- **My take:** (b). Multi-coverage doesn't unblock anything else in this phase and the join table needs a migration + indexes + public list filter changes. Confirm with PO before adding the join.

---

### 3.2 US044 — Upload News / Events (Admin)

**Goal:** one admin "Add News/Event" form, two backend paths (`News` vs. `Event`), both returning `Response<...Dto>` with `CONTENT_CREATED`.

**Code paths**
- `src/CCE.Application/Content/Commands/CreateNews/*` **(EDIT)** — handler returns `Response<NewsDto>`, calls `_messages.Ok(dto, "CONTENT_CREATED")`. Validator: `TitleAr/En` ≤255, `ContentAr/En` ≤2000.
- `src/CCE.Application/Content/Commands/CreateEvent/*` **(EDIT)** — handler returns `Response<EventDto>`. Validator: title ≤255, description ≤2000, `EndsOn > StartsOn`, optional URLs require https://.
- **Image upload** for News (BRD: PNG required): the admin first calls `POST /api/admin/assets` (already exists, `AssetEndpoints.cs`), gets back `assetFileId`, then submits `CreateNewsCommand` with `featuredImageUrl` derived from the asset record. **No raw multipart on the news endpoint.** This keeps the virus-scan boundary in one place.
- `src/CCE.Api.Internal/Endpoints/NewsEndpoints.cs` and `EventEndpoints.cs` **(EDIT)** — return `Response<T>`.

**News vs Event branching:** keep two endpoints (`POST /api/admin/news`, `POST /api/admin/events`) — the admin UI does the dispatch. Don't build a polymorphic `/content` endpoint; the form fields diverge enough (Event has date range, News doesn't) that overloading hurts clarity.

---

### 3.3 US046 — View Resources (Admin)

**Goal:** `GET /api/admin/resources` (paged list) + `GET /api/admin/resources/{id}` (detail), both returning `Response<...>`. The list endpoint already exists; the detail endpoint does **not**.

**Code paths**
- `src/CCE.Application/Content/Queries/ListResources/*` **(EDIT)** — return `Response<PagedResult<ResourceDto>>` via `MessageFactory.Ok(paged, "ITEMS_LISTED")`.
- `src/CCE.Application/Content/Queries/GetResourceById/` **(NEW)**:
  - `GetResourceByIdQuery.cs` — `record GetResourceByIdQuery(Guid Id) : IRequest<Response<ResourceDto>>`.
  - `GetResourceByIdQueryHandler.cs` — `_db.Resources.Where(r => r.Id == id).Select(MapToDto).FirstOrDefaultAsync(ct)`; null → `MessageFactory.NotFound<ResourceDto>("RESOURCE_NOT_FOUND")` (ERR042). Reuse the **same `MapToDto` projection** from `ListResourcesQueryHandler` — declare it `internal static` already; just call it.
- `src/CCE.Api.Internal/Endpoints/ResourceEndpoints.cs` **(EDIT)** — add `MapGet("/{id:guid}", ...)`.

**Performance:** the detail handler does NOT use the repository — it reads through `ICceDbContext` with a server-side `.Select()` so SQL only ships the columns the DTO actually needs. This is the read-path rule from §1.1.

**INF004 / no resources:** handled at the controller level — if `paged.Items.Count == 0` the list still returns `Success=true`, code `ITEMS_LISTED`, empty array. The frontend renders INF004 from an empty `Data.Items`, not from a server-side flag. (This matches how `ListPublicResources` already behaves.)

---

### 3.4 US043 — View News & Events (Admin)

Same shape as 3.3 but for News and Events. List + detail handlers already exist. The work is:
- Wrap both list endpoints' results in `Response<PagedResult<TDto>>`.
- Wrap detail endpoints in `Response<TDto>`; `null` → `MessageFactory.NewsNotFound<NewsDto>()` / `EventNotFound<EventDto>()`.
- Add State-Rep authorization to the **read** endpoints (BRD US043 grants State Rep view access). Add `Permissions.Content_News_View_Admin` (or similar — check `permissions.yaml` first) if not present.

---

### 3.5 US003 / US010 — Public Views

The public reads (`ListPublicResources`, `ListPublicNews`, `GetPublicNewsBySlug`, `ListPublicEvents`, `GetPublicEventById`, `GetPublicResourceById`) already do server-side projection through `ICceDbContext`. The only Phase 3 work:

1. **Wrap each in `Response<T>`** at the endpoint layer (not the handler — keep handlers returning `PagedResult<T>` so they're cache-friendly; the endpoint adds the envelope so the cache key stays stable on the inner data).
2. **OutputCache policies** (already configured in `CCE.Api.Common`): tag list endpoints with `"public-resources"`, `"public-news"`, `"public-events"`; the admin write commands need to **purge** the matching tags after a successful `SaveChangesAsync`. Hook this off `ResourcePublishedEvent`, `NewsPublishedEvent`, etc. via a `INotificationHandler<TEvent>` in Application, not in the handler.
3. **Search/filter** AC: list endpoints already accept filters; add `?search=` (Ar+En contains, OR'd) on the public News and Resources lists for parity with admin. **Do not** add fuzzy search here — Meilisearch already covers that and is wired in `SearchEndpoints`. Trying to do both in one place hurts cache hit rate.

**ALT002 (no results):** same as INF004 — return empty paged result, frontend renders the "no results" copy.

---

### 3.6 US048 / US045 — Deletes (permanent vs. soft)

**The conflict:** BRD says "permanent and irreversible". Current code soft-deletes News via `News.SoftDelete(deletedById, clock)`. Two correct answers:

- **(A) Honor the BRD literally → hard delete.** Add `DeleteResourceCommandHandler` and rewrite `DeleteNewsCommandHandler` / new `DeleteEventCommandHandler` to call `IRepository<T, Guid>.Delete(entity)` + `db.SaveChangesAsync(ct)`. Lose audit trail of "who deleted what".
- **(B) Keep soft-delete, expose it as "permanent from the user's perspective".** The audit trail stays. The UI never surfaces deleted items. Admin gets a "Restore" panel reachable only from the audit log if at all.

**My recommendation: (B).** Reasons:
1. The existing `[Audited]` + `AuditingInterceptor` pipeline is the project's compliance backbone. Hard deletes leak history we likely need for the moderation/abuse appeal flow (which `CommunityModerationEndpoints.cs` already implies exists).
2. The BRD wording is operationally about *user experience* ("the user can't get it back, period"), not literally `DELETE FROM`. The current soft-delete + global query filter (in `CceDbContext.OnModelCreating`) already achieves this UX.
3. Switching to hard delete forces cascade behavior decisions for FK referrers (e.g. `CountryResourceRequest.ResourceId`, `ResourcePublishedEvent` outbox rows) — that's Phase 4 territory.

**Action:** Confirm (B) with the PO before coding. If they pick (A), the work is mechanically simple but the migration story (existing soft-deleted rows + outbox rows referencing them) is not.

**Concrete plan assuming (B):**
- `src/CCE.Application/Content/Commands/DeleteResource/` **(NEW)** — `DeleteResourceCommand(Guid Id)`, handler injects `IResourceRepository` + `ICurrentUserAccessor` + `ISystemClock` + `MessageFactory`. Adds `Resource.SoftDelete(deletedById, clock)` to the aggregate (mirror `News.SoftDelete`). Returns `Response.Ok("RESOURCE_DELETED")` (CON027).
- `DeleteNewsCommandHandler` **(EDIT)** — replace `KeyNotFoundException` with `MessageFactory.NewsNotFound<VoidData>()`.
- `src/CCE.Application/Content/Commands/DeleteEvent/` **(NEW)** — same shape; add `Event.SoftDelete(...)` on the aggregate.
- Endpoints **(EDIT)**: `DELETE /api/admin/resources/{id}`, `DELETE /api/admin/news/{id}` (exists), `DELETE /api/admin/events/{id}`. All `Permissions.Content_Xxx_Delete`. All purge OutputCache tags on success.

---

## 4. Validators — shared validation rules

Put these in one place (`CCE.Application/Content/Validators/ContentValidators.cs` **(NEW)**, static helpers) so the 4 create/update validators stop duplicating them:

```csharp
public static IRuleBuilderOptions<T, string> BilingualTitle<T>(this IRuleBuilder<T, string> rb)
    => rb.NotEmpty().MaximumLength(255);

public static IRuleBuilderOptions<T, string> BodyText<T>(this IRuleBuilder<T, string> rb, int max)
    => rb.NotEmpty().MaximumLength(max);

public static IRuleBuilderOptions<T, string?> OptionalHttpsUrl<T>(this IRuleBuilder<T, string?> rb)
    => rb.Must(u => u is null || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
         .WithMessage("Must use https://.");
```

Field error codes (`VAL002` required, `VAL006` max-length, `VAL007` format) flow through `MessageFactory.Field(...)` already — the `ValidationBehavior` pipeline picks up FluentValidation failures and emits `Response.ValidationError` with localized field-level codes. No new wiring.

---

## 5. Endpoint conventions for this phase

| Verb | Route | Auth | Permission | Cache |
|---|---|---|---|---|
| GET  | `/api/resources` | Anonymous | — | OutputCache tag `public-resources` |
| GET  | `/api/resources/{id}` | Anonymous | — | tag `public-resources` |
| GET  | `/api/news` | Anonymous | — | tag `public-news` |
| GET  | `/api/news/{slug}` | Anonymous | — | tag `public-news` |
| GET  | `/api/events` | Anonymous | — | tag `public-events` |
| GET  | `/api/events/{id}` | Anonymous | — | tag `public-events` |
| GET  | `/api/admin/resources` | Bearer | `Resource_Center_Upload` | none (admin) |
| GET  | `/api/admin/resources/{id}` | Bearer | `Resource_Center_Upload` | none |
| POST | `/api/admin/resources` | Bearer | `Resource_Center_Upload` | purge `public-resources` |
| DELETE | `/api/admin/resources/{id}` | Bearer | `Resource_Center_Delete` | purge `public-resources` |
| GET / POST / DELETE | `/api/admin/news` and `/api/admin/events` | Bearer | per `permissions.yaml` | purge respective tag |

> Check `permissions.yaml` before adding new permissions — the source generator regenerates `Permissions.cs` on rebuild.

---

## 6. Performance plan (the "highest performance" ask)

The pattern in §1 already gets us most of the wins. Specific levers for this phase:

1. **`AsNoTracking()` on every read** — handled implicitly by `ICceDbContext`. Do **not** call `.AsTracking()` inside a query handler.
2. **Server-side DTO projection** — every read handler ends in `.Select(MapToDto)` **before** `ToPagedResultAsync` / `FirstOrDefaultAsync`. SQL emits only the DTO columns. This is the single biggest perf delta vs. fetch-then-map.
3. **List vs. detail DTO split** — already in place for public (`PublicResourceDto` vs. detail). Don't merge them. Lists drop long fields like `ContentAr` / `ContentEn` / `DescriptionAr` / `DescriptionEn`.
4. **OutputCache** — anonymous public reads only. 60s for lists, 5m for slug-based detail (slug is stable). Vary by `Accept-Language`. Purge on write commands.
5. **Single round trip for pagination** — `ToPagedResultAsync` already does `Count + Page` as one query when EF's `Take().LongCountAsync()` would be two. Keep using it; don't call `CountAsync()` separately.
6. **Indexes** — verify (or add EF Core configurations for) covering indexes:
   - `Resources (PublishedOn DESC, Id) WHERE IsDeleted = 0` — drives the public list ordering.
   - `Resources (CategoryId, PublishedOn DESC) WHERE PublishedOn IS NOT NULL` — category-filtered public list.
   - `News (PublishedOn DESC, Id) WHERE IsDeleted = 0`.
   - `Events (StartsOn ASC, Id) WHERE StartsOn >= NOW()` — upcoming-events list.
   - `News (Slug) UNIQUE WHERE IsDeleted = 0` (already in Phase 08 per comment in `News.cs`).
   - Migration goes in `src/CCE.Infrastructure/Migrations`.
7. **N+1 audit** — none of the read handlers in this phase navigate to related aggregates; if you find a `.Include(...)` slipping in (e.g. to fetch `AssetFile.Url` for the resource list), instead **project the asset URL into the DTO via a join `.Select`** — don't materialize the navigation.
8. **No tracking, no proxies, no lazy loading** — the EF config already disables lazy loading. Don't reintroduce it.

What we are NOT doing in Phase 3 (and why):
- **No Redis read-through cache layer.** OutputCache is sufficient; Redis adds a second cache-invalidation surface for marginal gains on already-cached responses.
- **No GraphQL / DataLoader.** Out of scope.
- **No CQRS read-store materialized views.** The single SQL Server is fast enough for current row counts.

---

## 7. Tests

For each story:
- **Application unit tests** (mock `ICceDbContext` via NSubstitute, mock `IRepository<T, Guid>`, mock `MessageFactory` is unnecessary — instantiate it with a fake `ILocalizationService`):
  - Command handlers: happy path, validation fail (via `ValidationBehavior`), not-found, unauthorized (no user), conflict (concurrency).
  - Query handlers: filter combinations, empty-result, pagination clamp.
- **Architecture tests** (`CCE.ArchitectureTests`): a new test that asserts `*QueryHandler` classes inject **only** `ICceDbContext` (not `IRepository<,>`), and `*CommandHandler` classes inject `IRepository<,>` and `ICceDbContext` but never project DTOs from raw queryables (i.e. no `.Select(... new XxxDto(...))` inside a command handler).
- **Integration tests** (`CceTestWebApplicationFactory`): one round-trip per endpoint exercising the `Response<T>` envelope: codes, messages, `Accept-Language: ar` vs. `en`.

---

## 8. Rollout order (the four parallel tracks)

Four developers can take one track each. They share §4 (validators) and §1 (envelope) which land first.

| Order | Track | Stories | Why |
|---|---|---|---|
| 0 (prep) | Common | shared validators (§4), `Response<T>` wrap helper, OutputCache tag purger | Unblocks everyone. |
| Track A | Resources (admin) | US047, US046, US048 | Single aggregate, cleanest. |
| Track B | News/Events (admin) | US044, US043, US045 | Two aggregates but parallel shape. |
| Track C | Public reads | US003, US010 | Just envelope + cache; the handlers exist. |
| Track D | Indexes + cache purge wiring | (cross-cutting) | Migration + event handlers. |

Estimated effort: A ≈ 1.5 days, B ≈ 2 days, C ≈ 0.5 day, D ≈ 1 day. Total ≈ 5 dev-days serial, ~2 days with four people running in parallel after the prep step.

---

## 9. My take on the social-media-adjacent flows

The user asked for my take on "social media flows" tied to this content. Phase 3 does **not** ship these, but the read/write surface we land here is the foundation for:

- **US011 Share News/Event** — a one-shot `POST /api/news/{id}/share` is the wrong shape. Sharing is a **client-side** action 95% of the time (Web Share API, copy link, native share sheet). The backend's only real job is to issue **canonical share URLs with OG tags** for crawlers. **Action for Phase 3:** make sure every public detail endpoint already returns the canonical slug/URL + a `Response.Data.canonicalUrl` field. Don't build a server-side "share" endpoint. If we ever need share-count metrics, log them client-side via the existing telemetry; don't gate sharing behind an API round trip that adds latency.
- **US012 Follow News Page** — this is a `UserFollow` + `PostFollow` style mechanic that already exists in `CCE.Domain.Community` (`TopicFollow`, `UserFollow`, `PostFollow`). When Phase 4 picks this up, **add `NewsFollow` only if news isn't modeled as a `Topic`**. The right pattern: treat News as a Topic kind, reuse `TopicFollow`, get notification fan-out for free via the existing `NotificationTemplate` pipeline.
- **US013 Add Event to Calendar** — already half-built: `Event.ICalUid` is stable, and `CCE.Application.Content.Public.IcsBuilder` exists. Phase 3 should expose `GET /api/events/{id}.ics` returning `text/calendar` with `Content-Disposition: attachment; filename=event-{slug}.ics`. **This is a 30-line endpoint** — worth landing alongside US010 if time permits, because the calendar use case is the #1 share vector for events. **Not on the official Phase 3 list, but I'd push for it.**
- **Notifications for followers** — when US012 lands, the right hook is `NewsPublishedEvent` → `INotificationHandler<NewsPublishedEvent>` in Application → enqueue `UserNotification` rows for everyone in `TopicFollows` where `TopicId = News.TopicId`. This is **outbox-pattern friendly**: `SaveChangesAsync` commits the news publish + the notification fan-out rows in one transaction, and a background dispatcher delivers them. The infrastructure (`NotificationLogs`, `UserNotifications`) is already in `ICceDbContext`.
- **Anti-pattern to avoid:** do NOT add Twitter/Facebook/LinkedIn API integrations for "auto-post when content publishes". That work needs OAuth flows per admin, per platform, and turns the CMS into a social-publishing pipeline. **Keep the server-side OG/Twitter Card metadata correct** and let admins post manually from their own accounts. If the PO really wants auto-posting, route it through Zapier/n8n via a single webhook — don't bake it into the API.

---

## 10. Definition of Done for Phase 3

- All eight endpoints (admin + public for News, Events, Resources; deletes for News, Events, Resources) return `Response<T>` with correct CON/ERR codes and AR/EN messages.
- `dotnet build CCE.sln` clean with `TreatWarningsAsErrors=true`.
- `dotnet test CCE.sln` green: new unit + integration tests cover happy + 1 fail path per endpoint.
- Architecture test enforces "queries use `ICceDbContext`, commands use `IRepository<,>` + `ICceDbContext`".
- Migrations applied for the new indexes in §6.6.
- OutputCache tags purge on writes (manually verified: publish a resource, GET `/api/resources` returns the new item before the 60s expiry).
- Soft-delete vs. hard-delete decision documented (a one-paragraph note in this file or a follow-up ADR) and confirmed with PO.
