# Sub-project 05: Admin / CMS Portal

## Goal

Build the Angular admin application that manages users, roles, content, taxonomies, settings, translations, reports, and audit-log inspection. RTL/LTR bilingual, Angular Material components, Bootstrap grid, DGA tokens, axe-clean, role-gated navigation. Consumes the Internal API (sub-project 3) via the generated `api-client` lib. After this sub-project, ministry admins have a working CMS.

## BRD references

- §4.1.19–4.1.29 — Admin functional requirements.
- §6.3.9–6.3.16 — Admin-facing forms.
- §6.4 — Reports UI (export CSV / Excel / PDF, schedule).
- §6.2.37–6.2.63 — Admin user stories.

## Dependencies

- Sub-project 3 (Internal API).

## Rough estimate

T-shirt size: **L**.

## DoD skeleton

- [ ] Admin shell: top bar (lang switch, profile menu, logout), side nav.
- [ ] CRUD screens for every §4.1.19–4.1.29 entity.
- [ ] Reactive Forms with typed schemas; FluentValidation errors mapped to field-level messages.
- [ ] Permissions guards on routes + UI elements (sourced from OpenAPI `x-permission`).
- [ ] Reports UI: filters, server-side pagination, export jobs.
- [ ] ngx-translate wired; ar/en messages tracked in CMS.
- [ ] axe-core: zero critical/serious in admin E2E suite.
- [ ] BFF cookie session (matches ADR-0015 target).
- [ ] Frontend coverage ≥ 60%; key services / pipes ≥ 80%.

Refined at this sub-project's own brainstorm cycle.

## Related

- ADRs: [0002](../adr/0002-angular-over-react.md), [0003](../adr/0003-material-bootstrap-grid-dga-tokens.md), [0008](../adr/0008-version-pins.md), [0012](../adr/0012-a11y-axe-and-k6-loadtest.md), [0015](../adr/0015-oidc-code-flow-pkce-bff-cookies.md).
