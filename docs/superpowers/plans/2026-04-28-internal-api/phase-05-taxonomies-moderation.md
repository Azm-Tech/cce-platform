# Phase 05 — Taxonomies + community moderation

> Parent: [`../2026-04-28-internal-api.md`](../2026-04-28-internal-api.md)

**Phase goal:** Ship CRUD for `ResourceCategory` + `Topic` taxonomies and admin soft-delete for community Posts + Replies.

**Tasks:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 04 closed at `ac2182e`. 659 + 1 skipped tests.

## Endpoint catalog

| # | Group | Endpoints | Permission |
|---|---|---|---|
| 5.1 | ResourceCategory list+get | `GET /resource-categories`, `GET /resource-categories/{id}` | `Resource.Center.Upload` (admin context) |
| 5.2 | ResourceCategory create+update+delete | `POST`, `PUT /{id}`, `DELETE /{id}` | `Resource.Center.Upload` |
| 5.3 | Topic list+get | `GET /topics`, `GET /topics/{id}` | `Community.Post.Moderate` |
| 5.4 | Topic create+update+delete | `POST`, `PUT /{id}`, `DELETE /{id}` | `Community.Post.Moderate` |
| 5.5 | DELETE community post (soft) | `DELETE /api/admin/community/posts/{id}` | `Community.Post.Moderate` |
| 5.6 | DELETE community reply (soft) | `DELETE /api/admin/community/replies/{id}` | `Community.Post.Moderate` |

## Cross-cutting

- Extend `ICceDbContext` with `ResourceCategories`, `Topics`, `Posts`, `PostReplies` accessors.
- New services: `IResourceCategoryService`, `ITopicService`, `ICommunityModerationService`.
- All CRUD follows the established pattern from Phases 4 (DTO + ListQuery + GetByIdQuery + CreateCommand + UpdateCommand + DeleteCommand + endpoint mapping + tests).
- Neither `ResourceCategory` nor `Topic` has a RowVersion — concurrency is omitted.

## Phase 05 — completion checklist

- [ ] 11 endpoints live across 3 controller groups.
- [ ] 4 new ICceDbContext accessors.
- [ ] 3 service abstractions.
- [ ] +~50 net new tests.
- [ ] 6 atomic commits.
- [ ] Build clean; full suite green.

When all boxes ticked, Phase 05 is complete.
