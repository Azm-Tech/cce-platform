# Sub-project 03: Internal API

## Goal

Implement the admin-facing REST API (`CCE.Api.Internal`): users, roles, permissions, content, taxonomies, settings, reports, and audit-log query endpoints. Every endpoint is permission-guarded against `permissions.yaml`, validated with FluentValidation, and exposed through OpenAPI for the Admin CMS to consume. After this sub-project, the Admin CMS sub-project (5) has a stable, generated client to call.

## BRD references

- §4.1.19–4.1.29 — Admin functional requirements.
- §6.2.37–6.2.63 — Admin user stories.
- §6.4.1–6.4.9 — Reports.
- §7.1, §7.2 — Internal messages and alerts.

## Dependencies

- Sub-project 2 (Data & Domain).

## Rough estimate

T-shirt size: **L**.

## DoD skeleton

- [ ] Endpoints for every BRD §4.1.19–4.1.29 admin requirement.
- [ ] All endpoints permission-guarded via `[HasPermission(Permissions.X.Y)]`.
- [ ] FluentValidation on every command DTO.
- [ ] Audit log entry on every state-changing operation.
- [ ] OpenAPI `internal-api.yaml` exported on each build; drift check green.
- [ ] Integration tests cover happy + auth-fail + validation-fail paths for each endpoint.
- [ ] Reports (§6.4) emit CSV + Excel + PDF as BRD specifies.
- [ ] API + Application coverage ≥ 70%.
- [ ] Sentry wired; structured Serilog logs to `logs/`.

Refined at this sub-project's own brainstorm cycle.

## Related

- ADRs: [0009](../adr/0009-openapi-as-contract-source.md), [0013](../adr/0013-permissions-source-generated-enum.md), [0014](../adr/0014-clean-architecture-layering.md).
