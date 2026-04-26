# CCE Load Tests (k6)

## Run via Docker Compose (no host install)

```bash
docker compose --profile loadtest run --rm k6 run /scenarios/health-anonymous.js
docker compose --profile loadtest run --rm k6 run /scenarios/health-authenticated.js
```

## Run via host k6 (requires `brew install k6`)

```bash
# Backend APIs must be running on localhost:5001 + 5002
API_BASE_URL=http://localhost:5001 k6 run loadtest/scenarios/health-anonymous.js
API_BASE_URL=http://localhost:5002 KEYCLOAK_URL=http://localhost:8080 k6 run loadtest/scenarios/health-authenticated.js
```

## Thresholds

| Scenario | VUs | Duration | p95 threshold | Error rate |
|---|---|---|---|---|
| `health-anonymous` | 100 | 60s | < 100 ms | < 1% |
| `health-authenticated` | 50 | 60s | < 200 ms | < 1% |

CI runs these on manual dispatch only (Phase 16).
