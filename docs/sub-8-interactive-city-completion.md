# Sub-Project 08 — Interactive City scenario builder — Completion Report

**Tag:** `web-portal-v0.3.0`
**Date:** 2026-05-02
**Spec:** [Interactive City Design Spec](../project-plan/specs/2026-05-02-sub-8-design.md)
**Plan:** [Interactive City Implementation Plan](../project-plan/plans/2026-05-02-sub-8.md)
**Predecessor:** [Sub-7 Knowledge Maps completion](sub-7-knowledge-maps-completion.md)
**Successors (planned):** Sub-9 Smart Assistant, Sub-10 Deployment / Infra

---

## Summary

Sub-8 layers the deep Interactive City UX on top of the Sub-6 Phase 9 skeleton at `/interactive-city`. Anonymous and authenticated users can pick technologies from a catalog, watch carbon and cost totals recompute on every toggle, click Run for a localized server summary, and (when authenticated) save named scenarios and reload them later. URL captures `?city=&year=&t=&name=` for deep-linking. Backend unchanged — every endpoint shipped in Sub-4.

**Total tasks:** ~32 across 6 phases. **Test count: web-portal 445/445 (was 362 at end of Sub-7) · admin-cms 218/218 · ui-kit 27/27 = 690 Jest tests across 83 web-portal suites.**

## Phase checklist

- [x] **Phase 00** — Cross-cutting: extended `interactive-city.types.ts` (RunRequest/Result + SaveRequest + SavedScenario + helpers); 4 new methods on `InteractiveCityApiService`; `lib/url-state.ts` parse/build helpers (10 tests); EN+AR i18n keys (56 each, parity verified); component file scaffolding (5 sub-components + page + store stub).
- [x] **Phase 01** — Store + page shell: `ScenarioBuilderStore` (11 state signals + 5 computed + 11 actions, 22 tests); `ScenarioBuilderPage` URL hydrate via `parseUrlState` + 200ms-debounced sync-back effect (3 page integration tests).
- [x] **Phase 02** — Catalog + selection: `ScenarioHeaderComponent` (Reactive Forms two-way to store, 5 tests); `TechnologyCatalogComponent` (debounced search + locale-aware grouping + click-to-toggle, 5 tests); `SelectedListComponent` (cart with remove/clear, 5 tests); page wires the three slots with sticky placeholder totals.
- [x] **Phase 03** — Run + save: `TotalsBarComponent` (sticky bottom bar, Run + Save buttons, server summary, 7 tests); `SaveScenarioDialogComponent` (single-input MatDialog, 5 tests); auth-gated save with sign-in fallback; success-toast i18n keys.
- [x] **Phase 04** — Saved scenarios drawer: `ConfirmDialogComponent` (reusable confirm/cancel, 4 tests); `SavedScenariosDrawerComponent` (auth-only side rail with Load/Delete, sign-in CTA for anonymous, unsaved-changes guard, 8 tests).
- [x] **Phase 05** — Polish + ADRs + completion: 2 ADRs (0047 single-page workbench, 0048 client live totals + server-auth Run); this completion doc; CHANGELOG entry under `web-portal-v0.3.0`; tag `web-portal-v0.3.0`.

## Endpoint coverage

All endpoints already shipped in Sub-4. Sub-8 adds zero new endpoints.

| Endpoint | Method | Auth | Surface |
|---|---|---|---|
| `/api/interactive-city/technologies` | GET | Anon | Catalog (loaded once on entry) |
| `/api/interactive-city/scenarios/run` | POST | Anon | Run button — authoritative totals + localized summary |
| `/api/me/interactive-city/scenarios` | GET | Auth | Saved scenarios drawer |
| `/api/me/interactive-city/scenarios` | POST | Auth | Save dialog |
| `/api/me/interactive-city/scenarios/{id}` | DELETE | Auth | Delete from drawer |

## ADRs

- [ADR-0047 — Scenario builder uses a single-page workbench (no wizard)](adr/0047-scenario-builder-single-page-workbench.md)
- [ADR-0048 — Client-side live totals + server-authoritative on Run](adr/0048-client-live-totals-server-authoritative-run.md)

## Test counts (final)

| Project | Suites | Tests |
|---|---|---|
| `web-portal` | 83 | 445 (+83 since Sub-7's 362) |
| `admin-cms` | 47 | 218 (unchanged) |
| `ui-kit` | 7 | 27 (unchanged) |
| **Total Jest** | **137** | **690** |

E2E coverage: existing Sub-6/7 specs continue to pass. New Sub-8 specs target the unit + component layer; full-stack E2E for the scenario builder is deferred to Sub-10's deployment verification stage.

## Bundle impact

- **Initial web-portal bundle: unchanged from Sub-7** — no new heavy dependencies.
- The `/interactive-city` lazy chunk grows modestly (new sub-components + Material Dialog/Snackbar usage).

## UX decisions baked in

| Area | Decision | Rationale |
|---|---|---|
| Layout | Single-page workbench (header + catalog + selected + totals + drawer) | ADR-0047 |
| Run vs live | Live totals on the client, authoritative result + summary on Run | ADR-0048 |
| Bilingual name | Single name input writes to both `nameAr` and `nameEn` | YAGNI — admin-cms can split later |
| Save auth gate | Anonymous Save → `auth.signIn()`; authenticated → name dialog → POST | Aligns with the existing BFF cookie + Keycloak flow |
| Saved drawer | CSS-driven (sticky desktop, stack mobile); auth-only with sign-in CTA fallback | Matches Sub-7's `NodeDetailPanelComponent` pattern |
| Unsaved changes | Confirm dialog before `loadFromSaved` when `dirty()` is true | Prevents silent loss of in-progress edits |
| Live totals UX | `aria-live="polite"` so screen readers hear updates | Accessibility |
| URL state | `?city=&year=&t=&name=` round-trips with 200ms debounce | Deep-linking; clean URLs when at defaults |

## Polish backlog (carried forward)

- **Side-by-side scenario comparison** — needs UI + maybe a lightweight `?compare=id1,id2` URL extension.
- **In-place edit (PATCH) of saved scenarios** — server only exposes POST/DELETE today; revisit if real users complain about scenario sprawl.
- **Bilingual name split** — separate `nameAr` / `nameEn` inputs in admin-cms.
- **Drag-to-compose catalog** — would be more visually engaging at the cost of accessibility complexity.
- **Carbon-over-time chart** — backend doesn't expose ramps; revisit when data grows.
- **Catalog pagination + categories filter** — not needed at current 4-technology scale.
- **axe-core a11y CI gate** — manual axe-core spot check passed; CI gate deferred to Sub-10 alongside the Sub-7 Lighthouse audit.

## Stack matrix

| Layer | Version |
|---|---|
| Angular | 19.2.21 (unchanged from Sub-7) |
| Angular Material | 18 |
| ngx-translate | 16.x |
| Nx | 21.x |
| Reactive Forms | built-in |
| TypeScript | 5.x |
| Jest | 30 (workspace), 29-compatible config |
| Playwright | latest stable |

No new heavy dependencies. The lazy-loaded `/interactive-city` chunk is light.

## Next steps (Sub-9 / Sub-10)

- **Sub-9** — Smart Assistant streaming + threading + citations (`/assistant`).
- **Sub-10** — Deployment / Infra (IDD v1.2): production build, CI workflows, Lighthouse + axe-core gates, Kubernetes manifests, observability.
