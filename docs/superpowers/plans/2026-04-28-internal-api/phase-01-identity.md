# Phase 01 — Identity admin endpoints

> Parent: [`../2026-04-28-internal-api.md`](../2026-04-28-internal-api.md) · Spec: [`../../specs/2026-04-28-internal-api-design.md`](../../specs/2026-04-28-internal-api-design.md) §3.7 (Phase 1)

**Phase goal:** Ship the first 6 admin endpoints under `/api/admin/users` and `/api/admin/state-rep-assignments`, with permission-claim plumbing wired so JWT-authenticated SuperAdmins actually pass `[Authorize(Policy = "User.Read")]` checks.

**Tasks:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 00 closed at `7a8f60d` (Internal API contract drift-clean; UserSyncMiddleware mounted; PagedResult / 409 mapping / Audit.Read in place).
- 385 backend tests passing (Domain 284 / Application 17 / Infrastructure 30 + 1 skipped / Architecture 12 / SourceGen 10 / Api Integration 36).

---

## Pre-execution sanity checks

1. `git status` clean apart from `.claude/`.
2. `dotnet build backend/CCE.sln --no-restore` 0 warnings / 0 errors.
3. `dotnet test backend/CCE.sln --no-build --no-restore` 6 result lines, all `Passed!` totalling 385 + 1 skipped.

---

## Endpoint catalog

| # | Verb + path | Permission | Body / query | Returns |
|---|---|---|---|---|
| 1.1 | `GET /api/admin/users?page=1&pageSize=20&search=...&role=...` | `User.Read` | – | `PagedResult<UserListItemDto>` |
| 1.2 | `GET /api/admin/users/{id}` | `User.Read` | – | `UserDetailDto` (or 404) |
| 1.3 | `PUT /api/admin/users/{id}/roles` | `User.RoleAssign` | `{ Roles: string[], RowVersion: byte[] }` | `UserDetailDto` (or 409 / 422) |
| 1.4 | `GET /api/admin/state-rep-assignments?page=1&pageSize=20&userId=...&countryId=...&active=true` | `User.RoleAssign` | – | `PagedResult<StateRepAssignmentDto>` |
| 1.5 | `POST /api/admin/state-rep-assignments` | `User.RoleAssign` | `{ UserId, CountryId }` | `201 StateRepAssignmentDto` (or 409 duplicate-active) |
| 1.6 | `DELETE /api/admin/state-rep-assignments/{id}` | `User.RoleAssign` | – | `204` (or 404 / 409 already revoked) |

