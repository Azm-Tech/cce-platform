# Sprint 05 — Country / State Representatives — Implementation Plan

**Stories:** US014, US060, US061 (state profile view/update) · US051 (view requests) · US052, US053 (submit resources / news / events)
**Branch:** `feat/add-home-page-sections` (or a fresh `feat/sprint-05-state-representatives`)
**Architecture:** Clean Architecture + DDD + CQRS (MediatR) across `CCE.Api.External` (public) and `CCE.Api.Internal` (admin / state-rep CMS).

---

## 0. What already exists (do **not** rebuild)

Verified in the current tree:

| Concern | Location | Status |
|---|---|---|
| `Country` aggregate (ISO codes, names, `LatestKapsarcSnapshotId` pointer) | `src/CCE.Domain/Country/Country.cs` | ✅ |
| `CountryProfile` (bilingual Description / KeyInitiatives / ContactInfo, `RowVersion`) | `src/CCE.Domain/Country/CountryProfile.cs` | ✅ (needs new fields — §3) |
| `CountryKapsarcSnapshot` (Classification, PerformanceScore, TotalIndex, append-only) | `src/CCE.Domain/Country/CountryKapsarcSnapshot.cs` | ✅ |
| `StateRepresentativeAssignment` (User↔Country, revocable) | `src/CCE.Domain/Identity/StateRepresentativeAssignment.cs` | ✅ |
| `cce-state-representative` role + `KnownRoles` + `RolePermissionMap.CceStateRepresentative` | `permissions.yaml`, `PermissionsGenerator.cs` | ✅ |
| `ICountryScopeAccessor` — returns `null` (admin/anon bypass), `[]` (other auth), or `[countryIds]` (state rep) | `src/CCE.Application/Common/CountryScope/`, `src/CCE.Api.Common/Identity/HttpContextCountryScopeAccessor.cs` | ✅ |
| `CountryResourceRequest` aggregate with `Submit()`/`Approve()`/`Reject()` + events | `src/CCE.Domain/Country/CountryResourceRequest.cs` | ✅ (generalize — §2) |
| Approve/Reject commands + endpoints | `CCE.Application/Content/Commands/{Approve,Reject}CountryResourceRequest/`, `CCE.Api.Internal/Endpoints/CountryResourceRequestEndpoints.cs` | ✅ |
| KAPSARC latest-snapshot query + DTO | `CCE.Application/Kapsarc/Queries/GetLatestKapsarcSnapshot/` | ✅ |
| Asset upload + virus scan pipeline | `CCE.Application/Content/Commands/UploadAsset/`, `CCE.Api.Internal/Endpoints/AssetEndpoints.cs` | ✅ |
| Notification dispatch (MassTransit, `INotificationMessageDispatcher`) | `CCE.Application/Notifications/...`, `CCE.Infrastructure/Notifications/Messaging/` | ✅ |
| Pagination helpers (`ToPagedResultAsync`, projection overload, `*Either`) | `CCE.Application/Common/Pagination/` | ✅ |

**Gaps this sprint closes:** public country-profile view query/endpoints, profile demographic fields + update command, the **Submit** side of the request workflow (none exists today — only Approve/Reject), generalization of the request aggregate to also carry **News/Event** submissions, a **List requests** query scoped by `ICountryScopeAccessor`, and the missing notification handlers for approve/reject.

### Two design decisions (confirmed)
1. **One generic request aggregate.** Refactor `CountryResourceRequest` → `CountryContentRequest` with a `ContentKind` discriminator (`Resource | News | Event`). US051 becomes a single list/queue.
2. **Extend `CountryProfile`** with `Population`, `AreaSqKm`, `GdpPerCapita`, and an NDC document asset reference. Existing editorial fields stay. CCE Classification/Performance/TotalIndex remain read-only from `CountryKapsarcSnapshot`.

---

## 1. Story → endpoint map

