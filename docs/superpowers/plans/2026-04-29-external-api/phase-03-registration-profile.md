# Phase 03 — Registration + profile

> Parent: [`../2026-04-29-external-api.md`](../2026-04-29-external-api.md)

**Phase goal:** Ship 5 self-service profile endpoints for authenticated users (RegisteredUser+) plus the registration redirect.

**Tasks:** 2 (consolidated)
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 02 closed at `b98b672`. 872 + 1 skipped tests.

## Endpoints

| # | Endpoint | Auth | Notes |
|---|---|---|---|
| 3.1 | `POST /api/users/register` (302 redirect to Keycloak signup), `GET /api/me`, `PUT /api/me` | Anonymous redirect / RegisteredUser+ | Reuse `User` entity setters (`SetLocalePreference`, `SetKnowledgeLevel`, `UpdateInterests`, `SetAvatarUrl`, `AssignCountry`/`ClearCountry`) |
| 3.2 | `POST /api/users/expert-request`, `GET /api/me/expert-status` | RegisteredUser+ | Self-service expert registration submit + status query |

## Cross-cutting

- `GET /api/me` returns the current user's full profile (reads from `users` table via JIT-synced row from Sub-3 Phase 0.4 middleware — but External API doesn't have UserSyncMiddleware; the user must already exist in the table from a prior Internal admin login, OR we add a similar JIT sync to External API).
- For v0.1.0 in External: the user has registered via Keycloak signup, then the FIRST authenticated External API request creates their row via a similar `UserSyncMiddleware` mounted on External. **Phase 03 Task 3.1 mounts the same `UserSyncMiddleware` on External** (the middleware is already in `CCE.Api.Common.Identity` shared between hosts).
- `PUT /api/me` only allows the user to update their own profile fields. No role assignments here.
- `POST /api/users/expert-request` calls `ExpertRegistrationRequest.Submit(...)` (sub-2 entity); persists; returns 201 with `Location: /api/me/expert-status`.
- `GET /api/me/expert-status` returns the latest expert request for the current user (or 404 if none submitted).

## Phase 03 — completion checklist

- [ ] 5 endpoints live + UserSyncMiddleware mounted on External.
- [ ] +~10 net tests.
- [ ] 2 atomic commits.
- [ ] Build clean.
