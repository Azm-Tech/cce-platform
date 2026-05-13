# Project Plan

Every sub-project that built CCE went through the same four steps:

1. **Brainstorm** — explore the idea, surface constraints, decompose if needed.
2. **Spec** — capture the agreed design in [`specs/`](specs/).
3. **Plan** — break the spec into a master plan + per-phase detail in [`plans/`](plans/).
4. **Execute** — one commit per task, exact commit messages from the plan.

The brainstorm step lives in chat history; the rest is written down here.

## Sub-projects

CCE was decomposed into **13 sub-projects** ([ADR-0001](../docs/adr/0001-decomposition-9-subprojects.md)). Each sub-project shipped on its own tag — see the master plan for goal + phases, the spec for design rationale, and the completion doc for the release retrospective.

| #    | Sub-project              | Spec                                                       | Master plan                                                       | Phase plans                                                  | Completion                                                              | Tag                                                                                           |
| ---- | ------------------------ | ---------------------------------------------------------- | ----------------------------------------------------------------- | ------------------------------------------------------------ | ----------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| 01   | Foundation               | [spec](specs/2026-04-24-foundation-design.md)              | [plan](plans/2026-04-24-foundation.md)                            | [20 phases](plans/2026-04-24-foundation/)                    | —                                                                       | [`foundation-v0.1.0`](https://github.com/Azm-Tech/cce-platform/releases/tag/foundation-v0.1.0) |
| 02   | Data domain              | [spec](specs/2026-04-27-data-domain-design.md)             | [plan](plans/2026-04-27-data-domain.md)                           | [11 phases](plans/2026-04-27-data-domain/)                   | —                                                                       | [`data-domain-v0.1.0`](https://github.com/Azm-Tech/cce-platform/releases/tag/data-domain-v0.1.0) |
| 03   | Internal API             | [spec](specs/2026-04-28-internal-api-design.md)            | [plan](plans/2026-04-28-internal-api.md)                          | [8 phases](plans/2026-04-28-internal-api/)                   | —                                                                       | [`internal-api-v0.1.0`](https://github.com/Azm-Tech/cce-platform/releases/tag/internal-api-v0.1.0) |
| 04   | External API             | [spec](specs/2026-04-29-external-api-design.md)            | [plan](plans/2026-04-29-external-api.md)                          | [4 phases](plans/2026-04-29-external-api/)                   | —                                                                       | [`external-api-v0.1.0`](https://github.com/Azm-Tech/cce-platform/releases/tag/external-api-v0.1.0) |
| 05   | Admin CMS                | [spec](specs/2026-04-29-admin-cms-design.md)               | [plan](plans/2026-04-29-admin-cms.md)                             | [3 phases](plans/2026-04-29-admin-cms/)                      | —                                                                       | [`admin-cms-v0.1.0`](https://github.com/Azm-Tech/cce-platform/releases/tag/admin-cms-v0.1.0) |
| 06   | Web portal               | [spec](specs/2026-04-30-web-portal-design.md)              | [plan](plans/2026-04-30-web-portal.md)                            | [10 phases](plans/2026-04-30-web-portal/)                    | —                                                                       | [`web-portal-v0.1.0`…`v0.4.0`](https://github.com/Azm-Tech/cce-platform/releases) |
| 07   | Knowledge maps           | [spec](specs/2026-05-01-sub-7-design.md)                   | [plan](plans/2026-05-01-sub-7.md)                                 | [9 phases](plans/2026-05-01-sub-7/)                          | [retro](../docs/sub-7-knowledge-maps-completion.md)                     | —                                                                                             |
| 08   | Interactive city         | [spec](specs/2026-05-02-sub-8-design.md)                   | [plan](plans/2026-05-02-sub-8.md)                                 | [6 phases](plans/2026-05-02-sub-8/)                          | [retro](../docs/sub-8-interactive-city-completion.md)                   | —                                                                                             |
| 09   | Smart Assistant          | [spec](specs/2026-05-02-sub-9-design.md)                   | [plan](plans/2026-05-02-sub-9.md)                                 | [4 phases](plans/2026-05-02-sub-9/)                          | [retro](../docs/sub-9-assistant-completion.md)                          | —                                                                                             |
| 10a  | App productionization    | [spec](specs/2026-05-03-sub-10a-design.md)                 | [plan](plans/2026-05-03-sub-10a.md)                               | [5 phases](plans/2026-05-03-sub-10a/)                        | [retro](../docs/sub-10a-app-productionization-completion.md)            | [`app-v1.0.0`](https://github.com/Azm-Tech/cce-platform/releases/tag/app-v1.0.0)               |
| 10b  | Deployment automation    | [spec](specs/2026-05-03-sub-10b-design.md)                 | [plan](plans/2026-05-03-sub-10b.md)                               | [3 phases](plans/2026-05-03-sub-10b/)                        | [retro](../docs/sub-10b-deployment-automation-completion.md)            | [`deploy-v1.0.0`](https://github.com/Azm-Tech/cce-platform/releases/tag/deploy-v1.0.0)         |
| 10c  | Production infra + DR    | [spec](specs/2026-05-04-sub-10c-design.md)                 | [plan](plans/2026-05-04-sub-10c.md)                               | [6 phases](plans/2026-05-04-sub-10c/)                        | [retro](../docs/sub-10c-production-infra-completion.md)                 | [`infra-v1.0.0`](https://github.com/Azm-Tech/cce-platform/releases/tag/infra-v1.0.0)           |
| 11   | Entra ID migration       | [spec](specs/2026-05-04-sub-11-design.md)                  | [plan](plans/2026-05-04-sub-11.md)                                | [5 phases](plans/2026-05-04-sub-11/)                         | [retro](../docs/sub-11-entra-id-migration-completion.md)                | [`entra-id-v1.0.0`](https://github.com/Azm-Tech/cce-platform/releases/tag/entra-id-v1.0.0)     |

> **Sub-project briefs** — one-page summaries for each sub-project live in [`../docs/subprojects/`](../docs/subprojects/).

## How a plan is structured

```
project-plan/plans/<YYYY-MM-DD>-<slug>.md      ← master plan: goal, scope, phase index
project-plan/plans/<YYYY-MM-DD>-<slug>/        ← per-phase detail
   phase-00-<name>.md                           ← one phase = one shippable slice
   phase-01-<name>.md
   ...
```

Each phase plan is a sequence of bite-sized tasks (2–5 minutes each), with the exact code, command, and verification step in-line. The format is **TDD-first**: the failing test always appears before the implementation, and the commit message is part of the plan.

## How a spec is structured

A spec captures the design decision after brainstorming. Sections vary, but every spec covers:

- **Goal** — one sentence, what this builds
- **Constraints** — what we have to live with (existing code, infra, agreements)
- **Architecture** — high-level shape, component boundaries, data flow
- **Decisions** — chosen approach + rejected alternatives (with rationale)
- **Definition of Done** — testable acceptance criteria
- **Phases** — the natural slicing into shippable pieces

## See also

- [Architecture Decision Records (60+)](../docs/adr/) — every non-trivial decision has an ADR
- [Sub-project briefs](../docs/subprojects/) — one-page summary per sub-project
- [Runbooks](../docs/runbooks/) — operational procedures (backup/restore, DR promotion, secret rotation, env promotion, migrations, rollback)
- [Root README](../README.md) — quickstart + repo map
