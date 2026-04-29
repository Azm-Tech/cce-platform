# Phase 01 — Public reads

> Parent: [`../2026-04-29-external-api.md`](../2026-04-29-external-api.md) · Spec: [`../../specs/2026-04-29-external-api-design.md`](../../specs/2026-04-29-external-api-design.md) §3.4 (Phase 1)

**Phase goal:** Ship 14 anonymous-OK public read endpoints under `/api/...` for news, events, resources, pages, homepage, topics, categories, and countries. Each returns ONLY published content (`PublishedOn != null` for News/Events/Resources; `IsActive = true` for HomepageSections; `IsActive` for Topics/Categories; not soft-deleted everywhere).

**Tasks:** 4 (consolidated from the original 9 in the master plan)
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 00 closed at `84dbf65`. 813 + 1 skipped tests; build clean.

## Endpoint catalog

| # | Group | Endpoints | Source |
|---|---|---|---|
| 1.1 | News + Events public | `GET /api/news`, `GET /api/news/{slug}`, `GET /api/events`, `GET /api/events/{id}`, `GET /api/events/{id}.ics` | News, Events |
| 1.2 | Resources public | `GET /api/resources`, `GET /api/resources/{id}`, `GET /api/resources/{id}/download` | Resource, AssetFile |
| 1.3 | Pages + Homepage + Taxonomies | `GET /api/pages/{slug}`, `GET /api/homepage-sections`, `GET /api/topics`, `GET /api/categories` | Page, HomepageSection, Topic, ResourceCategory |
| 1.4 | Countries public | `GET /api/countries`, `GET /api/countries/{id}/profile` | Country, CountryProfile |

## Cross-cutting

- **Public DTOs are intentionally narrower than admin DTOs** — they omit `IsDeleted`, internal IDs (UploadedById, AuthorId), and edit-affordance fields (RowVersion). Use `PublicNewsDto`, `PublicResourceDto`, etc.
- All endpoints use the existing `ICceDbContext` IQueryables (no new accessors needed).
- All endpoints are anonymous-OK — gated only by output-cache + rate-limiter middleware.
- For `GET /api/resources/{id}/download`: handler verifies `Resource.IsPublished`, loads the linked `AssetFile`, asserts `VirusScanStatus == Clean`, then streams from `IFileStorage.OpenReadAsync`.
- For `GET /api/events/{id}.ics`: handler emits an iCalendar VCALENDAR with one VEVENT. Use a small helper, no NuGet dep.
- All "not-found" returns 404 (using Phase 0 mapping).

## Phase 01 — completion checklist

- [ ] 14 endpoints live, all anonymous-OK.
- [ ] +~30 net tests (handler unit + endpoint integration).
- [ ] 4 atomic commits.
- [ ] Build clean.
- [ ] Suite green.

When all boxes ticked, Phase 01 is complete.
