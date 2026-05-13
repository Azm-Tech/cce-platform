# Phase 02 — Expert workflow endpoints

> Parent: [`../2026-04-28-internal-api.md`](../2026-04-28-internal-api.md) · Spec: [`../../specs/2026-04-28-internal-api-design.md`](../../specs/2026-04-28-internal-api-design.md) §3.7 (Phase 2)

**Phase goal:** Ship the 4 admin endpoints under `/api/admin/expert-requests` and `/api/admin/expert-profiles` that drive the expert-registration workflow.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 01 closed at `16df556`. 437 + 1 skipped backend tests passing.
- Cross-cutting infrastructure in place: `ICceDbContext` with `IQueryable<>` accessors, `RoleToPermissionClaimsTransformer`, `HttpContextCurrentUserAccessor`, `IStateRepAssignmentService` pattern, `KeyNotFoundException` → 404 mapping, `DomainException` → 400 mapping, `AdminAuthFixture`.

---

## Pre-execution sanity checks

1. `git status` clean apart from `.claude/`.
2. `dotnet build backend/CCE.sln --no-restore` 0/0.
3. `dotnet test backend/CCE.sln --no-build --no-restore` 437 + 1 skipped.

---

## Endpoint catalog

| # | Verb + path | Permission | Body / query | Returns |
|---|---|---|---|---|
| 2.1 | `GET /api/admin/expert-requests?page=1&pageSize=20&status=Pending&requestedById=...` | `Community.Expert.ApproveRequest` | – | `PagedResult<ExpertRequestDto>` |
| 2.2 | `POST /api/admin/expert-requests/{id}/approve` | `Community.Expert.ApproveRequest` | `{ AcademicTitleAr, AcademicTitleEn }` | `200 ExpertProfileDto` (or 404 / 400) |
| 2.3 | `POST /api/admin/expert-requests/{id}/reject` | `Community.Expert.ApproveRequest` | `{ RejectionReasonAr, RejectionReasonEn }` | `200 ExpertRequestDto` (or 404 / 400) |
| 2.4 | `GET /api/admin/expert-profiles?page=1&pageSize=20&search=...` | `Community.Expert.ApproveRequest` | – | `PagedResult<ExpertProfileDto>` |

---

## Domain handles

- `ExpertRegistrationRequest.Approve(approvedById, ISystemClock)` — transitions Pending → Approved; raises `ExpertRegistrationApprovedEvent`.
- `ExpertRegistrationRequest.Reject(rejectedById, reasonAr, reasonEn, ISystemClock)` — transitions Pending → Rejected; raises `ExpertRegistrationRejectedEvent`.
- `ExpertProfile.CreateFromApprovedRequest(request, academicTitleAr, academicTitleEn, ISystemClock)` — factory; throws if request not in Approved state.
- The Approve domain event does NOT carry academic titles. The handler in Task 2.2 calls `request.Approve(...)` AND `ExpertProfile.CreateFromApprovedRequest(...)` in the same unit-of-work transaction. The domain event still fires (used by Phase 07 for notifications) but doesn't create the profile.

---

## Cross-cutting work needed

1. Extend `ICceDbContext` with `IQueryable<ExpertRegistrationRequest> ExpertRegistrationRequests` and `IQueryable<ExpertProfile> ExpertProfiles`. Implement explicitly on `CceDbContext`.
2. Define `IExpertWorkflowService` (Application interface) + `ExpertWorkflowService` (Infrastructure implementation) for state-mutating operations. Methods:
   - `Task<ExpertRegistrationRequest?> FindIncludingDeletedAsync(Guid id, CancellationToken ct)` — used by approve/reject to load before mutating.
   - `Task SaveAsync(ExpertProfile newProfile, CancellationToken ct)` — adds profile + saves changes (the request mutation persists via tracker on the same SaveChanges).
   - `Task UpdateRequestAsync(ExpertRegistrationRequest request, CancellationToken ct)` — used by reject (only request mutates, no profile created).
3. The first task that touches these adds the surface; subsequent tasks reuse.

---

## Task 2.1 — `GET /api/admin/expert-requests`

**Files (new):**
- `CCE.Application/Common/Interfaces/ICceDbContext.cs` (modify — add ExpertRegistrationRequests + ExpertProfiles)
- `CCE.Infrastructure/Persistence/CceDbContext.cs` (modify — explicit interface accessors)
- `CCE.Application/Identity/Dtos/ExpertRequestDto.cs`
- `CCE.Application/Identity/Queries/ListExpertRequests/ListExpertRequestsQuery.cs` (Page, PageSize, Status?, RequestedById?)
- `.../ListExpertRequestsQueryHandler.cs`
- `CCE.Api.Internal/Endpoints/ExpertEndpoints.cs` (NEW: `MapExpertEndpoints`)
- `CCE.Api.Internal/Program.cs` (modify — call `MapExpertEndpoints`)
- `CCE.Application.Tests/Identity/Queries/ListExpertRequestsQueryHandlerTests.cs` (~4 tests)
- `CCE.Api.IntegrationTests/Endpoints/ExpertRequestsEndpointTests.cs` (~2 tests)

