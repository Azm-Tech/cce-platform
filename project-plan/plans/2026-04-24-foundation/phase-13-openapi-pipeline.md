# Phase 13 — OpenAPI Contract Pipeline

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Bridge backend ↔ frontend through OpenAPI. Backend APIs (External + Internal) export `openapi.json` files to `contracts/` at build time. Frontend `api-client` lib regenerates TypeScript clients from those specs. A drift check (`generate` then assert clean working tree) catches contract mismatches before they reach prod.

**Tasks in this phase:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 12 complete; Docker stack healthy; backend builds clean; frontend libs all green.

---

## Pre-execution sanity checks

1. `git status` clean.
2. `cd backend && dotnet build CCE.sln --nologo 2>&1 | tail -3` → 0 errors.
3. `cd frontend && pnpm nx build api-client 2>&1 | tail -3` → succeeds (placeholder lib from Phase 10).
4. `test ! -f contracts/openapi.external.json && echo OK` → `OK` (no pre-existing exports).

If any fail, stop and report.

---

## Why this matters

Per spec ADR-0009 + §4.4, OpenAPI is the **single contract source** between backend and frontend. Without an automated pipeline:

- Devs rename a DTO field on the backend; frontend keeps using the old name; runtime 500.
- Devs add a new endpoint on the backend; frontend has no typed client; reach for `any`.
- A breaking API change ships to staging unnoticed because no one regenerated.

Foundation's pipeline:

1. **Backend build** runs Swashbuckle CLI → emits `contracts/openapi.{external,internal}.json` deterministically.
2. **Frontend `api-client:generate` target** runs `@hey-api/openapi-ts` → produces `frontend/libs/api-client/src/lib/generated/{external,internal}/` with strongly-typed services + models.
3. **CI drift check** runs `generate` then asserts `git diff --exit-code` — fails if a backend change wasn't reflected in committed clients.

Phase 16 wires the CI workflow; Phase 13 builds the local pipeline.

---

## Task 13.1: Export `openapi.json` from both backend APIs

**Files:**

- Create: `contracts/.gitkeep` (so the directory exists in git)
- Create: `scripts/generate-openapi.sh` (run-and-curl approach)

**Rationale (revised from earlier draft):** The original plan called for `Swashbuckle.AspNetCore.Cli` as a local dotnet tool. In practice that install proved flaky on the build host (NuGet metadata timeout against `api.nuget.org`), and the CLI tool itself can fail to load namespaced top-level-statement Programs (Phase 12 wrapped both Programs in `CCE.Api.External` / `CCE.Api.Internal` namespaces). Switching to **run-the-API-on-a-port-and-curl-/swagger** is simpler, faster, and uses the exact same Swashbuckle code path Swagger UI uses at runtime — guaranteed to match what real clients see.

- [ ] **Step 1: Create `contracts/.gitkeep`**

```bash
mkdir -p contracts
touch contracts/.gitkeep
```

- [ ] **Step 2: Write `scripts/generate-openapi.sh`**

