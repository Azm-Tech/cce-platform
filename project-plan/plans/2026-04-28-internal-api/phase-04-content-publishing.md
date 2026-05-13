# Phase 04 — Content (news + events + pages + homepage)

> Parent: [`../2026-04-28-internal-api.md`](../2026-04-28-internal-api.md) · Spec: [`../../specs/2026-04-28-internal-api-design.md`](../../specs/2026-04-28-internal-api-design.md) §3.7 (Phase 4)

**Phase goal:** Ship full CRUD + workflow endpoints for News, Events, Pages, and Homepage Sections.

**Tasks:** 9
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 03 closed at `e4e4aac`. 530 + 1 skipped backend tests.

---

## Endpoint catalog

| # | Group | Endpoints | Permission |
|---|---|---|---|
| 4.1 | News list+get | `GET /news`, `GET /news/{id}` | `News.Update` (read = admin context, gated on edit perm) |
| 4.2 | News create+update + News.UpdateContent domain method | `POST /news`, `PUT /news/{id}` | `News.Update` |
| 4.3 | News delete+publish | `DELETE /news/{id}`, `POST /news/{id}/publish` | `News.Delete` + `News.Publish` |
| 4.4 | Events list+get+create | `GET /events`, `GET /events/{id}`, `POST /events` | `Event.Manage` |
| 4.5 | Events update+reschedule+delete + Event.UpdateContent | `PUT /events/{id}`, `POST /events/{id}/reschedule`, `DELETE /events/{id}` | `Event.Manage` |
| 4.6 | Pages list+get+create | `GET /pages`, `GET /pages/{id}`, `POST /pages` | `Page.Edit` |
| 4.7 | Pages update+delete | `PUT /pages/{id}`, `DELETE /pages/{id}` | `Page.Edit` |
| 4.8 | Homepage sections list+create+update+delete | `GET /homepage-sections`, `POST /homepage-sections`, `PUT /homepage-sections/{id}`, `DELETE /homepage-sections/{id}` | `Page.Edit` |
| 4.9 | Homepage sections reorder | `POST /homepage-sections/reorder` (body: ordered list of IDs) | `Page.Edit` |

(Permissions match `permissions.yaml`: `News.Publish/Update/Delete`, `Event.Manage`, `Page.Edit`. HomepageSections share `Page.Edit` since there's no dedicated permission.)

---

## Cross-cutting work per task family

Each task family ships:
1. DTO(s) for read shape (`NewsDto`, `EventDto`, etc.) + RowVersion as base64 string where applicable.
2. ListQuery + Handler.
3. GetByIdQuery + Handler.
4. CreateCommand + Validator + Handler.
5. UpdateCommand + Validator + Handler (with RowVersion concurrency where the entity has `byte[] RowVersion`).
6. DeleteCommand + Handler (soft-delete via domain `SoftDelete`).
7. Workflow command + handler (`Publish`, `Reschedule`, `Reorder` as applicable).
8. Service abstraction in Application + Infrastructure implementation (`INewsService`, `IEventService`, `IPageService`, `IHomepageSectionService`).
9. ICceDbContext extension with new `IQueryable<>` accessors.
10. Endpoint group in `CCE.Api.Internal/Endpoints/` (`NewsEndpoints`, etc.).
11. Tests: handler unit (Substitute<ICceDbContext> + Substitute<IService> + LINQ-to-Objects queryables) + minimal integration (anonymous 401 + admin 200 or 404 where applicable).

---

## Domain methods to add

- `News.UpdateContent(titleAr, titleEn, contentAr, contentEn, slug, featuredImageUrl?)` — added in Task 4.2.
- `Event.UpdateContent(titleAr, titleEn, descAr, descEn, locationAr?, locationEn?, onlineMeetingUrl?, featuredImageUrl?)` — added in Task 4.5.

Both audited via existing AuditingInterceptor. Domain tests for each (1 happy + 1 rejection).

---

## Phase 04 — completion checklist

- [ ] 18 endpoints live across 4 controller groups.
- [ ] 4 service abstractions + Infrastructure impls.
- [ ] 4 new ICceDbContext IQueryable accessors (News, Events, Pages, HomepageSections — most already on CceDbContext but not on the interface).
- [ ] 2 new domain methods (News.UpdateContent + Event.UpdateContent) with tests.
- [ ] +~80 net new tests (handler + validator + integration).
- [ ] 9 atomic commits.
- [ ] Build clean; full suite green.
- [ ] OpenAPI contract drift-clean.

When all boxes ticked, Phase 04 is complete.
