# ADR-0008: Version pins (.NET 8, Angular 18.2, ngx-translate, Signals)

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §3](../superpowers/specs/2026-04-24-foundation-design.md#3-stack-decisions)

## Context

A 5+ year government project benefits from explicit, justifiable version pins. Every choice below was a real fork in the road: latest vs LTS, build-time vs runtime i18n, store-based vs Signals-based state.

## Decision

| Layer              | Pin                                                     | Rationale                                                                                                                 |
| ------------------ | ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| Backend runtime    | **.NET 8 LTS**                                          | Long-term support window aligns with gov procurement; out-of-LTS releases (.NET 9) shift workload onto upgrade timelines. |
| Frontend framework | **Angular 18.2**                                        | Stable, Signals-mature, latest Material 18 compatibility.                                                                 |
| i18n               | **ngx-translate** (over `@angular/localize` build-time) | Translations are managed by the CMS at runtime; build-time i18n requires per-locale builds and ships fixed strings.       |
| State              | **Signals + services** (no NgRx in Foundation)          | Foundation scope doesn't need an event-sourced store; NgRx adds boilerplate without payback at this stage.                |

## Consequences

### Positive

- Predictable upgrade cadence — .NET 8 supported into late 2026, Angular 18 supported into late 2025-early 2026 (then bump to next LTS).
- Runtime translations let the CMS drive content without rebuilds.
- Less state-management boilerplate; clearer code paths.

### Negative

- ngx-translate doesn't get xliff/xlf message extraction tooling that `@angular/localize` does — translation export pipeline must be designed (sub-project 5).
- A future sub-project (notably 7 — feature modules) may require richer state; revisit NgRx then.

### Neutral / follow-ups

- Re-evaluate Angular and .NET pins each sub-project cycle.
- A pin change is itself an ADR.

## Alternatives considered

### Option A: .NET 9 (latest)

- Rejected: STS support window is 18 months; gov projects prefer LTS.

### Option B: `@angular/localize` build-time

- Rejected: doesn't support runtime CMS-driven translations.

### Option C: NgRx from day one

- Rejected: premature for Foundation; revisit in feature modules cycle.

## Related

- [ADR-0002](0002-angular-over-react.md)
- `backend/Directory.Packages.props`
- `frontend/package.json`
