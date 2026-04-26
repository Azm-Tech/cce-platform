# Phase 15 — k6 Load Tests

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Stand up k6 load tests against the External API's `/health` endpoint with declared p95 thresholds (per spec §8.3 + DoD #11–12). One scenario for anonymous load, one for authenticated load (token from Keycloak service-account flow). Phase 16 wires the optional CI workflow.

**Tasks in this phase:** 3
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 14 complete; backend builds clean; Docker stack healthy.

---

## Pre-execution sanity checks

1. `git status` clean.
2. Docker: 5 services healthy.
3. `which k6` — k6 may or may not be installed locally. Foundation runs k6 via Docker Compose `loadtest` profile (no host-side install required).

---

## Task 15.1: Create `loadtest/` scenarios

**Files:**
- Create: `loadtest/scenarios/health-anonymous.js`
- Create: `loadtest/scenarios/health-authenticated.js`
- Create: `loadtest/README.md`

**Rationale:** Foundation thresholds per spec §8.3:
- `/health` (anonymous): p95 < 100ms at 100 VUs × 60s
- `/health/authenticated` (Internal): p95 < 200ms at 50 VUs × 60s

The scenarios use environment variables (`API_BASE_URL`, `KEYCLOAK_URL`) so they run identically against local-Docker, dev, or staging.

- [ ] **Step 1: Create the directory**

```bash
mkdir -p loadtest/scenarios
```

- [ ] **Step 2: Write `loadtest/scenarios/health-anonymous.js`**

```javascript
// k6 — anonymous /health load test
// Run: k6 run loadtest/scenarios/health-anonymous.js
// Or via Docker:  docker compose --profile loadtest run --rm k6 run /scenarios/health-anonymous.js

import http from 'k6/http';
import { check } from 'k6';

const API_BASE_URL = __ENV.API_BASE_URL || 'http://api-external:5001';

export const options = {
  scenarios: {
    anonymous_health: {
      executor: 'constant-vus',
      vus: 100,
      duration: '60s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],          // <1% errors
    http_req_duration: ['p(95)<100'],        // p95 below 100ms
  },
};

export default function () {
  const res = http.get(`${API_BASE_URL}/health`, {
    headers: { 'Accept-Language': 'ar' },
  });
  check(res, {
    'status 200': (r) => r.status === 200,
    'has status:ok': (r) => typeof r.body === 'string' && r.body.includes('"status":"ok"'),
  });
}
```

- [ ] **Step 3: Write `loadtest/scenarios/health-authenticated.js`**

```javascript
// k6 — authenticated load test against Internal API.
// Acquires a service-account token from Keycloak's cce-admin-cms client once at setup,
// then reuses it across all VUs.

import http from 'k6/http';
import { check } from 'k6';

const API_BASE_URL = __ENV.API_BASE_URL || 'http://api-internal:5002';
const KEYCLOAK_URL = __ENV.KEYCLOAK_URL || 'http://keycloak:8080';
const CLIENT_ID = __ENV.OIDC_CLIENT_ID || 'cce-admin-cms';
const CLIENT_SECRET = __ENV.OIDC_CLIENT_SECRET || 'dev-internal-secret-change-me';

export const options = {
  scenarios: {
    authenticated_health: {
      executor: 'constant-vus',
      vus: 50,
      duration: '60s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<200'],
  },
};

export function setup() {
  const tokenResp = http.post(
    `${KEYCLOAK_URL}/realms/cce-internal/protocol/openid-connect/token`,
    {
      grant_type: 'client_credentials',
      client_id: CLIENT_ID,
      client_secret: CLIENT_SECRET,
    },
    { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } },
  );
  if (tokenResp.status !== 200) {
    throw new Error(`token acquisition failed: ${tokenResp.status} ${tokenResp.body}`);
  }
  const token = JSON.parse(tokenResp.body).access_token;
  return { token };
}

export default function (data) {
  // Hit /auth/echo (which only requires authentication, not SuperAdmin policy).
  const res = http.get(`${API_BASE_URL}/auth/echo`, {
    headers: { Authorization: `Bearer ${data.token}` },
  });
  check(res, {
    'status 200 or 403': (r) => r.status === 200 || r.status === 403,
  });
}
```

Note: `/health/authenticated` requires `groups: SuperAdmin` claim which a service-account token doesn't have, so we hit `/auth/echo` instead — same auth pipeline, same JWT validation cost, just a more permissive policy.

- [ ] **Step 4: Write `loadtest/README.md`**

```markdown
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
```

- [ ] **Step 5: Commit**

```bash
git add loadtest/
git -c commit.gpgsign=false commit -m "feat(phase-15): add k6 scenarios for /health (anonymous) + /auth/echo (authenticated) with thresholds"
```

---

## Task 15.2: Add `loadtest` profile to `docker-compose.yml`

