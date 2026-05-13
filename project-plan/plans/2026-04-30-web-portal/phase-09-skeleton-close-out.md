# Phase 09 — Skeleton + close-out

> Parent: [`../2026-04-30-web-portal.md`](../2026-04-30-web-portal.md) · Spec: [`../../specs/2026-04-30-web-portal-design.md`](../../specs/2026-04-30-web-portal-design.md) §5 (`/api/knowledge-maps`, `/api/interactive-city/technologies`, `/api/assistant/query`) + §7 (ADRs)

**Phase goal:** Ship the three Sub-7 placeholder entry-points (Knowledge Maps, Interactive City, Assistant) so users can find them today even though full UX lands in Sub-7. Then write the four sub-6 ADRs (0039–0042), the completion doc, the CHANGELOG entry, and tag `web-portal-v0.1.0`. Ends with a Lighthouse audit.

**Tasks:** 7
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 08 closed (`2009ec8`).
- web-portal 249/249 Jest passing; admin-cms 218/218; lint + build clean.

---

## Endpoint coverage (skeletons consume real endpoints)

| Endpoint | Method | Phase 09 surface | Anonymous? |
|---|---|---|---|
| `/api/knowledge-maps` | GET | Task 9.1 (KnowledgeMapsListPage) | ✓ |
| `/api/interactive-city/technologies` | GET | Task 9.2 (InteractiveCityPage) | ✓ |
| `/api/assistant/query` | POST | Task 9.3 (AssistantPage) | ✓ |

The skeleton pages render real data so the integration is verified in v0.1.0; the **interactive scenario UI**, **map graph visualization**, and **assistant streaming chat** all defer to Sub-7. Each page surfaces a "Coming in Sub-7" notice next to the data it can show today.

---

## Task 9.1: Knowledge Maps skeleton page

**Files (all new):**
- `features/knowledge-maps/knowledge-maps.types.ts`
- `features/knowledge-maps/knowledge-maps-api.service.{ts,spec.ts}`
- `features/knowledge-maps/knowledge-maps-list.page.{ts,html,scss,spec.ts}`
- `features/knowledge-maps/routes.ts`
- Modify: `apps/web-portal/src/app/app.routes.ts` — add `/knowledge-maps`.
- Modify: `core/layout/nav-config.ts` — already has `nav.knowledgeMaps`? If not, add.

KnowledgeMapsApiService.listMaps() → `Result<KnowledgeMap[]>` (GET `/api/knowledge-maps`).

KnowledgeMapsListPage: list of maps (id, localized name, description, node-count if returned), each row a deep-link to `/knowledge-maps/:id` (skeleton subpage rendering "Detailed graph view coming in Sub-7"). Plus a top "Notice: detailed view coming in Sub-7" alert.

Tests (~3): service GET, page init load + render, empty state.

Commit: `feat(web-portal): Knowledge Maps skeleton page (Phase 9.1)`

---

## Task 9.2: Interactive City skeleton page

**Files (all new):**
- `features/interactive-city/interactive-city.types.ts`
- `features/interactive-city-api.service.{ts,spec.ts}` (technologies-list only)
- `features/interactive-city/interactive-city.page.{ts,html,scss,spec.ts}`
- `features/interactive-city/routes.ts`
- Modify: `apps/web-portal/src/app/app.routes.ts`.

Service: `listTechnologies()` → `Result<Technology[]>`. Renders a chip-grid + "Scenario builder coming in Sub-7" notice. **No** scenario-run / save / delete UI in v0.1.0 even though backend exposes them — those land with the full UX in Sub-7.

Tests (~3): service GET, page init, empty state.

Commit: `feat(web-portal): Interactive City skeleton page (Phase 9.2)`

---

## Task 9.3: Assistant skeleton page

**Files (all new):**
- `features/assistant/assistant-api.service.{ts,spec.ts}`
- `features/assistant/assistant.page.{ts,html,scss,spec.ts}`
- `features/assistant/routes.ts`
- Modify: `apps/web-portal/src/app/app.routes.ts`.

