# Sub-Project 02 — Data & Domain — Progress

**Spec:** [`../superpowers/specs/2026-04-27-data-domain-design.md`](../superpowers/specs/2026-04-27-data-domain-design.md)
**Plan:** [`../superpowers/plans/2026-04-27-data-domain.md`](../superpowers/plans/2026-04-27-data-domain.md)
**Brief:** [`02-data-domain.md`](02-data-domain.md)

## Phase status

| # | Phase | Status |
|---|---|---|
| 00 | Bootstrap | ✅ Done |
| 01 | Permissions YAML + source-gen | ✅ Done |
| 02 | Identity | ✅ Done |
| 03 | Content | ⏳ Pending |
| 04 | Country | ⏳ Pending |
| 05 | Community | ⏳ Pending |
| 06 | Knowledge Maps + City + Notif + Surveys | ⏳ Pending |
| 07 | Persistence wiring | ⏳ Pending |
| 08 | Migration | ⏳ Pending |
| 09 | Seeder | ⏳ Pending |
| 10 | Architecture tests + ADRs + release | ⏳ Pending |

## Test totals

| Layer | At start | Current | Target |
|---|---|---|---|
| Domain | 16 | 78 | ~136 |
| Application | 12 | 12 | ~72 |
| Infrastructure | 6 | 6 | ~46 |
| Architecture | 0 | 0 | ~15 |
| Source generator | 0 | 10 | ~20 |
| Api Integration | 28 | 28 | ~38 |
| **Cumulative** | **62** (backend) | **134** | **~327** (backend) |

(Frontend test counts unchanged — sub-project 2 is backend-only.)

## Cross-phase notes

- **2026-04-27 — IDD v1.1 review:** Brand stays "CCE Knowledge Center" (do NOT rename to "Taqah" despite IDD's DNS hostnames). Treat IDD's listed "port 433" as a typo for 443 (HTTPS). Prod DNS hostnames `taqah-ext`/`taqah-int`/`api.taqah`/`Api.admin-portal` baked into env config in sub-project 8.

## Release tag

`data-domain-v0.1.0` will be tagged at end of Phase 10.
