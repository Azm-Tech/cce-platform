# CCE Sub-Project 03 — Internal API — Design Spec

**Date:** 2026-04-28
**Sub-project owner:** Internal API
**Brief:** [`../../subprojects/03-internal-api.md`](../../subprojects/03-internal-api.md)
**Predecessors:** [Foundation](2026-04-24-foundation-design.md) (sub-project 1), [Data & Domain](2026-04-27-data-domain-design.md) (sub-project 2)

---

## 1. Goal

Ship the admin REST API (`CCE.Api.Internal`) — ~47 endpoints across users, roles, content, taxonomies, country profiles, notifications, reports, and audit-log query. Every endpoint is permission-gated against `permissions.yaml`, validates input with FluentValidation, audits state changes via the existing `AuditingInterceptor`, and is exported through OpenAPI for the Admin CMS sub-project (5) to consume.

## 2. Scope

In scope:

- All endpoints needed for BRD §4.1.19–4.1.29 (admin functional requirements) and §6.4 (reports).
- Streamed CSV download for 8 reports.
- File upload + ClamAV virus scan + `IFileStorage` abstraction with a `LocalFileStorage` implementation.
- Audit log query with structured filters.
- Optimistic concurrency on aggregate edits (RowVersion → 409).
- Just-in-time user-record sync from Keycloak claims.
- Country-scoped data filtering for `StateRepresentative` role.
- 4 new ADRs documenting key decisions.
- Annotated tag `internal-api-v0.1.0`.

Out of scope (deferred):

- Excel + PDF report formats — sub-project 8.
- Background virus-scan worker — sub-project 8.
- `S3FileStorage` / `AzureBlobFileStorage` implementations — sub-project 8.
- Diff free-text search on audit log — future phase.
- External (public) API endpoints — sub-project 4.
- Admin CMS UI consumption — sub-project 5.

## 3. Architecture

### 3.1 Layer placement

- **`CCE.Application`** — every endpoint maps to a MediatR command or query handler. Handlers depend on `ICceDbContext` (interface). Validation lives in FluentValidation classes registered via DI assembly scan (Foundation pattern).
- **`CCE.Api.Internal`** — minimal-API endpoint mapping (`MapGet`/`MapPost`/etc.) per controller folder; each endpoint translates HTTP → MediatR send → HTTP. Permission attributes applied per endpoint.
- **`CCE.Api.Common`** — JIT user sync middleware (shared with External API in sub-project 4).
- **`CCE.Infrastructure/Files/`** — `LocalFileStorage` + DI registration.

### 3.2 Cross-cutting (Phase 0)

#### 3.2.1 JIT user sync middleware

`UserSyncMiddleware` in `CCE.Api.Common.Identity`. On every authenticated request:

1. Extract JWT `sub` claim → parse Guid.
2. Check `IMemoryCache` for `"user-synced:{sub}"`. If present, skip.
3. Else `SELECT users WHERE Id = @sub`. If exists, set the cache entry (5 min) and continue.
4. If missing, INSERT a new `User` row from claims:
   - `Id = sub`
   - `Email = email` claim
   - `UserName = preferred_username` claim
   - `LocalePreference = "ar"` (default)
   - Map Keycloak `groups` claim entries (e.g., `"/cce-admins"`, `"/cce-content-managers"`, `"/cce-state-reps"`) to CCE role names via a config-driven map. Insert corresponding `IdentityUserRole<Guid>` rows.
   - Commit via `ICceDbContext.SaveChangesAsync()`.
5. Set the cache entry, continue the pipeline.

The middleware fails open: if claims are malformed (e.g., `sub` not a Guid), it logs a warning and continues — the endpoint will reject the request via `[HasPermission]` if the user has no roles. Non-authenticated requests (no JWT) skip the middleware entirely.

Unit-tested with a fake `ICceDbContext` (InMemory) and integration-tested via Keycloak Testcontainers.

#### 3.2.2 RowVersion concurrency mapping