**Files:**
- Modify: `docker-compose.yml`

**Rationale:** Adds a `k6` service gated by Compose's `--profile loadtest` flag so it doesn't spin up on normal `docker compose up`. Mounts the scenarios as a volume; uses the official `grafana/k6` image.

- [ ] **Step 1: Append the `k6` service to `docker-compose.yml`**

Add inside the `services:` section (after `clamav`):

```yaml

  k6:
    image: grafana/k6:0.55.0
    profiles: ["loadtest"]
    container_name: cce-k6
    networks:
      - cce-net
    volumes:
      - ./loadtest/scenarios:/scenarios:ro
    environment:
      API_BASE_URL: "http://api-external:5001"
      KEYCLOAK_URL: "http://keycloak:8080"
      OIDC_CLIENT_ID: "cce-admin-cms"
      OIDC_CLIENT_SECRET: "${KEYCLOAK_CLIENT_SECRET_INTERNAL:-dev-internal-secret-change-me}"
    # No `command` — passed at run time via `docker compose --profile loadtest run k6 run /scenarios/...`
```

Note: this references `api-external` and `api-internal` services. Foundation's `docker-compose.yml` doesn't yet include those services (they're added in a deployment phase later). For now, k6 inside the cce-net can reach the host's APIs via `host.docker.internal` — we'll fall back to that.

Update the env vars to use host.docker.internal:

```yaml
    environment:
      API_BASE_URL: "http://host.docker.internal:5001"
      KEYCLOAK_URL: "http://host.docker.internal:8080"
      OIDC_CLIENT_ID: "cce-admin-cms"
      OIDC_CLIENT_SECRET: "${KEYCLOAK_CLIENT_SECRET_INTERNAL:-dev-internal-secret-change-me}"
    extra_hosts:
      - "host.docker.internal:host-gateway"
```

- [ ] **Step 2: Validate compose syntax**

```bash
docker compose config --quiet && echo "OK"
```
Expected: `OK`. The new k6 service should appear under `services` but only when the `loadtest` profile is active.

- [ ] **Step 3: Verify k6 image is reachable (pull manually first since it doesn't auto-pull without the profile flag)**

```bash
docker pull grafana/k6:0.55.0 2>&1 | tail -3
```
Expected: image pulled or already present.

- [ ] **Step 4: Commit**

```bash
git add docker-compose.yml
git -c commit.gpgsign=false commit -m "feat(phase-15): add k6 service to docker-compose under 'loadtest' profile (host.docker.internal pass-through)"
```

---

## Task 15.3: Smoke-run both scenarios + commit

**Files:** None — verification only.

- [ ] **Step 1: Start both backend APIs in background**

```bash
cd backend
dotnet run --project src/CCE.Api.External --urls http://localhost:5001 > /tmp/api-external.log 2>&1 &
EXT_PID=$!
dotnet run --project src/CCE.Api.Internal --urls http://localhost:5002 > /tmp/api-internal.log 2>&1 &
INT_PID=$!
cd ..
sleep 6
curl -s -o /dev/null -w "external=%{http_code} " http://localhost:5001/health
curl -s -o /dev/null -w "internal=%{http_code}\n" http://localhost:5002/
```
Expected: both return 200.

- [ ] **Step 2: Run anonymous scenario via Docker**

```bash
docker compose --profile loadtest run --rm k6 run /scenarios/health-anonymous.js 2>&1 | tail -25
```
Expected: thresholds met (p95 < 100ms, error rate < 1%). Last line shows `running (1m00.0s/1m00s)` then thresholds report.

If thresholds fail:
- p95 high → API performance issue (rare for `/health` which is in-memory).
- error rate high → API not running or unreachable.
Stop and inspect the API logs.

- [ ] **Step 3: Run authenticated scenario via Docker**

```bash
docker compose --profile loadtest run --rm k6 run /scenarios/health-authenticated.js 2>&1 | tail -25
```
Expected: token acquisition succeeds in setup, then 60s of authenticated load against `/auth/echo`. Thresholds met.

- [ ] **Step 4: Stop the APIs**

```bash
kill $EXT_PID $INT_PID 2>/dev/null
wait $EXT_PID $INT_PID 2>/dev/null
```

- [ ] **Step 5: (No commit — verification only)**

---

## Phase 15 — completion checklist

- [ ] `loadtest/scenarios/health-anonymous.js` and `health-authenticated.js` committed.
- [ ] `loadtest/README.md` documents host vs Docker invocation.
- [ ] `docker-compose.yml` has a `k6` service under `profiles: ["loadtest"]`.
- [ ] Both scenarios run green against the local stack with thresholds met.
- [ ] `git status` clean.
- [ ] ~2 new commits.

**If all boxes ticked, phase 15 is complete. Proceed to phase 16 (CI workflows).**