```bash
#!/usr/bin/env bash
# Regenerate OpenAPI specs by running each API on a port and curling /swagger/v1/swagger.json.
# Phase 16 CI runs this and asserts the working tree is clean afterwards.

set -euo pipefail

cd "$(dirname "$0")/.."

EXT_PORT="${EXT_PORT:-15001}"
INT_PORT="${INT_PORT:-15002}"

cleanup() {
  if [[ -n "${EXT_PID:-}" ]] && kill -0 "$EXT_PID" 2>/dev/null; then
    kill "$EXT_PID" 2>/dev/null || true
    wait "$EXT_PID" 2>/dev/null || true
  fi
  if [[ -n "${INT_PID:-}" ]] && kill -0 "$INT_PID" 2>/dev/null; then
    kill "$INT_PID" 2>/dev/null || true
    wait "$INT_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

echo "==> dotnet build backend (Debug)"
dotnet build backend/CCE.sln --nologo -c Debug >/dev/null

echo "==> starting CCE.Api.External on port $EXT_PORT"
dotnet run --project backend/src/CCE.Api.External --no-build --urls "http://localhost:$EXT_PORT" \
  > /tmp/cce-api-external-export.log 2>&1 &
EXT_PID=$!

echo "==> starting CCE.Api.Internal on port $INT_PORT"
dotnet run --project backend/src/CCE.Api.Internal --no-build --urls "http://localhost:$INT_PORT" \
  > /tmp/cce-api-internal-export.log 2>&1 &
INT_PID=$!

echo "==> waiting for swagger endpoints to be ready"
for i in $(seq 1 30); do
  ext_ok=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$EXT_PORT/swagger/v1/swagger.json" || echo 000)
  int_ok=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$INT_PORT/swagger/v1/swagger.json" || echo 000)
  if [[ "$ext_ok" == "200" ]] && [[ "$int_ok" == "200" ]]; then
    break
  fi
  sleep 2
done

echo "==> exporting contracts/openapi.external.json"
curl -s "http://localhost:$EXT_PORT/swagger/v1/swagger.json" | jq --sort-keys . > contracts/openapi.external.json
echo "==> exporting contracts/openapi.internal.json"
curl -s "http://localhost:$INT_PORT/swagger/v1/swagger.json" | jq --sort-keys . > contracts/openapi.internal.json

echo "==> done"
ls -l contracts/openapi.*.json
```

```bash
chmod +x scripts/generate-openapi.sh
```

- [ ] **Step 3: Run the script to verify export works**

```bash
./scripts/generate-openapi.sh 2>&1 | tail -10
```

Expected: prints "done" + lists both `openapi.*.json` files with non-zero size.

- [ ] **Step 4: Sanity-check the JSON has expected paths**

```bash
jq -r '.paths | keys[]' contracts/openapi.external.json
jq -r '.paths | keys[]' contracts/openapi.internal.json
```

Expected (External): `/`, `/auth/echo`, `/health`. (Note: `/health/ready` is registered via `MapHealthChecks` which Swashbuckle's default discovery doesn't pick up — that's fine; it's a runtime probe, not a public API surface.)
Expected (Internal): `/`, `/auth/echo`, `/health`, `/health/authenticated`.

No MSBuild post-build hook is wired — Phase 16 invokes `scripts/generate-openapi.sh` directly from CI, and devs run it manually before commits that touch the API surface.

- [ ] **Step 5: Commit**

```bash
git add contracts/ scripts/generate-openapi.sh
git -c commit.gpgsign=false commit -m "feat(phase-13): export OpenAPI specs to contracts/ via run-and-curl helper script"
```

---

## Task 13.2: Install `@hey-api/openapi-ts` and configure for both APIs

**Files:**

- Modify: `frontend/package.json` (add devDependency)
- Create: `frontend/libs/api-client/openapi-ts.config.ts`
- Modify: `frontend/libs/api-client/src/lib/generated/.gitkeep` (replaced after generation)

**Rationale:** `@hey-api/openapi-ts` is the actively-maintained successor to `openapi-typescript-codegen`. Generates strongly-typed services + models for each OpenAPI document. We configure it once in the api-client lib and emit two output dirs (one per API).

- [ ] **Step 1: Install the dev dependency**

```bash
cd frontend
pnpm add -D @hey-api/openapi-ts@0.61.2
cd ..
```

- [ ] **Step 2: Write `frontend/libs/api-client/openapi-ts.config.ts`**

`@hey-api/openapi-ts` 0.61 supports a single config or an array. We use an array to emit two clients:

```typescript
import { defineConfig } from "@hey-api/openapi-ts";

export default defineConfig([
  {
    input: "../../contracts/openapi.external.json",
    output: "src/lib/generated/external",
    plugins: ["@hey-api/typescript", "@hey-api/sdk", "@hey-api/client-fetch"],
  },
  {
    input: "../../contracts/openapi.internal.json",
    output: "src/lib/generated/internal",
    plugins: ["@hey-api/typescript", "@hey-api/sdk", "@hey-api/client-fetch"],
  },
]);
```