`ExceptionHandlingMiddleware` (Foundation Phase 07) extended to map `ConcurrencyException` (sub-project 2's `DbExceptionMapper` output) to:

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://cce.moenergy.gov.sa/problems/concurrency",
  "title": "Concurrent edit",
  "status": 409,
  "detail": "The resource was modified by another user. Reload and retry.",
  "instance": "/api/admin/news/{id}"
}
```

#### 3.2.3 PagedResult<T>

```csharp
namespace CCE.Application.Common.Pagination;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total);

public static class PaginationExtensions
{
    public const int MaxPageSize = 100;

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var total = await query.LongCountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, page, pageSize, total);
    }
}
```

#### 3.2.4 OpenAPI per-API export

Foundation's Swashbuckle config emits one document. Sub-project 3 amends `CCE.Api.Common.OpenApi.AddCceOpenApi()` to take an API name + a path predicate, then mounts at `/swagger/internal/v1/swagger.json` for the Internal API. The drift-check script (`scripts/check-contracts-clean.sh`) is extended to fetch + diff the new path.

### 3.3 Endpoint conventions

- **Route prefix:** `/api/admin/<resource>`.
- **HTTP shapes:**
  - `GET ...?page=1&pageSize=20&filter=...` → `200 PagedResult<T>`.
  - `GET .../{id}` → `200 T` or `404`.
  - `POST ...` body = create-DTO → `201 T` with `Location` header.
  - `PUT .../{id}` body = update-DTO including `byte[] RowVersion` → `200 T` or `409`.
  - `DELETE .../{id}?rowVersion=...` → `204` or `409` (RowVersion in query for soft-delete).
  - Workflow transitions: `POST .../{id}/{verb}` (e.g., `/approve`, `/reject`, `/publish`, `/restore`).
- **Authorization:** every endpoint annotated with `[HasPermission(Permissions.X.Y)]`. No bare `[Authorize(Roles=...)]`.
- **Country scoping:** `ICountryScopeAccessor.GetAuthorizedCountryIds()` reads active state-rep assignments for the JWT sub. Country-scoped queries `WHERE country_id IN (@allowedIds)`. ContentManager + SuperAdmin bypass (returns `null`, meaning "no scope").
- **Audit:** state-changing endpoints just call domain methods; the existing `AuditingInterceptor` writes `audit_events` rows.
- **OpenAPI:** every endpoint declares `[ProducesResponseType]` for 200/201/204/400/401/403/404/409 as applicable. Tags grouped by feature area.

### 3.4 File upload + virus scan

#### 3.4.1 `IFileStorage`

```csharp
namespace CCE.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string suggestedFileName, CancellationToken ct);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
}
```

`LocalFileStorage` writes under `CceInfrastructureOptions.LocalUploadsRoot` (default `./backend/uploads/`). Storage key format: `uploads/yyyy/MM/{guid}{ext}`.

#### 3.4.2 Upload flow (`POST /api/admin/assets`)

1. Caller has `Resource.Center.Upload` or `Resource.Country.Submit` permission.
2. Validate MIME type against config-driven allow-list (default: PDF, PNG, JPEG, SVG, MP4, ZIP).
3. Stream request body to `IFileStorage.SaveAsync(...)` → returns `storageKey`.
4. Synchronously scan via `IClamAvScanner` (Foundation has the daemon stub; this is its first real consumer):
   - On `Clean` → `assetFile.MarkClean(clock)`.
   - On `Infected` → `assetFile.MarkInfected(clock)` + delete the storage object (no point keeping malware).
   - On scan failure → `assetFile.MarkScanFailed(clock)` (admin reviews manually).
5. Return `201 { assetFileId, virusScanStatus, originalFileName, sizeBytes, mimeType }`.

#### 3.4.3 Resource publish gate

`POST /api/admin/resources/{id}/publish` checks `AssetFile.VirusScanStatus == Clean` before calling `resource.Publish(clock)`. Otherwise 409 with detail `"Asset has not passed virus scan."`. Drafting is not gated.

#### 3.4.4 Limits

- **Size:** default 100 MB per upload. Enforced via `[RequestSizeLimit(100 * 1024 * 1024)]` on the endpoint and `MaxRequestBodySize` in Kestrel config.
- **MIME allow-list:** in `CceInfrastructureOptions.AllowedAssetMimeTypes` — config-driven so it doesn't need a deploy to amend.

### 3.5 Reports CSV (Phase 7)

8 endpoints under `/api/admin/reports/{name}.csv`. Each uses streaming via `IAsyncEnumerable<TRow>` through `CsvHelper`'s `WriteRecordsAsync`. UTF-8 BOM included for Excel-on-Windows compatibility.

| Endpoint | Permission | Source |
|---|---|---|
| `users-registrations.csv` | `Report.UserRegistrations` | `users` ⨝ roles |
| `experts.csv` | `Report.ExpertList` | `expert_profiles` |
| `satisfaction-survey.csv` | `Report.SatisfactionSurvey` | `service_ratings` |
| `community-posts.csv` | `Report.CommunityPosts` | `posts` |
| `news.csv` | `Report.News` | `news` |
| `events.csv` | `Report.Events` | `events` |
| `resources.csv` | `Report.Resources` | `resources` |
| `country-profiles.csv` | `Report.CountryProfiles` | `countries` |

Each accepts optional `?from=ISO8601&to=ISO8601` for time-bounding.

CsvHelper version pinned at the latest stable in CPM (33.0.1 as of 2026-04). Filename: `<name>-<YYYY-MM-DD>.csv`.

### 3.6 Audit log query (Phase 8)

`GET /api/admin/audit-events` — new permission `Audit.Read` (SuperAdmin only). Adds one row to `permissions.yaml` under a new `Audit` group; existing source generator picks it up at next build.

Filters (all optional, all ANDed):

| Param | Type | Behavior |
|---|---|---|
| `actor` | string | `WHERE actor = @actor` (exact match, e.g. `"user:abc-..."`, `"system"`) |
| `actionPrefix` | string | `WHERE action LIKE @actionPrefix + '.%'` (e.g., `News` matches `News.Added/Modified/Deleted`) |
| `resourceType` | string | `WHERE resource LIKE @resourceType + '/%'` |
| `correlationId` | Guid | exact match |
| `from` | ISO8601 datetime | `>= occurred_on` |
| `to` | ISO8601 datetime | `<= occurred_on` |
| `page` | int | 1-based, default 1 |
| `pageSize` | int | default 50, max 100 |

Default sort: `occurred_on DESC`. Total count via `COUNT(*)`. No diff free-text search in this phase.

### 3.7 Endpoint catalog (47 endpoints)

#### Phase 1 — Identity admin (~6)
1. `GET /api/admin/users`
2. `GET /api/admin/users/{id}`
3. `PUT /api/admin/users/{id}/roles` — replace role assignments
4. `GET /api/admin/state-rep-assignments`
5. `POST /api/admin/state-rep-assignments` — assign rep to country
6. `DELETE /api/admin/state-rep-assignments/{id}` — revoke

#### Phase 2 — Expert workflow (~4)
7. `GET /api/admin/expert-requests` (filter by status)
8. `POST /api/admin/expert-requests/{id}/approve` (body: academic titles)
9. `POST /api/admin/expert-requests/{id}/reject` (body: bilingual reason)
10. `GET /api/admin/expert-profiles`

#### Phase 3 — Content (resources + assets) (~7)
11. `POST /api/admin/assets` (multipart upload)
12. `GET /api/admin/assets/{id}`
13. `GET /api/admin/resources` (paged + filter)
14. `POST /api/admin/resources` (draft)
15. `PUT /api/admin/resources/{id}` (edit)
16. `POST /api/admin/resources/{id}/publish`
17. `POST /api/admin/country-resource-requests/{id}/approve` + `/reject`

(The country-resource workflow shares the resources controller; counted as 1 endpoint with 2 verbs.)

#### Phase 4 — News + Events + Pages + Homepage (~9)
18. `GET /POST /PUT /DELETE /api/admin/news` + `POST /publish`
19. `GET /POST /PUT /DELETE /api/admin/events` + `POST /reschedule`
20. `GET /POST /PUT /DELETE /api/admin/pages`
21. `GET /POST /PUT /DELETE /api/admin/homepage-sections` + `POST /reorder`

#### Phase 5 — Taxonomies + community moderation (~6)
22. `GET /POST /PUT /DELETE /api/admin/resource-categories`
23. `GET /POST /PUT /DELETE /api/admin/topics`
24. `DELETE /api/admin/community/posts/{id}` (soft moderation)
25. `DELETE /api/admin/community/replies/{id}` (soft moderation)

#### Phase 6 — Country admin + Notifications admin (~6)
26. `GET /PUT /api/admin/countries`
27. `GET /PUT /api/admin/countries/{id}/profile`
28. `GET /POST /PUT /api/admin/notification-templates`

#### Phase 7 — Reports (8)
29–36. CSV streamers per Section 3.5.

#### Phase 8 — Audit log + release (1)
37. `GET /api/admin/audit-events`

(The above list slightly compresses CRUD bundles into one row per resource for readability; final endpoint count is ~47 distinct verbs.)

## 4. Data flows

### 4.1 Admin first-login

1. Browser → Admin CMS login → Keycloak → JWT.
2. Browser → `GET /api/admin/users` with bearer token.
3. `UserSyncMiddleware` runs; sees no `users` row for `sub`; INSERTs from claims; assigns roles based on Keycloak group.
4. `[HasPermission(User.Read)]` checks the Identity claims, passes.
5. Handler runs; returns paged user list.
6. Admin sees their own row in the list, with the assigned roles already attached.

### 4.2 State rep submits a country resource

1. State rep → `POST /api/admin/assets` with a PDF.
2. `LocalFileStorage` saves; ClamAV scans synchronously; `AssetFile` row created with `Clean` status.
3. State rep → `POST /api/admin/country-resource-requests` body = `{ countryId, proposedTitleAr/En, proposedDescriptionAr/En, proposedResourceType, proposedAssetFileId }`. (StateRepresentative permission `Resource.Country.Submit`.)
4. `CountryResourceRequest` aggregate created with `Pending` status.
5. ContentManager → `GET /api/admin/country-resource-requests?status=Pending` → sees the new row.
6. ContentManager → `POST /api/admin/country-resource-requests/{id}/approve` → request transitions to `Approved`, raises `CountryResourceRequestApprovedEvent`. The handler also creates a `Resource` from the request fields (the event fires the side-effect via `DomainEventDispatcher`).
7. State rep gets a `RESOURCE_REQUEST_APPROVED` notification (in-app).

### 4.3 News publish

1. ContentManager → `POST /api/admin/news` body = draft. `News` aggregate created.
2. ContentManager → `PUT /api/admin/news/{id}` to refine + provide `RowVersion`. EF concurrency check on save.
3. ContentManager → `POST /api/admin/news/{id}/publish`. Domain method `news.Publish(clock)` raises `NewsPublishedEvent`. AuditingInterceptor writes `News.Modified` audit row in same transaction.

### 4.4 Reports CSV download

1. Admin → `GET /api/admin/reports/news.csv?from=2026-01-01&to=2026-12-31`.
2. `[HasPermission(Permissions.Report_News)]` passes.
3. Handler returns `IAsyncEnumerable<NewsReportRow>` from EF.
4. Endpoint streams rows directly to `Response.Body` via `CsvHelper`. Memory bounded; client downloads as fast as queries return.

### 4.5 Audit log query

1. SuperAdmin → `GET /api/admin/audit-events?actionPrefix=Resource&from=2026-04-01`.
2. `[HasPermission(Permissions.Audit_Read)]` passes (new permission seeded in this sub-project).
3. Query: `WHERE action LIKE 'Resource.%' AND occurred_on >= '2026-04-01' ORDER BY occurred_on DESC OFFSET 0 LIMIT 50`.
4. `PagedResult<AuditEventDto>` returned with raw diff JSON for each row.

## 5. Error handling

- Domain exceptions (sub-project 2) propagate via the existing pipeline:
  - `DomainException` → 400 ProblemDetails.
  - `DuplicateException` → 409 with `type: .../duplicate`.
  - `ConcurrencyException` → 409 with `type: .../concurrency`.
- `ValidationException` (FluentValidation) → 400 with field-level errors (Foundation pattern).
- `KeyNotFoundException` from handlers → 404.
- All 4xx + 5xx responses use RFC 7807 ProblemDetails (`Content-Type: application/problem+json`).
- Sentry captures unhandled exceptions (Foundation Phase 16 wired this).
- Serilog structured logs include `correlation_id`, `actor`, `endpoint`, `latency_ms`.

## 6. Testing strategy

Per the plan:

- **Application unit tests** in `CCE.Application.Tests` mirroring handler folders. Each handler: happy + permission-fail + validation-fail + concurrency-fail (where applicable). ~70 new tests.
- **Integration tests** in `CCE.Api.IntegrationTests` (existing). Each controller: 200/201/204 + 401/403/400/409. Uses Keycloak Testcontainers + the Foundation `TestKeycloakHarness`. ~50 new tests.
- **OpenAPI snapshot:** `contracts/internal-api.yaml` committed; CI runs `scripts/check-contracts-clean.sh` on every PR.
- **Architecture tests** (sub-project 2's `CCE.ArchitectureTests`): no new rules, but the existing 12 stay green (Application doesn't depend on Infrastructure or EFCore — handlers depend on `ICceDbContext` interface).

## 7. ADRs (4 new)

To be written in Phase 8 of this sub-project:

- **ADR-0027** — JIT user sync from Keycloak claims.
- **ADR-0028** — `IFileStorage` abstraction with `LocalFileStorage` (dev only).
- **ADR-0029** — Country-scoped query pattern via `ICountryScopeAccessor`.
- **ADR-0030** — Streamed CSV reports via `IAsyncEnumerable<T>`.

## 8. Versioning

- New CPM packages: `CsvHelper` 33.0.1.
- `permissions.yaml` adds `Audit.Read` permission (SuperAdmin only).
- Migration: none expected. If we add `(occurred_on)` non-clustered index based on profiling, that lands as `AuditEventsOccurredOnIndex` migration.

## 9. Definition of Done

- [ ] 47 endpoints implemented + permission-gated.
- [ ] JIT user sync middleware + 5-min memory cache.
- [ ] `IFileStorage` + `LocalFileStorage` + virus-scan-gated publish.
- [ ] 8 streamed CSV reports.
- [ ] Audit-log structured-filter query + new `Audit.Read` permission.
- [ ] Country-scoped queries for StateRepresentative.
- [ ] Optimistic concurrency 409 mapping.
- [ ] FluentValidation on every command DTO.
- [ ] ~120 new tests passing on top of sub-project 2's 376.
- [ ] Application + Api Integration coverage ≥ 70%.
- [ ] OpenAPI `internal-api.yaml` exported + drift-checked.
- [ ] 4 ADRs (0027–0030).
- [ ] `docs/internal-api-completion.md` DoD report.
- [ ] CHANGELOG entry.
- [ ] `internal-api-v0.1.0` annotated tag.

## 10. Phase plan

9 phases (0–8). Each phase delivers shippable, testable software end-to-end (handlers + endpoints + tests + permission gates). Master plan + per-phase plan files written in `project-plan/plans/2026-04-28-internal-api/`.
