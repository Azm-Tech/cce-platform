# CCE Roadmap

## Sub-projects

| # | Sub-project | Status | Goal | BRD refs | Depends on |
|---|---|---|---|---|---|
| 1 | Foundation | **In progress (this is Foundation)** | Scaffold + CI + dev infra | NFR §4.1.32 | — |
| 2 | Data & Domain | Pending | Full EF schema, migrations, seed data, permission matrix | §4.1.31, §4.1.32 | 1 |
| 3 | Internal API | Pending | Admin endpoints + reports | §4.1.19–4.1.29, §6.2.37–6.2.63, §6.4.1–6.4.9 | 2 |
| 4 | External API | Pending | Public endpoints + smart-assistant + community | §4.1.1–4.1.18, §6.2.1–6.2.36 | 2 |
| 5 | Admin / CMS Portal | Pending | Angular admin app | §4.1.19–4.1.29, §6.3.9–6.3.16, §6.4 | 3 |
| 6 | External Web Portal | Pending | Angular public app | §4.1.1–4.1.18, §6.3.1–6.3.8 | 4 |
| 7 | Feature Modules | Pending | Knowledge Maps, Interactive City, Smart Assistant, Community | §4.1.4, §4.1.5, §4.1.11, §4.1.12, §4.1.13, §6.2.6–6.2.9, §6.2.19–6.2.31 | 6 |
| 8 | Integration Gateway | Pending | KAPSARC, ADFS, Email, SMS, SIEM, iCal | §6.5, §7.1, §7.2, HLD §3.1.2–3.1.8 | 3, 4 |
| 9 | Mobile (Flutter) | Pending | WebView shell for iOS/Android/Huawei | HLD §3.2.2 | 6 |

## Foundation completion (sub-project 1)

See [`subprojects/01-foundation.md`](subprojects/01-foundation.md) for the brief.
DoD tracked in [Foundation spec §11](superpowers/specs/2026-04-24-foundation-design.md#11-definition-of-done).

## Per-sub-project briefs

- [02 Data & Domain](subprojects/02-data-domain.md)
- [03 Internal API](subprojects/03-internal-api.md)
- [04 External API](subprojects/04-external-api.md)
- [05 Admin / CMS Portal](subprojects/05-admin-cms.md)
- [06 External Web Portal](subprojects/06-web-portal.md)
- [07 Feature Modules](subprojects/07-feature-modules.md)
- [08 Integration Gateway](subprojects/08-integrations.md)
- [09 Mobile (Flutter)](subprojects/09-mobile-flutter.md)

## Traceability

- [`requirements-trace.csv`](requirements-trace.csv) — every BRD section → sub-project mapping.