| Story | Role | API | Endpoint | Permission |
|---|---|---|---|---|
| US014 view state profile (public) | Visitor + User | External | `GET /api/countries`, `GET /api/countries/{id}/profile` | `AllowAnonymous` |
| US060 view profile (state rep) | State Rep | Internal | `GET /api/state/profile` (my assigned country/countries) | `Country.Profile.Update`† |
| US061 update profile | State Rep + Admin | Internal | `PUT /api/state/profile/{countryId}` | `Country.Profile.Update` |
| US051 view requests | State Rep | Internal | `GET /api/state/requests`, `GET /api/state/requests/{id}` | `Resource.Country.Submit`† |
| US052 submit resource | State Rep + Admin | Internal | `POST /api/state/requests/resource` | `Resource.Country.Submit` |
| US053 submit news/event | State Rep + Admin | Internal | `POST /api/state/requests/news`, `POST /api/state/requests/event` | `Resource.Country.Submit` (or new `Content.Country.Submit` — §6) |

† Read endpoints reuse the existing write permission as the gate (state reps already hold it); data is further narrowed by `ICountryScopeAccessor` so a rep only sees their own country. No new "read" permission needed.

> **Optimized-query principle applied throughout:** every list/detail query is `AsNoTracking`, uses the **projection** overload of `ToPagedResultAsync` (selects only DTO columns — no full-entity materialization), resolves KAPSARC via the `Country.LatestKapsarcSnapshotId` **pointer** (avoids an `ORDER BY SnapshotTakenOn` scan of the time-series table), and applies the `ICountryScopeAccessor` filter **inside** the SQL `WHERE` (never in memory).

---

## 2. Generalize the request aggregate (US051/052/053 foundation)

**Goal:** one aggregate, one repository, one list query, one review queue — covering Resource, News, and Event submissions.

### 2.1 Domain — `src/CCE.Domain/Country/`
- **Rename** `CountryResourceRequest` → `CountryContentRequest` (keep file in `Country/`). Per `permissions.yaml` "never rename" rule, that applies to *permission strings*, not classes — but the DB table is renamed via migration (§5).
- Add `ContentKind` enum: `Resource = 0, News = 1, Event = 2`.
- Generalize payload. Keep the shared fields (`CountryId`, `RequestedById`, `Status`, `SubmittedOn`, `AdminNotes*`, `ProcessedBy/On`, title/description bilingual). Replace resource-only fields with a discriminated payload:
  - `ContentKind Kind`
  - `ResourceType? ProposedResourceType` (Resource only)
  - `System.Guid? ProposedAssetFileId` (Resource = the file; News/Event = optional featured image asset)
  - `System.Guid? ProposedTopicId` (News/Event)
  - `System.DateTimeOffset? ProposedStartsOn` / `ProposedEndsOn`, `ProposedLocationAr/En`, `ProposedOnlineMeetingUrl` (Event only)
- Replace `Submit(...)` with **three factories** that enforce per-kind invariants and set `Kind`:
  - `SubmitResource(countryId, requestedById, titleAr/En, descAr/En, resourceType, assetFileId, clock)`
  - `SubmitNews(countryId, requestedById, titleAr/En, contentAr/En, topicId, featuredImageAssetId?, clock)`
  - `SubmitEvent(countryId, requestedById, titleAr/En, descAr/En, topicId, startsOn, endsOn, locationAr/En?, onlineMeetingUrl?, clock)`
  - Each validates required fields (mirrors existing `Submit` guards) and the existing `start < end` rule from `Event.Schedule`.
- `Approve()` / `Reject()` keep their signatures and Pending-only guards. Update events to `CountryContentRequestApprovedEvent` / `...RejectedEvent`, carrying `ContentKind` so the (future, Sprint-07/US050) approval handler can route to `Resource.Draft` / `News.Draft` / `Event.Schedule`.
- Keep `CountryContentRequestStatus` (rename from `CountryResourceRequestStatus`): `Pending=0, Approved=1, Rejected=2`.

> The approve→create-actual-content handler is **out of scope** (US050, Sprint-07). The approved event is raised and left for that phase; note it in the plan but don't build it.

### 2.2 Application
- Move/rename the existing `Approve`/`Reject` command folders to `Content/Commands/{Approve,Reject}CountryContentRequest/` (keep behavior; just retarget the renamed aggregate/repo). Update `Permissions.Resource_Country_Approve/Reject` usages — unchanged strings.
- Add `Content/Dtos/CountryContentRequestDto.cs` (includes `Kind`, status, proposed fields, submitter, processed metadata, admin notes).

