# Sub-project 07: Feature Modules

## Goal

Build the four feature-rich modules that distinguish CCE from a generic CMS: **Knowledge Maps**, **Interactive City**, **Smart Assistant**, and **Community**. Each is a self-contained feature with its own data model contributions, API endpoints, and Angular feature module. Lands after the public portal shell so the modules can plug into existing navigation and content surfaces.

## BRD references

- §4.1.4 — Knowledge Maps.
- §4.1.5 — Interactive City.
- §4.1.11 — Smart Assistant.
- §4.1.12, §4.1.13 — Community.
- §6.2.6–§6.2.9 — Smart-assistant user stories.
- §6.2.19–§6.2.31 — Community user stories.

## Dependencies

- Sub-project 6 (Web Portal shell).
- Sub-project 8 (Integration Gateway) for smart-assistant provider, if external LLM is used.

## Rough estimate

T-shirt size: **XL** (largest single sub-project — four substantive features).

## DoD skeleton

- [ ] **Knowledge Maps:** browseable graph UI, taxonomy-driven, RTL-aware.
- [ ] **Interactive City:** scene rendering (per UX brief), hotspots, deep-links into content.
- [ ] **Smart Assistant:** chat UI + backend integration; conversation persistence; rate limiting.
- [ ] **Community:** posts, comments, reactions, moderation queue, notifications.
- [ ] All four modules respect permissions and rate limits.
- [ ] axe-core clean; bilingual; mobile-responsive.
- [ ] Module-level coverage gates per Foundation TDD policy.

Refined at this sub-project's own brainstorm cycle (likely four separate brainstorms — one per module).

## Related

- ADRs: [0007](../adr/0007-tdd-strict-backend-test-after-ui.md), [0012](../adr/0012-a11y-axe-and-k6-loadtest.md).
