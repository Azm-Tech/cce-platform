# Sub-project 06: External Web Portal

## Goal

Build the Angular public-facing portal: home, knowledge content browse / search, interactive city / knowledge maps entry points, smart-assistant chat, community pages, user profile, notifications, registration / login (via BFF). Bilingual ar/en, RTL/LTR, WCAG 2.1 AA, mobile-responsive, DGA-themed. After this sub-project, public users can consume CCE content.

## BRD references

- §4.1.1–4.1.18 — Public functional requirements.
- §6.3.1–6.3.8 — Public-facing forms.
- §6.2.1–6.2.36 — Public user stories.

## Dependencies

- Sub-project 4 (External API).

## Rough estimate

T-shirt size: **L**.

## DoD skeleton

- [ ] Public shell: header (logo, lang switch, sign-in), footer, breadcrumb.
- [ ] Browse + search content pages with server-side pagination.
- [ ] Interactive city and knowledge maps entry points (full content lands in sub-project 7).
- [ ] Smart-assistant chat UI (skeleton; full integration in sub-project 7).
- [ ] Community feed + post + comment screens.
- [ ] BFF cookie session (login flow round-trips through external-api BFF endpoints).
- [ ] Notifications drawer.
- [ ] axe-core: zero critical/serious in public E2E suite.
- [ ] k6 load thresholds met for representative public pages.
- [ ] Lighthouse Performance ≥ 80 on home + content list.

Refined at this sub-project's own brainstorm cycle.

## Related

- ADRs: [0002](../adr/0002-angular-over-react.md), [0003](../adr/0003-material-bootstrap-grid-dga-tokens.md), [0012](../adr/0012-a11y-axe-and-k6-loadtest.md), [0015](../adr/0015-oidc-code-flow-pkce-bff-cookies.md).