### 2.3 Infrastructure
- Rename repo `CountryResourceRequestRepository` → `CountryContentRequestRepository`; add `AddAsync` (currently only `FindIncludingDeletedAsync`/`UpdateAsync`).
- Update EF configuration (table rename, new nullable columns, discriminator column `kind`, index `(country_id, status, kind)` for the scoped list).

---

## 3. Country profile fields (US014/US060/US061)

### 3.1 Domain — `CountryProfile.cs`
Add to the entity + private ctor:
- `int Population` (>0)
- `decimal AreaSqKm` (>0, precision 18,2)
- `decimal GdpPerCapita` (>0, precision 18,2)
- `System.Guid? NationallyDeterminedContributionAssetId` (FK → `AssetFile`; story says "PNG attachment")

Extend `Create(...)` and `Update(...)` signatures with the four new fields and add guards (`Population > 0`, `AreaSqKm > 0`, `GdpPerCapita > 0`). Keep `MarkAsModified` + `RowVersion` concurrency exactly as-is.

> The story labels the field "PDF nationally determined contribution" in US014 but "Must be PNG format" in US061. Treat the **asset** as the source of truth and validate the MIME type at the upload boundary against the configured allow-list (`AllowedAssetMimeTypes`), not in the domain. Flag this AR-spec inconsistency to the PO; default to accepting PDF **and** PNG until clarified.

### 3.2 Infrastructure
- `CountryProfileConfiguration`: add the three numeric columns + decimal precision, the nullable NDC asset FK (no cascade; `Restrict`).

---

## 4. Application layer — queries & commands

All handlers follow the existing conventions: `IRequest<Response<T>>` / `IRequest<PagedResult<T>>`, `ICurrentUserAccessor.GetUserId()`, `ISystemClock`, validators auto-discovered via `AddValidatorsFromAssembly`, manual projection mapping (the repo maps by hand, not Mapster).

### 4.1 US014 — public profile view
- `Country/Queries/ListCountries/` → `PagedResult<CountryListItemDto>` (Id, IsoAlpha3, NameAr/En, RegionAr/En, FlagUrl). `AsNoTracking`, projection overload, `IsActive == true` filter, ordered by `NameEn`.
- `Country/Queries/GetCountryProfile/GetCountryProfileQuery(System.Guid CountryId)` → `Response<CountryProfileDetailDto>`.
  - **Single optimized query:** join `Country` → `CountryProfile` (1:1) → `CountryKapsarcSnapshot` via `c.LatestKapsarcSnapshotId` (left join on the pointer, not a `TOP 1 ORDER BY`), projected straight into the DTO.
  - DTO fields: Population, AreaSqKm, GdpPerCapita, NDC asset (id + download url + filename), Description/KeyInitiatives/ContactInfo, **read-only** CceClassification / CcePerformance / CceTotalIndex (null when no snapshot), `KapsarcSnapshotTakenOn`.
  - Returns `Response` not-found (→ ALT001 / ERR001 mapping) when country missing or profile absent.

### 4.2 US060 — state-rep profile view
- `Country/Queries/GetMyCountryProfile/` → reuses `GetCountryProfile` projection but resolves the country from `ICountryScopeAccessor.GetAuthorizedCountryIdsAsync`. If the rep maps to exactly one country, return it; if several, return a small list (`GET /api/state/profile` returns array). Empty scope → INF005.

### 4.3 US061 — update profile
- `Country/Commands/UpdateCountryProfile/UpdateCountryProfileCommand(CountryId, Population, AreaSqKm, GdpPerCapita, NdcAssetId?, [existing editorial fields])` → `Response<CountryProfileDetailDto>`.
- Handler: load profile (tracked), **guard country scope** (state rep may only edit their assigned `CountryId`; admins bypass — check `ICountryScopeAccessor` result `!= null && !contains(countryId)` ⇒ forbidden), set expected `RowVersion`, call `profile.Update(...)`, `SaveChangesAsync`. KAPSARC fields are never accepted in the command → BC001 satisfied by construction.
- Validator: `Population` integer > 0, `AreaSqKm`/`GdpPerCapita` > 0, NDC asset id non-empty if provided. Missing required ⇒ FluentValidation → ERR013; concurrency/db failure ⇒ ERR033.
- Confirmation `CON026` via the existing `Response` message-code mechanism.

