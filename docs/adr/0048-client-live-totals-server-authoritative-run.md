# ADR-0048 — Client-side live totals + server-authoritative on Run

**Status:** Accepted
**Date:** 2026-05-02
**Deciders:** CCE frontend team

---

## Context

The Sub-8 scenario builder lets users toggle technologies and see two totals update: **carbon impact** (`Σ carbonImpactKgPerYear`) and **cost** (`Σ costUsd`). The backend exposes a `POST /api/interactive-city/scenarios/run` endpoint that accepts a `cityType + targetYear + configurationJson` payload and returns those same two totals plus a localized `summaryAr / summaryEn` string.

Two extreme designs would be:

| Option | Behaviour | Tradeoff |
|---|---|---|
| **Server-only** | Every toggle posts to `/scenarios/run` and the UI shows whatever the server returns. | Always authoritative. Heavy on network — every click is a round-trip. Slow on mobile / poor connections. Backend has to absorb the load. |
| **Client-only** | All math runs in the browser; the server endpoint is never called. | Instant. No round-trip cost. But: no localized summary; the server-side check that the catalog hasn't drifted under us is gone; saved scenarios can't display server-computed totals out of the gate. |
| **Hybrid (chosen)** | Live totals are computed in the browser (sum of selected technologies' fields). The Run button posts the current configuration to `/scenarios/run` and the server response augments the totals with a localized summary string. Server result is cleared whenever the user edits, so it's never stale. | Best of both. Slight code overhead in the store. |

The catalog is a small list (currently 4 technologies; expected to grow to perhaps 30 over the next year). The math is a sum — not something the server has unique knowledge of beyond the catalog values themselves.

## Decision

**`liveTotals` is a `computed` signal in `ScenarioBuilderStore` that sums `carbonImpactKgPerYear` and `costUsd` over the selected technologies, defensively ignoring ids that aren't in the loaded catalog. The Run button posts the current configuration to `POST /api/interactive-city/scenarios/run`, and on success the store sets `serverResult` which the totals bar surfaces beneath the live numbers as a localized summary string. `serverResult` is cleared on every edit so it can never show stale numbers.**

- `liveTotals` updates synchronously on toggle (no debounce, no flicker).
- `serverResult` is `null` after an edit and after `loadFromSaved`; it populates only after a successful Run.
- The `TotalsBar`'s `aria-live="polite"` region reads out the live numbers as they change so screen-reader users get the same instant feedback sighted users do.
- The Run network call also acts as a sanity check: if the catalog drifts (technologies added, prices changed) between the page load and the Run, the server's number will diverge and the user will see the discrepancy.

## Consequences

**Positive:**
- Toggle → live totals update is instant. No spinner, no perceived latency.
- The Run flow still exists, so users get a localized summary and an authoritative result when they want one.
- The page is fully functional offline up to (but not including) Run + Save.
- Backend doesn't have to absorb a request per toggle.

**Negative:**
- Two sources of truth (`liveTotals` + `serverResult`). The store has to clear `serverResult` on every editing action — easy to forget when adding new edit actions later. (Mitigation: `setCityType / setTargetYear / toggle / clear` all explicitly clear it; the spec for the store covers this.)
- If a technology's price changes server-side after the catalog loads, the user will see the old number until they Run. Acceptable given the catalog is curated and changes rarely.

**Neutral:**
- A future optimisation could add a debounced background `runScenario` call so the localized summary stays fresh without a button press. Not in scope for v0.1.0 — the mental model "edit freely, click Run when ready" is clear enough.
