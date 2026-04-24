# Keycloak realm export

This directory holds the Keycloak realm configuration auto-imported on container startup.

- `realm-export.json` — single-file realm export. Overwritten in Phase 02 of the Foundation plan with the real `cce-internal` + `cce-external` realms, clients, roles, and seeded admin user.

## How import works

The `keycloak` service in `docker-compose.yml` mounts this directory read-only at
`/opt/keycloak/data/import` and runs `start-dev --import-realm`, which imports every
`*.json` file in that directory on startup.

## Do not commit real secrets

Client secrets in `realm-export.json` are **dev-only placeholder values** matched
against the Gitleaks allowlist (`security/gitleaks.toml`). Production clients regenerate
secrets via Keycloak admin API or ADFS federation (handled in sub-project 8).