**Acceptance:**
- Default sort `SubmittedOn DESC`.
- Status filter accepts `Pending`/`Approved`/`Rejected` enum names; case-insensitive.
- DTO: `Id, RequestedById, RequestedByUserName?, RequestedBioAr, RequestedBioEn, RequestedTags, SubmittedOn, Status, ProcessedById?, ProcessedOn?`.
- Endpoint gated by `Permissions.Community_Expert_ApproveRequest`.
- 200 + paged shape with admin token; 401 anonymous.

**Commit:** `feat(api-internal): GET /api/admin/expert-requests + ICceDbContext accessors (Phase 2.1)`

---

## Task 2.2 — `POST /api/admin/expert-requests/{id}/approve`

**Files (new):**
- `CCE.Application/Identity/Dtos/ExpertProfileDto.cs`
- `CCE.Application/Identity/IExpertWorkflowService.cs`
- `CCE.Infrastructure/Identity/ExpertWorkflowService.cs`
- `CCE.Infrastructure/DependencyInjection.cs` (modify — register service)
- `CCE.Application/Identity/Commands/ApproveExpertRequest/ApproveExpertRequestCommand.cs` (Id, AcademicTitleAr, AcademicTitleEn)
- `.../ApproveExpertRequestCommandValidator.cs` (Id not empty; titles not empty)
- `.../ApproveExpertRequestCommandHandler.cs` — load request, call `request.Approve(adminId, clock)`, build profile via `CreateFromApprovedRequest`, persist via service, return DTO.
- `CCE.Api.Internal/Endpoints/ExpertEndpoints.cs` (modify — `MapPost("/{id:guid}/approve")`)
- Tests: handler (~4: not-found, no actor, already approved, happy) + validator (~3) + integration (~2).

**Acceptance:**
- 200 + `ExpertProfileDto` on success.
- 404 if request missing.
- 400 if request not Pending (DomainException).
- 400 if titles empty (validation).
- DomainEvent fires (verified via existing dispatcher coverage; not re-tested here).
- Endpoint gated by `Permissions.Community_Expert_ApproveRequest`.

**Commit:** `feat(api-internal): POST /api/admin/expert-requests/{id}/approve (Phase 2.2)`

---

## Task 2.3 — `POST /api/admin/expert-requests/{id}/reject`

**Files (new):**
- `CCE.Application/Identity/Commands/RejectExpertRequest/RejectExpertRequestCommand.cs` (Id, RejectionReasonAr, RejectionReasonEn)
- `.../RejectExpertRequestCommandValidator.cs`
- `.../RejectExpertRequestCommandHandler.cs` — load request, call `request.Reject(adminId, reasonAr, reasonEn, clock)`, persist via `service.UpdateRequestAsync`, return updated `ExpertRequestDto`.
- `CCE.Api.Internal/Endpoints/ExpertEndpoints.cs` (modify — `MapPost("/{id:guid}/reject")`)
- Tests: handler (~4: not-found, no actor, already-processed, happy) + validator (~3) + integration (~2).

**Acceptance:**
- 200 + `ExpertRequestDto` (now in Rejected status with reasons populated).
- 404 / 400 / 401 same patterns as 2.2.
- Endpoint gated by `Permissions.Community_Expert_ApproveRequest`.

**Commit:** `feat(api-internal): POST /api/admin/expert-requests/{id}/reject (Phase 2.3)`

---

## Task 2.4 — `GET /api/admin/expert-profiles`

**Files (new):**
- `CCE.Application/Identity/Queries/ListExpertProfiles/ListExpertProfilesQuery.cs` (Page, PageSize, Search?)
- `.../ListExpertProfilesQueryHandler.cs`
- `CCE.Api.Internal/Endpoints/ExpertEndpoints.cs` (modify — `MapGet` on a new `expert-profiles` MapGroup)
- Tests: handler (~3) + integration (~2).

**Acceptance:**
- 200 + `PagedResult<ExpertProfileDto>`.
- Search matches case-insensitive against UserName/Email of the linked user (JOIN to Users).
- Sort by `ApprovedOn DESC`.
- Endpoint gated by `Permissions.Community_Expert_ApproveRequest`.

**Commit:** `feat(api-internal): GET /api/admin/expert-profiles (Phase 2.4)`

---

## Phase 02 — completion checklist

- [ ] 4 endpoints live; `IExpertWorkflowService` shipped; `ICceDbContext` extended with two accessors.
- [ ] Build clean; full test suite green.
- [ ] +~24 tests (handler + validator + integration across 4 tasks).
- [ ] 4 atomic commits.
- [ ] `contracts/openapi.internal.json` regenerated; drift script clean.

When all boxes ticked, Phase 02 is complete.
