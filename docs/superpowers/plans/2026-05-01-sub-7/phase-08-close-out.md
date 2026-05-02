# Phase 08 — Close-out

> Parent: [`../2026-05-01-sub-7.md`](../2026-05-01-sub-7.md) · Spec: [`../../specs/2026-05-01-sub-7-design.md`](../../specs/2026-05-01-sub-7-design.md) §11 (ADRs), §12 (DoD), §13 (close-out)

**Phase goal:** Ship Sub-7 by writing the four ADRs, the completion doc, the CHANGELOG entry under `web-portal-v0.2.0`, and tagging the release. Lighthouse audit deferred where the local sandbox lacks headless Chrome (same approach as Sub-6's close-out).

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 07 closed (`35d4240`).
- web-portal: 362/362 Jest tests passing; lint + build clean.

---

## Task 8.1: ADRs 0043–0046

**Files (4 new):**
- `docs/adr/0043-server-driven-graph-layout.md`
- `docs/adr/0044-rtl-graph-mirror-strategy.md`
- `docs/adr/0045-lazy-heavy-graph-deps.md`
- `docs/adr/0046-dual-view-a11y-graph-list.md`

Each follows the established Sub-5 / Sub-6 ADR template (Status, Date, Deciders, Context with options table, Decision, Consequences).

Single bundled commit: `docs(adr): Sub-7 ADRs 0043–0046 (Phase 8.1)`

## Task 8.2: Completion doc

**File (new):** `docs/sub-7-knowledge-maps-completion.md`

Phase-by-phase checklist (all 9 phases ticked), test counts, endpoint coverage, ADR references, polish backlog notes, stack matrix, Lighthouse note.

Commit: `docs(sub-7): Knowledge Maps completion doc (Phase 8.2)`

## Task 8.3: CHANGELOG entry under `web-portal-v0.2.0`

Insert a new release block above `web-portal-v0.1.0`. Same format as prior entries.

Commit: `chore(sub-7): CHANGELOG entry for web-portal-v0.2.0 (Phase 8.3)`

## Task 8.4: Tag `web-portal-v0.2.0`

`git tag -a web-portal-v0.2.0 -m "..."` on the latest commit. No file changes.

## Task 8.5: Lighthouse audit (deferred)

Local sandbox lacks headless Chrome — defer to Sub-8 deployment verification (same as Sub-6). Document the deferral in the completion doc.

---

## Phase 08 — completion checklist

- [ ] Task 8.1 — 4 ADRs (0043–0046) committed.
- [ ] Task 8.2 — Completion doc committed.
- [ ] Task 8.3 — CHANGELOG entry committed.
- [ ] Task 8.4 — Tag `web-portal-v0.2.0` created.
- [ ] Task 8.5 — Lighthouse deferral documented.

**If all boxes ticked, Sub-7 SHIPPED.**