### 4.4 US051 — list / view requests (scoped)
- `Content/Queries/ListCountryContentRequests/ListCountryContentRequestsQuery(Page, PageSize, Status?, Kind?)` → `PagedResult<CountryContentRequestDto>`.
  - Apply `ICountryScopeAccessor`: `null` ⇒ admin sees all; non-empty ⇒ `WHERE country_id IN (...)`; empty ⇒ return empty page (state rep with no assignment → INF005).
  - `AsNoTracking`, projection overload, ordered `SubmittedOn DESC`, uses index `(country_id, status, kind)`.
- `Content/Queries/GetCountryContentRequestById/` → same scope guard; not-found/forbidden → ERR001.

### 4.5 US052 — submit resource
- `Content/Commands/SubmitCountryResourceRequest/` → resolves the rep's `CountryId` from scope accessor (reject if ambiguous/none), validates the asset exists & `VirusScanStatus == Clean` (reuse the check in `CreateResourceCommandHandler`), calls `CountryContentRequest.SubmitResource(...)`, `AddAsync`. Returns `Response<Guid>` with `CON024`.
- Raises no domain event on submit; instead the handler dispatches an **admin notification** (MSG003) — see §7.
- Missing fields → ERR013; persistence failure → ERR029.

### 4.6 US053 — submit news / event
- `Content/Commands/SubmitCountryNewsRequest/` and `.../SubmitCountryEventRequest/` mirroring §4.5, calling `SubmitNews` / `SubmitEvent`. Validate `TopicId` exists; for events validate `StartsOn < EndsOn` (also enforced in domain). Same CON024 / ERR013 / ERR029 + MSG003.

---

## 5. Persistence & migration

One EF migration (`Sprint05_StateRepresentatives`):
1. Rename table `country_resource_requests` → `country_content_requests`; add `kind` (int, default 0 = Resource for existing rows), nullable `proposed_topic_id`, `proposed_starts_on`, `proposed_ends_on`, `proposed_location_ar/en`, `proposed_online_meeting_url`; make `proposed_resource_type` / `proposed_asset_file_id` nullable. Add index `(country_id, status, kind)`.
2. `country_profiles`: add `population` (int), `area_sq_km` (decimal 18,2), `gdp_per_capita` (decimal 18,2), `nationally_determined_contribution_asset_id` (uniqueidentifier null, FK → `asset_files`, `Restrict`).

Backfill: existing profile rows need non-null numeric values — make the columns **nullable in the DB** initially OR backfill a sentinel and tighten later. **Recommendation:** add as nullable at the DB level, enforce `>0` in the domain on write; this avoids a destructive backfill and keeps US014 tolerant of legacy rows (render "—" when null). Adjust the DTO to `int?`/`decimal?` accordingly.

> Apply with the documented flow (`$env:CCE_DESIGN_SQL_CONN=...; dotnet ef database update --project src/CCE.Infrastructure --startup-project src/CCE.Infrastructure`). Seeder (`ReferenceDataSeeder`) optionally extended with demo demographic values under `--demo`.

---

## 6. Permissions (`permissions.yaml`)

