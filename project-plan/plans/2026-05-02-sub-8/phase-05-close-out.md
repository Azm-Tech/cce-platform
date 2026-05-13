# Phase 05 — Polish + ADRs + close-out (Sub-8)

> Parent: [`../2026-05-02-sub-8.md`](../2026-05-02-sub-8.md) · Spec: [`../../specs/2026-05-02-sub-8-design.md`](../../specs/2026-05-02-sub-8-design.md) §11 (close-out)

**Phase goal:** Land the architecture decisions Sub-8 introduced as ADRs, write the completion document mirroring the Sub-7 structure, update CHANGELOG, and tag `web-portal-v0.3.0`.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 04 closed (commit `a642b8f` or later); 83 suites · 445 tests green.

---

## Task 5.1: ADR-0047 — Single-page workbench (no wizard)

`docs/adr/0047-scenario-builder-single-page-workbench.md`. Status: Accepted. Justifies the single-page layout over a multi-step wizard for the scenario builder. Mirrors the structure of Sub-7's ADRs (Context / Decision / Consequences).

## Task 5.2: ADR-0048 — Client-side live totals + server-authoritative on Run

`docs/adr/0048-client-live-totals-server-authoritative-run.md`. Status: Accepted. Documents why we sum carbon + cost client-side on toggle (no per-toggle network round-trip) but still post the configuration to `/scenarios/run` on the Run button to validate against the server's authoritative numbers + a localized summary.

## Task 5.3: Completion document `docs/sub-8-interactive-city-completion.md`

Same shape as `docs/sub-7-knowledge-maps-completion.md` (tag, date, spec/plan refs, summary, phase checklist, endpoint coverage, ADRs, test counts, bundle impact, UX decisions, polish backlog, stack matrix, next steps).

## Task 5.4: CHANGELOG entry + tag

Update `CHANGELOG.md` with a `web-portal-v0.3.0` section listing what shipped in Sub-8. Then `git tag web-portal-v0.3.0`.

## Phase 05 close-out

- ADRs land alphabetically in `docs/adr/`.
- Completion doc cross-references the ADRs + spec + plan.
- CHANGELOG mirrors the entry.
- Tag exists locally (`git tag -l 'web-portal-v0.3.0'` shows it).
- Final test/lint/build still green.