The `client-fetch` plugin uses the browser's native `fetch()` API. Phase 14+ swaps it for a custom HttpClient adapter if needed; for Foundation, fetch is fine.

- [ ] **Step 3: Verify the config + spec produce valid output (smoke run)**

```bash
cd frontend/libs/api-client
pnpm exec openapi-ts 2>&1 | tail -20
ls -la src/lib/generated/
cd ../../..
```

Expected: prints "Successfully generated" or similar; `external/` and `internal/` subdirectories appear with `index.ts`, `types.gen.ts`, `sdk.gen.ts`, etc.

If the config can't be found, run with explicit path: `pnpm exec openapi-ts --file openapi-ts.config.ts`.

- [ ] **Step 4: Commit**

```bash
git add frontend/package.json frontend/pnpm-lock.yaml frontend/libs/api-client/openapi-ts.config.ts frontend/libs/api-client/src/lib/generated/
git -c commit.gpgsign=false commit -m "feat(phase-13): add @hey-api/openapi-ts config + generate first TS clients for external + internal APIs"
```

---

## Task 13.3: Add Nx `generate` target for `api-client` + smoke test

**Files:**

- Modify: `frontend/libs/api-client/project.json` (add `generate` target)
- Modify: `frontend/libs/api-client/src/index.ts` (re-export generated)
- Create: `frontend/libs/api-client/src/lib/api-client.spec.ts` (smoke import test)

**Rationale:** Make the regen a first-class Nx target so CI can call `pnpm nx run api-client:generate` consistently. The smoke test imports a generated type to prove the bridge works at type-level.

- [ ] **Step 1: Add `generate` target to `frontend/libs/api-client/project.json`**

Find the `targets` object. Add:

```jsonc
"generate": {
  "executor": "nx:run-commands",
  "options": {
    "cwd": "libs/api-client",
    "command": "pnpm exec openapi-ts"
  },
  "outputs": ["{projectRoot}/src/lib/generated"]
}
```

The `outputs` declaration lets Nx cache the result so re-runs without spec changes are instant.

- [ ] **Step 2: Update `frontend/libs/api-client/src/index.ts`**

Append:

```typescript
// Generated TypeScript clients from contracts/openapi.{external,internal}.json
// Regenerate via: pnpm nx run api-client:generate
export * as ExternalApi from "./lib/generated/external";
export * as InternalApi from "./lib/generated/internal";
```

- [ ] **Step 3: Write the smoke import test**

`frontend/libs/api-client/src/lib/api-client.spec.ts`:

```typescript
import { ExternalApi, InternalApi } from "../index";

describe("api-client", () => {
  it("exports ExternalApi namespace", () => {
    expect(ExternalApi).toBeDefined();
  });

  it("exports InternalApi namespace", () => {
    expect(InternalApi).toBeDefined();
  });

  it("ExternalApi exposes at least one symbol from the generated SDK", () => {
    expect(Object.keys(ExternalApi).length).toBeGreaterThan(0);
  });
});
```

- [ ] **Step 4: Run target via Nx + run tests**

```bash
cd frontend
pnpm nx run api-client:generate 2>&1 | tail -5
pnpm nx test api-client --watch=false 2>&1 | tail -8
pnpm nx build api-client 2>&1 | tail -5
cd ..
```

Expected: all 3 commands succeed. 3 tests pass (the placeholder default test from Phase 10 + 3 new ones = 4 in api-client.spec.ts and possibly the prior placeholder).

If TypeScript compile errors come from generated code (e.g., a missing type), the spec parser may have produced an inconsistent JSON — re-run `./scripts/generate-openapi.sh` and `pnpm nx run api-client:generate`.

- [ ] **Step 5: Commit**

```bash
git add frontend/libs/api-client/
git -c commit.gpgsign=false commit -m "feat(phase-13): add Nx generate target + namespace exports + smoke tests for api-client"
```

---

## Task 13.4: Add drift-check script + manual verification