Current `Resource.Country.Submit` is resource-specific but adequate as the single submit gate. For clarity (and because News/Event aren't "resources"), **add** a sibling without breaking the existing one:

```yaml
  Content:
    Country:
      Submit:
        description: Submit a country-scoped resource/news/event for approval
        roles: [cce-state-representative, cce-admin, cce-super-admin]
      View:
        description: View own country's content requests
        roles: [cce-state-representative, cce-admin, cce-super-admin]
```

Keep `Resource.Country.Submit/Approve/Reject` as-is (never rename). Rebuild `CCE.Domain` so the source generator emits the new constants, then gate the new endpoints with `Permissions.Content_Country_Submit` / `Content_Country_View`. (Admins are added so US052/053's "Admin / Super Admin Can" rows are honored.)

> If the PO prefers not to add new permission strings, fall back to reusing `Resource_Country_Submit` for all three submit endpoints and the existing approve permission for the list — note that in the PR description.

---

## 7. Notifications (MSG003 + close the approve/reject gap)

- **On submit (US052/053):** handler dispatches `NotificationMessage` with a new `TemplateCode "COUNTRY_CONTENT_SUBMITTED"` (MSG003), `EventType` = a new `NotificationEventType.CountryContentSubmitted`, `Channels: [InApp, Email]`, recipients = admins/content-managers. Reuse the dispatch pattern from `ExpertRegistrationApprovedNotificationHandler`. Resolve admin recipients the same way other admin-facing notifications do (confirm the existing recipient-resolution helper; if none, target by role).
- **Close existing gap:** add the two missing `INotificationHandler` handlers for `CountryContentRequestApprovedEvent` / `...RejectedEvent` → notify `RequestedById` (`CountryContentApproved`/`Rejected` already exist in `NotificationEventType`). These satisfy the requester-feedback half of the workflow even though the actual-content-creation handler is Sprint-07.

---

## 8. API endpoints

- **New file** `CCE.Api.Internal/Endpoints/StateRepresentativeEndpoints.cs` — group `/api/state`, tag `"StateRepresentative"`:
  - `GET /profile`, `PUT /profile/{countryId:guid}`
  - `GET /requests`, `GET /requests/{id:guid}`, `POST /requests/resource`, `POST /requests/news`, `POST /requests/event`
  - Each `.RequireAuthorization(...)` per §1/§6; request bodies as `sealed record` DTOs in the endpoints file (matching `CreateResourceRequest` convention).
- **Extend** `CCE.Api.External/Endpoints/` — add `CountriesPublicEndpoints.cs` (or extend existing country endpoints): `GET /api/countries`, `GET /api/countries/{id:guid}/profile`, both `AllowAnonymous`, output-cached like other public reads.
- Register all in the respective `Program.cs` (`MapStateRepresentativeEndpoints()`, `MapCountriesPublicEndpoints()`).

---

## 9. Tests

- **Domain (`CCE.Domain.Tests`):** `CountryContentRequest` factories (per-kind invariants, event/start<end), `Approve`/`Reject` Pending-only guards; `CountryProfile.Update` numeric guards + KAPSARC immutability (no setter path).
- **Application (unit, NSubstitute `ICceDbContext`):** `UpdateCountryProfile` scope-guard (rep editing foreign country ⇒ forbidden; admin bypass), `GetCountryProfile` projection incl. null-snapshot path, `ListCountryContentRequests` scope filter (null/empty/list), submit handlers (asset-clean check, CON024, MSG003 dispatch).
- **Integration (`CceTestWebApplicationFactory`, `TestAuthHandler`):** state-rep can submit + list only their country; anonymous can hit `GET /api/countries/{id}/profile`; rep cannot edit another country's profile (403); ERR013/ERR033 mappings.
- **Architecture tests:** ensure new Application code has no Infrastructure dependency; endpoints stay Minimal-API.

---

## 10. Sequencing (PR-sized steps)

1. **Domain refactor** — generalize `CountryContentRequest` + `ContentKind`; extend `CountryProfile`. Update domain tests. *(build green, no API change yet)*
2. **Infrastructure** — repo rename + `AddAsync`, EF configs, **migration**, apply to dev DB.
3. **Permissions** — `permissions.yaml` additions, rebuild generator, verify constants.
4. **Profile read/update** — US014/US060/US061 queries, command, validators, public + internal endpoints, output cache.
5. **Request workflow** — submit (US052/053) + list/detail (US051) commands/queries/endpoints; retarget Approve/Reject.
6. **Notifications** — MSG003 on submit + approve/reject requester handlers.
7. **Tests + docs** — integration suite, Swagger annotations, update `CLAUDE.md` if new conventions emerge.

Each step ends with `dotnet build CCE.sln` (warnings = errors) and `dotnet test CCE.sln` green before moving on.

---

## 11. Open questions for the PO
1. **NDC file type:** US014 says PDF, US061 says PNG. Which? (Plan accepts both until clarified.)
2. **Multi-country reps:** can one State Rep represent more than one country? `StateRepresentativeAssignment` supports it; `GET /api/state/profile` returns an array to be safe. Confirm whether UI assumes exactly one.
3. **New permission strings** vs reusing `Resource.Country.Submit` for news/events (§6).
4. **Legacy profile rows** without demographic data — confirm nullable-at-DB approach (render "—") is acceptable vs a backfill.
