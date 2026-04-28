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
| 03 | Content | ✅ Done |
| 04 | Country | ✅ Done |
| 05 | Community | ✅ Done |
| 06 | Knowledge Maps + City + Notif + Surveys | ✅ Done |
| 07 | Persistence wiring | ✅ Done |
| 08 | Migration | ✅ Done |
| 09 | Seeder | ✅ Done |
| 10 | Architecture tests + ADRs + release | ✅ Done |

## Test totals

| Layer | At start | Current | Target |
|---|---|---|---|
| Domain | 16 | 284 | ~136 |
| Application | 12 | 12 | ~72 |
| Infrastructure | 6 | 30 (+ 1 skipped) | ~46 |
| Architecture | 0 | 12 | ~15 |
| Source generator | 0 | 10 | ~20 |
| Api Integration | 28 | 28 | ~38 |
| **Cumulative** | **62** (backend) | **376** + 1 skipped | **~327** (backend) |

(Frontend test counts unchanged — sub-project 2 is backend-only.)

## Cross-phase notes

- **2026-04-27 — IDD v1.1 review:** Brand stays "CCE Knowledge Center" (do NOT rename to "Taqah" despite IDD's DNS hostnames). Treat IDD's listed "port 433" as a typo for 443 (HTTPS). Prod DNS hostnames `taqah-ext`/`taqah-int`/`api.taqah`/`Api.admin-portal` baked into env config in sub-project 8.

## Release tag

`data-domain-v0.1.0` annotated tag created 2026-04-28.

Sub-project 2 is **complete** (11/11 phases). See [`docs/data-domain-completion.md`](../data-domain-completion.md) for the full DoD verification report.