(Permission names match what's already in `permissions.yaml`. Confirm the exact identifiers via the source-generated `Permissions` class.)

---

## Cross-cutting work in Task 1.1

The first endpoint forces us to confront an unfinished Foundation seam: the existing `PermissionPolicyRegistration` (foundation) registers an authorization policy per permission name keyed on a `groups` claim equal to the permission. But Keycloak emits *role-name* groups (e.g., `SuperAdmin`), not 42 permission claims. So a SuperAdmin's JWT today carries `groups=SuperAdmin` and any `[Authorize(Policy = "User.Read")]` check fails.

Task 1.1 wires up an `IClaimsTransformation` that, on each authenticated request, expands the role-name groups in the principal into the corresponding permission claims via the source-generated `RolePermissionMap`. Cached for the request lifetime; principal is re-issued with the expanded claims. After this, every later endpoint just declares `[Authorize(Policy = Permissions.X.Y)]` and it works.

Place the transformer in `CCE.Api.Common.Authorization.RoleToPermissionClaimsTransformer` (file `RoleToPermissionClaimsTransformer.cs`); register from `AddCcePermissionPolicies(...)`.

---

## Application + Internal API layout (Phase 01 contributions)

```
backend/src/CCE.Application/
└── Identity/
    ├── Queries/ListUsers/ListUsersQuery.cs
    ├── Queries/ListUsers/ListUsersQueryHandler.cs
    ├── Queries/GetUserById/GetUserByIdQuery.cs
    ├── Queries/GetUserById/GetUserByIdQueryHandler.cs
    ├── Queries/ListStateRepAssignments/ListStateRepAssignmentsQuery.cs
    ├── Queries/ListStateRepAssignments/ListStateRepAssignmentsQueryHandler.cs
    ├── Commands/AssignUserRoles/AssignUserRolesCommand.cs
    ├── Commands/AssignUserRoles/AssignUserRolesCommandHandler.cs
    ├── Commands/AssignUserRoles/AssignUserRolesCommandValidator.cs
    ├── Commands/CreateStateRepAssignment/CreateStateRepAssignmentCommand.cs
    ├── Commands/CreateStateRepAssignment/CreateStateRepAssignmentCommandHandler.cs
    ├── Commands/CreateStateRepAssignment/CreateStateRepAssignmentCommandValidator.cs
    ├── Commands/RevokeStateRepAssignment/RevokeStateRepAssignmentCommand.cs
    ├── Commands/RevokeStateRepAssignment/RevokeStateRepAssignmentCommandHandler.cs
    └── Dtos/{UserListItemDto,UserDetailDto,StateRepAssignmentDto}.cs

backend/src/CCE.Application/Common/Interfaces/ICceDbContext.cs   ← extend with DbSets
backend/src/CCE.Api.Common/Authorization/RoleToPermissionClaimsTransformer.cs  ← NEW (Task 1.1)
backend/src/CCE.Api.Internal/Endpoints/IdentityEndpoints.cs       ← NEW (extension MapIdentityEndpoints)
backend/src/CCE.Api.Internal/Program.cs                            ← MODIFY (call MapIdentityEndpoints)

backend/tests/CCE.Application.Tests/Identity/...                  ← unit tests (NSubstitute + FluentAssertions)
backend/tests/CCE.Api.IntegrationTests/Endpoints/UsersEndpointTests.cs              ← integration tests (one test class per endpoint group)
backend/tests/CCE.Api.IntegrationTests/Endpoints/StateRepAssignmentsEndpointTests.cs
```

`ICceDbContext` currently exposes only `SaveChangesAsync()`. To let handlers query without depending on `CceDbContext` directly, add typed `IQueryable<>` accessors as needed: `Users`, `Roles`, `StateRepresentativeAssignments`, `Countries`. Implementing `ICceDbContext.Users => Users.AsQueryable()` in `CceDbContext` keeps Application out of `Microsoft.AspNetCore.Identity` while giving handlers the LINQ surface they need.

---

## Common testing pattern

- **Handler unit tests** (`CCE.Application.Tests`) use `NSubstitute` for `ICceDbContext` (with EF in-memory sometimes for query-shape testing) + xUnit + FluentAssertions. Each handler: happy + validation-fail + concurrency-fail (where applicable) + not-found (where applicable). Assertions hit the returned `PagedResult<T>` / DTO / Unit response.
- **Endpoint integration tests** (`CCE.Api.IntegrationTests`) use `WebApplicationFactory<CCE.Api.Internal.Program>` with the existing Keycloak `client_credentials` token-acquisition pattern from `EndToEndAuthFlowTests`. Each endpoint: 200/201/204 + 401 (no token) + 403 (token without permission) + happy. The Phase 01 task brief will instruct the implementer to add a Foundation-style `AdminAuthFixture` (issues a SuperAdmin JWT via Keycloak service-account, caches once per fixture) the first time it's needed (Task 1.1).

---

## Task 1.1 — `GET /api/admin/users` + claims transformer + DTOs

**Files (new):**
- `backend/src/CCE.Application/Common/Interfaces/ICceDbContext.cs` (modify — add `IQueryable<User> Users { get; }`, `IQueryable<Role> Roles { get; }`, `IQueryable<IdentityUserRole<Guid>> UserRoles { get; }`)
- `backend/src/CCE.Infrastructure/Persistence/CceDbContext.cs` (modify — implement the new interface members)
- `backend/src/CCE.Application/Identity/Dtos/UserListItemDto.cs`
- `backend/src/CCE.Application/Identity/Dtos/UserDetailDto.cs`
- `backend/src/CCE.Application/Identity/Queries/ListUsers/ListUsersQuery.cs` (record with `Page`, `PageSize`, `Search`, `Role`)
- `backend/src/CCE.Application/Identity/Queries/ListUsers/ListUsersQueryHandler.cs`
- `backend/src/CCE.Api.Common/Authorization/RoleToPermissionClaimsTransformer.cs`
- `backend/src/CCE.Api.Common/Authorization/PermissionPolicyRegistration.cs` (modify — register the transformer)
- `backend/src/CCE.Api.Internal/Endpoints/IdentityEndpoints.cs` (`MapGet("/api/admin/users")` calling MediatR)
- `backend/src/CCE.Api.Internal/Program.cs` (modify — call `app.MapIdentityEndpoints();`)
- `backend/tests/CCE.Application.Tests/Identity/Queries/ListUsersQueryHandlerTests.cs` (4 tests: empty / single page / search filter / role filter)
- `backend/tests/CCE.Api.IntegrationTests/Endpoints/UsersEndpointTests.cs` (3 tests: 200 with admin token / 401 anonymous / 403 token without `User.Read` permission — but achieving 403 requires a non-SuperAdmin token, which the existing harness can't emit easily; if not feasible, ship just 200 + 401 and note the 403 in a follow-up TODO)
- `backend/tests/CCE.Api.IntegrationTests/Identity/AdminAuthFixture.cs` (helper — issues SuperAdmin token via Keycloak `client_credentials`, caches; reused by later phases)

**Acceptance:**
- `GET /api/admin/users` returns 200 with `PagedResult<UserListItemDto>`. `UserListItemDto` includes `Id`, `Email`, `UserName`, `Roles` (string[]), `IsActive`.
- Filters: `search` matches case-insensitive against `UserName` or `Email` (LIKE `%term%`); `role` filters to users in that role.
- Pagination: defaults `page=1`, `pageSize=20`. `pageSize` clamped to `[1, 100]` via `PaginationExtensions`.
- `RoleToPermissionClaimsTransformer` adds `groups` claims (the SAME claim type the existing policy uses) for every permission in `RolePermissionMap.<roleName>` for each role-name group already on the principal. Idempotent (runs once per principal). After the transformer runs, `[Authorize(Policy = "User.Read")]` passes for SuperAdmin.
- Endpoint annotated `[Authorize(Policy = Permissions.User_Read)]`. Permission name comes from the source-generated constant.
- 4 unit tests pass; integration tests pass (at least 200 + 401; 403 if feasible).
- Build clean (0/0).

**Verify:** `dotnet test backend/CCE.sln --no-build --no-restore` net new tests +6 to +7.

**Commit:** `feat(api-internal): GET /api/admin/users + role→permission claims transformer (Phase 1.1)`

---

## Task 1.2 — `GET /api/admin/users/{id}`

**Files:**
- `backend/src/CCE.Application/Identity/Queries/GetUserById/GetUserByIdQuery.cs` (record `Id`)
- `backend/src/CCE.Application/Identity/Queries/GetUserById/GetUserByIdQueryHandler.cs` — returns `UserDetailDto?` (null → endpoint returns 404)
- `backend/src/CCE.Api.Internal/Endpoints/IdentityEndpoints.cs` (modify — add the `MapGet("/api/admin/users/{id:guid}")` line)
- `backend/tests/CCE.Application.Tests/Identity/Queries/GetUserByIdQueryHandlerTests.cs` (3 tests: returns DTO when found / null when not found / DTO includes role list with names)
- `backend/tests/CCE.Api.IntegrationTests/Endpoints/UsersEndpointTests.cs` (modify — add 200/404 tests; both auth'd as SuperAdmin)

**Acceptance:**
- `GET /api/admin/users/{id}` returns 200 + `UserDetailDto` when the user exists, 404 ProblemDetails when not. `UserDetailDto` includes `Id`, `Email`, `UserName`, `LocalePreference`, `KnowledgeLevel`, `Interests`, `CountryId`, `AvatarUrl`, `Roles`.
- Endpoint gated by `[Authorize(Policy = Permissions.User_Read)]`.

**Commit:** `feat(api-internal): GET /api/admin/users/{id} (Phase 1.2)`

---

## Task 1.3 — `PUT /api/admin/users/{id}/roles`

**Files:**
- `backend/src/CCE.Application/Identity/Commands/AssignUserRoles/AssignUserRolesCommand.cs` (record `Id`, `Roles` (string[]), `RowVersion` (byte[]))
- `.../AssignUserRolesCommandValidator.cs` — validates Roles is non-null, contains only known role names (use `Permissions.RoleNames` if available, else hardcode the 5 seeded names), no duplicates, `RowVersion.Length == 8`
- `.../AssignUserRolesCommandHandler.cs` — load user via `_db.Users.FindAsync(id)`, validate `RowVersion` match (if `User` has rowversion; otherwise document why concurrency check is omitted — User entity doesn't use RowVersion in sub-project 2's design, so fall back to "no concurrency check; admin tools are single-operator"), compute role diffs (add new, remove missing, leave existing), persist via `_db.SaveChangesAsync()`. Returns updated `UserDetailDto`.
- `backend/src/CCE.Api.Internal/Endpoints/IdentityEndpoints.cs` (modify — `MapPut("/api/admin/users/{id:guid}/roles")`)
- `backend/tests/CCE.Application.Tests/Identity/Commands/AssignUserRolesCommandHandlerTests.cs` (4 tests: happy add / happy remove / unknown role rejected / not-found returns 404)
- `backend/tests/CCE.Application.Tests/Identity/Commands/AssignUserRolesCommandValidatorTests.cs` (3 tests: valid / empty roles array allowed / duplicate role rejected)
- Update `UsersEndpointTests.cs` for 200 + 401 + 422 paths.

**Acceptance:**
- `PUT /api/admin/users/{id}/roles` body shape: `{ "roles": ["SuperAdmin"], "rowVersion": "AAAAAAAAB9E=" }` (base64 byte[]).
- 200 + `UserDetailDto` on success.
- 404 if user missing; 422 (FluentValidation) if validation fails.
- Concurrency: if `User.RowVersion` IS modeled (verify in Phase 2 entity inspection), 409 on RowVersion mismatch; otherwise this protection is documented as out-of-scope-for-Phase-01 in the commit message body and a follow-up issue captured.
- Endpoint gated by `[Authorize(Policy = Permissions.User_RoleAssign)]`.

**Commit:** `feat(api-internal): PUT /api/admin/users/{id}/roles (Phase 1.3)`

---

## Task 1.4 — `GET /api/admin/state-rep-assignments`

**Files:**
- `backend/src/CCE.Application/Identity/Dtos/StateRepAssignmentDto.cs`
- `backend/src/CCE.Application/Identity/Queries/ListStateRepAssignments/ListStateRepAssignmentsQuery.cs` (record `Page`, `PageSize`, `UserId`, `CountryId`, `Active`)
- `.../ListStateRepAssignmentsQueryHandler.cs`
- `backend/src/CCE.Application/Common/Interfaces/ICceDbContext.cs` (modify — add `IQueryable<StateRepresentativeAssignment> StateRepresentativeAssignments { get; }` if not already added)
- `backend/src/CCE.Infrastructure/Persistence/CceDbContext.cs` (modify — expose the corresponding `DbSet<>` projected as `IQueryable<>`)
- `backend/src/CCE.Api.Internal/Endpoints/IdentityEndpoints.cs` (modify — add `MapGet("/api/admin/state-rep-assignments")`)
- `backend/tests/CCE.Application.Tests/Identity/Queries/ListStateRepAssignmentsQueryHandlerTests.cs` (4 tests: no filters / userId filter / countryId filter / active=false includes revoked)
- `backend/tests/CCE.Api.IntegrationTests/Endpoints/StateRepAssignmentsEndpointTests.cs` (NEW — 200 / 401)

**Acceptance:**
- `GET /api/admin/state-rep-assignments` returns 200 + `PagedResult<StateRepAssignmentDto>`. `StateRepAssignmentDto` includes `Id`, `UserId`, `UserName`, `CountryId`, `CountryName` (if joining countries is feasible; otherwise just `CountryId`), `AssignedOn`, `AssignedById`, `RevokedOn`, `RevokedById`, `IsActive` (= `RevokedOn is null && !IsDeleted`).
- Filter `active=true` (default) restricts to active assignments via the existing soft-delete query filter (which already excludes `IsDeleted = true` rows).
- Filter `active=false` queries `IgnoreQueryFilters()` to include revoked rows.
- Endpoint gated by `[Authorize(Policy = Permissions.User_RoleAssign)]`.

**Commit:** `feat(api-internal): GET /api/admin/state-rep-assignments (Phase 1.4)`

---

## Task 1.5 — `POST /api/admin/state-rep-assignments`

**Files:**
- `backend/src/CCE.Application/Identity/Commands/CreateStateRepAssignment/CreateStateRepAssignmentCommand.cs` (record `UserId`, `CountryId`)
- `.../CreateStateRepAssignmentCommandValidator.cs` — validates UserId/CountryId not empty Guids
- `.../CreateStateRepAssignmentCommandHandler.cs` — verifies the user exists; verifies the country exists; calls `StateRepresentativeAssignment.Assign(userId, countryId, currentAdminId, clock)`; persists; returns `StateRepAssignmentDto`. Relies on the existing filtered unique index (`(UserId, CountryId) WHERE IsDeleted = 0`) to enforce "no duplicate active assignment" — duplicate insert fails with `DuplicateException` from sub-project 2's `DbExceptionMapper`, which Phase 0's middleware already maps to 409.
- `backend/src/CCE.Application/Common/Interfaces/ICurrentUserAccessor.cs` — verify `GetActor()` returns a Guid-parseable string for the JWT sub. If not, extend it with `GetUserId()` returning `Guid?`. (Foundation's `SystemCurrentUserAccessor` returns `"system"` — for the Internal API host, we need an `HttpContextCurrentUserAccessor` that reads the JWT sub. Add this in Task 1.5 as part of the work.)
- `backend/src/CCE.Api.Internal/Identity/HttpContextCurrentUserAccessor.cs` — NEW (registers via `AddInternalApiServices()` or inline DI, replaces the SystemCurrentUserAccessor for this host).
- `backend/src/CCE.Api.Internal/Program.cs` (modify — register the HttpContext-based accessor).
- `backend/src/CCE.Api.Internal/Endpoints/IdentityEndpoints.cs` (modify — add `MapPost("/api/admin/state-rep-assignments")`)
- Tests: handler (3: happy / unknown user / unknown country) + validator (2: valid / empty Guid rejected) + endpoint integration (201 / 401 / 409 duplicate).

**Acceptance:**
- 201 with `Location: /api/admin/state-rep-assignments/{newId}` and body = `StateRepAssignmentDto`.
- 409 with type `https://cce.moenergy.gov.sa/problems/duplicate` if the (UserId, CountryId) pair already has an active assignment.
- 422 if UserId or CountryId is empty Guid (validation).
- 400 if UserId references a non-existent user, or CountryId references a non-existent country (handled in handler — return `KeyNotFoundException` → 404 by Foundation's existing pipeline; or document choosing 400 with explanation).
- Endpoint gated by `[Authorize(Policy = Permissions.User_RoleAssign)]`.

**Commit:** `feat(api-internal): POST /api/admin/state-rep-assignments (Phase 1.5)`

---

## Task 1.6 — `DELETE /api/admin/state-rep-assignments/{id}`

**Files:**
- `backend/src/CCE.Application/Identity/Commands/RevokeStateRepAssignment/RevokeStateRepAssignmentCommand.cs` (record `Id`)
- `.../RevokeStateRepAssignmentCommandHandler.cs` — load by Id, call `assignment.Revoke(currentAdminId, clock)`, save. Returns `Unit`.
- `backend/src/CCE.Api.Internal/Endpoints/IdentityEndpoints.cs` (modify — `MapDelete("/api/admin/state-rep-assignments/{id:guid}")`)
- Tests: handler (3: happy / not-found / already-revoked → DomainException → 400) + endpoint integration (204 / 404 / 401).

**Acceptance:**
- 204 No Content on success.
- 404 if the assignment doesn't exist.
- 400 with type `https://cce.moenergy.gov.sa/problems/invariant` if the assignment is already revoked (the domain method throws `DomainException` which Phase 0 already maps).
- Endpoint gated by `[Authorize(Policy = Permissions.User_RoleAssign)]`.

**Commit:** `feat(api-internal): DELETE /api/admin/state-rep-assignments/{id} (Phase 1.6)`

---

## Phase 01 — completion checklist

- [ ] 6 endpoints live: `GET /users`, `GET /users/{id}`, `PUT /users/{id}/roles`, `GET /state-rep-assignments`, `POST /state-rep-assignments`, `DELETE /state-rep-assignments/{id}`.
- [ ] `RoleToPermissionClaimsTransformer` flattens role-name groups → permission groups; SuperAdmin requests pass `[Authorize(Policy = Permissions.User_Read)]`.
- [ ] `HttpContextCurrentUserAccessor` registered in Internal API; handlers can resolve the acting admin's Guid from the JWT sub.
- [ ] `ICceDbContext` exposes `IQueryable<>` accessors for User/Role/UserRole/StateRepresentativeAssignment entities; Application handlers use them without referencing Infrastructure.
- [ ] Application + IntegrationTests grow by ~25-30 net (handler unit tests + endpoint integration tests).
- [ ] `dotnet build backend/CCE.sln` 0/0; full test suite green.
- [ ] `contracts/openapi.internal.json` regenerated (will gain ~6 path entries); drift script clean.
- [ ] 6 atomic commits.

When all boxes ticked, Phase 01 is complete. Phase 02 (Expert workflow) follows the same pattern.