Service: `query({ question, locale })` → `Result<{ reply: string }>` (POST `/api/assistant/query` returns whatever AskAssistant returns — for v0.1.0 we shape it as a single reply string).

AssistantPage: simple input + reply-bubble UI. Type a question, hit Send, render the reply text. Adds an "Conversational threading + streaming + citations coming in Sub-7" notice. Anonymous OK (server route is `AllowAnonymous`).

Tests (~4): service POST, page submit + reply rendering, empty input blocks send, error renders inline banner.

Commit: `feat(web-portal): Assistant skeleton page (Phase 9.3)`

---

## Task 9.4: ADRs 0039 + 0040 + 0041 + 0042

**Files (all new):**
- `docs/adr/0039-bff-cookie-auth-anonymous-first.md`
- `docs/adr/0040-hybrid-layout-top-nav-and-filter-rail.md`
- `docs/adr/0041-same-origin-scoped-http-interceptors.md`
- `docs/adr/0042-anonymous-friendly-write-affordances.md`

Each ADR follows the established Sub-5 template (`docs/adr/0035-*.md` reference): Status, Date, Deciders, Context (with options table when relevant), Decision, Consequences.

All four bundled into a single docs commit.

Commit: `docs(adr): web-portal ADRs 0039-0042 (Phase 9.4)`

---

## Task 9.5: Completion doc

**Files (all new):**
- `docs/web-portal-completion.md`

Completion doc contents:
- Phase-by-phase checklist (all phases 0-9 ticked).
- Test counts: web-portal **249/249** + Phase 9 additions, admin-cms **218/218**, ui-kit **27/27**.
- Endpoint coverage map (which of the 46 External API endpoints are covered, which intentionally deferred).
- ADR references (0039–0042).
- Phase 9 polish backlog summary (carried forward from each phase plan's "Phase 9 polish" sections).
- Stack / version / build matrix.
- Next steps (Sub-7).

Commit: `docs(sub-6): web-portal completion doc (Phase 9.5)`

---

## Task 9.6: CHANGELOG entry

**Files (modified):**
- `CHANGELOG.md` — insert above `admin-cms-v0.1.0` entry. Format mirrors that prior release.

Commit: `chore(sub-6): CHANGELOG entry for web-portal-v0.1.0 (Phase 9.6)`

---

## Task 9.7: Tag + Lighthouse audit

**Operations:**
1. Verify all preceding tasks committed; full test sweep + lint + build clean.
2. Tag the merge commit:
   ```bash
   git tag -a web-portal-v0.1.0 -m "Sub-6: External Web Portal v0.1.0"
   ```
3. Lighthouse audit on Home + Knowledge Center list pages; record scores in `docs/web-portal-completion.md`. Threshold: Performance ≥ 80 (DoD §3.11 / §9). If below, document the blocking issue but do NOT roll back the tag.

If Lighthouse cannot be run in the local environment (no Chrome / no internet), document the deferral in the completion doc; tag still ships.

Commit (tag-only commit if any deferral note added): `chore(sub-6): tag web-portal-v0.1.0 + Lighthouse audit (Phase 9.7)`

---

## Phase 09 — completion checklist

- [ ] Task 9.1 — Knowledge Maps skeleton page (~3 tests).
- [ ] Task 9.2 — Interactive City skeleton page (~3 tests).
- [ ] Task 9.3 — Assistant skeleton page (~4 tests).
- [ ] Task 9.4 — ADRs 0039–0042 committed with `Status: Accepted`.
- [ ] Task 9.5 — `docs/web-portal-completion.md` written.
- [ ] Task 9.6 — `CHANGELOG.md` entry above `admin-cms-v0.1.0`.
- [ ] Task 9.7 — `web-portal-v0.1.0` tag created; Lighthouse audit recorded (or deferral documented).
- [ ] All Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.

**If all boxes ticked, Sub-6 SHIPPED. Hand-off to Sub-7 begins.**
