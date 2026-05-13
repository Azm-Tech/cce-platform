# Phase 06 — Country admin + notifications admin

> Parent: [`../2026-04-28-internal-api.md`](../2026-04-28-internal-api.md)

**Phase goal:** Ship admin endpoints for `Country` (list + update names/active), `CountryProfile` (get + update with concurrency), and `NotificationTemplate` (list + get + create + update + activate/deactivate).

**Tasks:** 6 (consolidated to 3 commits for efficiency)
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 05 closed at `1729295`. 719 + 1 skipped tests.

## Endpoint catalog

| # | Endpoints | Permission |
|---|---|---|
| 6.1+6.2 | `GET /api/admin/countries` (paged), `GET /api/admin/countries/{id}`, `PUT /api/admin/countries/{id}` (UpdateNames + Activate/Deactivate) | `Country.Profile.Update` |
| 6.3+6.4 | `GET /api/admin/countries/{id}/profile`, `PUT /api/admin/countries/{id}/profile` (CountryProfile, with RowVersion) | `Country.Profile.Update` |
| 6.5+6.6 | `GET /api/admin/notification-templates`, `GET /api/admin/notification-templates/{id}`, `POST /api/admin/notification-templates`, `PUT /api/admin/notification-templates/{id}` | `Notification.TemplateManage` |

(NotificationTemplate has no DELETE — Activate/Deactivate suffices for lifecycle.)

## Cross-cutting

- Extend `ICceDbContext` with `IQueryable<NotificationTemplate> NotificationTemplates`. (`Countries` already added in Task 1.5; `CountryProfiles` lookup happens via existing `CceDbContext.CountryProfiles` DbSet — extend ICceDbContext too.)
- New services: `ICountryAdminService`, `ICountryProfileService`, `INotificationTemplateService`.
- `Country` has no RowVersion — UpdateNames doesn't need concurrency. `CountryProfile` HAS RowVersion — concurrency-checked. `NotificationTemplate` has no RowVersion.

## Phase 06 — completion checklist

- [ ] 9 endpoints live.
- [ ] 3 service abstractions.
- [ ] 2 new ICceDbContext accessors (CountryProfiles + NotificationTemplates).
- [ ] +~40 net tests.
- [ ] 3 atomic commits.
- [ ] Build clean.