**Files:**

- Create: `scripts/check-contracts-clean.sh`

**Rationale:** A bash script that regenerates and asserts `git diff` is clean. Phase 16 wires this to GitHub Actions; Foundation lets devs run it locally before opening a PR.

- [ ] **Step 1: Write `scripts/check-contracts-clean.sh`**

```bash
#!/usr/bin/env bash
# Verifies committed OpenAPI specs + generated TS clients match the live backend.
# Used by CI (Phase 16) and devs before opening PRs.

set -euo pipefail

cd "$(dirname "$0")/.."

echo "==> regenerating OpenAPI specs"
./scripts/generate-openapi.sh >/dev/null

echo "==> regenerating frontend TS clients"
(cd frontend && pnpm nx run api-client:generate >/dev/null)

echo "==> diff check"
if [[ -z "$(git status --porcelain contracts/ frontend/libs/api-client/src/lib/generated/)" ]]; then
  echo "OK — contracts and generated clients match committed state."
  exit 0
else
  echo "FAIL — contracts or generated clients have drifted. Diff follows:"
  git --no-pager diff --stat contracts/ frontend/libs/api-client/src/lib/generated/
  echo
  echo "Run ./scripts/generate-openapi.sh && (cd frontend && pnpm nx run api-client:generate) and commit."
  exit 1
fi
```

```bash
chmod +x scripts/check-contracts-clean.sh
```

- [ ] **Step 2: Run the drift check (should pass since we just generated everything)**

```bash
./scripts/check-contracts-clean.sh
```

Expected: prints `OK — contracts and generated clients match committed state.`. Exit 0.

- [ ] **Step 3: Negative test — induce drift to verify the check catches it**

```bash
# Mutate the External API to add a temporary endpoint
sed -i.bak 's|app.MapGet("/", () => "CCE.Api.External — Foundation");|app.MapGet("/", () => "CCE.Api.External — Foundation"); app.MapGet("/_temp_drift_test", () => "drift");|' \
  backend/src/CCE.Api.External/Program.cs

# Run the check — should FAIL
./scripts/check-contracts-clean.sh && echo "BUG: check did not detect drift" || echo "OK — drift was caught"

# Revert the mutation
mv backend/src/CCE.Api.External/Program.cs.bak backend/src/CCE.Api.External/Program.cs

# Regenerate to revert any generated artifacts
./scripts/generate-openapi.sh >/dev/null
(cd frontend && pnpm nx run api-client:generate >/dev/null)

# Verify clean tree
git status --porcelain contracts/ frontend/libs/api-client/src/lib/generated/
```

Expected: middle command prints `OK — drift was caught`. Final `git status` prints nothing (clean).

- [ ] **Step 4: Commit**

```bash
git add scripts/check-contracts-clean.sh
git -c commit.gpgsign=false commit -m "feat(phase-13): add scripts/check-contracts-clean.sh drift detector for CI + dev pre-PR check"
```

---

## Phase 13 — completion checklist

- [ ] `Swashbuckle.AspNetCore.Cli` installed as a local dotnet tool.
- [ ] `contracts/openapi.external.json` and `contracts/openapi.internal.json` committed and well-formed.
- [ ] Both API csprojs have an `ExportOpenApiSpec` post-build MSBuild target.
- [ ] `@hey-api/openapi-ts` installed in frontend; `openapi-ts.config.ts` defines two outputs.
- [ ] `frontend/libs/api-client/src/lib/generated/{external,internal}/` has generated TS clients (committed).
- [ ] `pnpm nx run api-client:generate` regenerates without diff.
- [ ] `pnpm nx test api-client` 3 smoke tests pass.
- [ ] `scripts/generate-openapi.sh` and `scripts/check-contracts-clean.sh` are executable.
- [ ] Drift detection works (negative test in Step 3 of Task 13.4).
- [ ] `git status` clean.
- [ ] ~4 new commits.

**If all boxes ticked, phase 13 is complete. Proceed to phase 14 (Playwright + axe-core).**
