# Sub-Project 03 — Internal API — Completion Report

**Tag:** `internal-api-v0.1.0`
**Date:** 2026-04-29
**Spec:** [Internal API Design Spec](../project-plan/specs/2026-04-28-internal-api-design.md)
**Plan:** [Internal API Implementation Plan](../project-plan/plans/2026-04-28-internal-api.md)

## Tooling versions

```
host:       Darwin 24.3.0 arm64
dotnet:     8.0.125
dotnet-ef:  8.0.10
sql:        Azure SQL Edge 1.0.7 (dev) / SQL Server 2022 (prod target)
git tag preceding: data-domain-v0.1.0
```

## DoD verification (spec §9)

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | All 47+ admin REST endpoints mapped under `/api/admin/*` | PASS | Endpoints: Identity, Expert, Assets, Resources, ResourceCategories, CountryResourceRequests, Countries, CountryProfiles, News, Events, Pages, HomepageSections, Topics, CommunityModeration, NotificationTemplates, Reports (8 CSV), AuditEvents |
| 2 | JWT auth via Keycloak (dev) / ADFS (prod) with `AddCceJwtAuth` | PASS | `CCE.Api.Common/Auth/JwtAuthExtensions.cs`; bearer token validated on every protected endpoint |
| 3 | Permission policies wired via `AddCcePermissionPolicies` | PASS | `CCE.Api.Common/Authorization/CcePermissionPolicies.cs`; 42 policies registered from generated `Permissions` class |
| 4 | JIT user-sync middleware (`UserSyncMiddleware` + `IUserSyncService`) | PASS | `CCE.Api.Common/Middleware/UserSyncMiddleware.cs`; `IMemoryCache` 5-min TTL; [ADR-0027](adr/0027-jit-user-sync-from-keycloak-claims.md) |
| 5 | `IFileStorage` abstraction + `LocalFileStorage` dev implementation | PASS | `CCE.Application.Common.Interfaces.IFileStorage`; `CCE.Infrastructure/Storage/LocalFileStorage.cs`; [ADR-0028](adr/0028-ifilestorage-abstraction-with-localfilestorage.md) |
| 6 | `IClamAvScanner` synchronous TCP virus scan at upload time | PASS | `CCE.Infrastructure/Storage/ClamAvScanner.cs`; rejects infected files with HTTP 422 |
| 7 | 8 streamed CSV reports via `IAsyncEnumerable<TRow>` + CsvHelper | PASS | `ReportEndpoints.cs`; 8 report services; UTF-8 BOM for Excel compat; [ADR-0029](adr/0029-streamed-csv-reports-via-iasyncenumerable.md) |
| 8 | `GET /api/admin/audit-events` with structured filters + pagination | PASS | `AuditEndpoints.cs`; `ListAuditEventsQueryHandler`; filters: actor, actionPrefix, resourceType, correlationId, from, to |
| 9 | `RoleToPermissionClaimsTransformer` maps role name → permission claims | PASS | `CCE.Api.Common/Authorization/RoleToPermissionClaimsTransformer.cs` |
| 10 | `HttpContextCurrentUserAccessor` reads JWT `sub` on Internal API | PASS | `CCE.Api.Internal/Identity/HttpContextCurrentUserAccessor.cs` |
| 11 | `ICurrentUserAccessor` replaced in DI for Internal API host | PASS | `Program.cs`: `services.Replace(ServiceDescriptor.Scoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>())` |
| 12 | Anonymous → 401 verified for every endpoint family | PASS | Integration tests for every endpoint file; 167 Api Integration tests total |
| 13 | SuperAdmin → 200 (or 404 for unknown resource) verified | PASS | `AdminAuthFixture` issues signed JWT; verified across Resources, News, Events, Pages, Reports, AuditEvents etc. |
| 14 | 3 ADRs added (0027–0029) | PASS | `docs/adr/0027-...0029-*.md` |

## Final test totals

| Layer | At start (data-domain-v0.1.0) | Current (internal-api-v0.1.0) | Delta |
|---|---|---|---|
| Domain | 284 | 290 | +6 |
| Application | 12 | 278 | +266 |
| Infrastructure | 30 (+1 skipped) | 37 (+1 skipped) | +7 |
| Architecture | 12 | 12 | 0 |
| Source generator | 10 | 10 | 0 |
| Api Integration | 28 | 167 | +139 |
| **Cumulative backend** | **376** + 1 skipped | **794** + 1 skipped | **+418** |

## Cross-phase notes

- Phase 3.1 (foundation wiring) introduced `HttpContextCurrentUserAccessor`, `UserSyncMiddleware`, and `RoleToPermissionClaimsTransformer` as a foundation shared by all later endpoint phases.
- Architecture tests (Phase 3.1) verified that every handler, endpoint class, and infrastructure type lives in its correct layer — no regressions introduced in later phases.
- `AdminAuthFixture` generates a signed JWT token for `SuperAdmin` using a deterministic RSA key registered in `WebApplicationFactory` — avoids requiring a live Keycloak service in integration tests.
- `LocalFileStorage` writes to a temp directory in tests and cleans up on dispose; real ClamAV connection is skipped in integration tests via `ClamAv:Enabled=false` test config.
- Permission count went from 41 (`data-domain-v0.1.0`) to 42 (`internal-api-v0.1.0`) with the addition of `Audit.Read`.
- `ActionPrefix` filter uses `StartsWith(prefix + ".")` (in-memory LINQ) rather than `EF.Functions.Like` to keep the handler testable with NSubstitute queryables. The compiled SQL uses `LIKE 'prefix.%'` via EF's `StartsWith` translation.

## Known follow-ups (not blockers)

1. **Country scoping deferred** — spec §3.5 describes a `ICountryScopeAccessor` pattern that scopes GET queries to the `StateRepresentative`'s assigned country. This was designed but not implemented in Sub-project 3. ADR-0030 (Country-scoped query pattern) was intentionally skipped to keep ADRs faithful to shipped work. Tracked for Sub-project 5 (Public API) where country filtering is mandatory.
2. **`SuperAdmin` token harness uses embedded RSA key** — the `AdminAuthFixture` signs JWTs with a test-only key pair hard-coded into `CCE.TestInfrastructure`. Rotating this key requires rebuilding the test assembly. A Keycloak service-account flow is planned for sub-project 8 end-to-end tests.
3. **`LocalFileStorage` not replicated** — uploaded files are lost if the dev container is recreated. Sub-project 8 will introduce `S3FileStorage`/`AzureBlobFileStorage` without changing application-layer code (ADR-0028).
4. **ClamAV mock in integration tests** — the `IClamAvScanner` is stubbed to return `Clean` in the `WebApplicationFactory` test host. A real ClamAV integration test is deferred to the sub-project 8 deployment pipeline.
5. **`MigrationParityTests` remains `[Skip]`'d** — inherited from Sub-project 2; run locally before each release.

## Release tag

`internal-api-v0.1.0` annotated tag created at HEAD of `main` after Phase 8 close.
