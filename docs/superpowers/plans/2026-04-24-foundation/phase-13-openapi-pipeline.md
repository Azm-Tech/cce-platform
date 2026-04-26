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

## Task 13.1: Export `openapi.json` from both backend APIs at build time

**Files:**
- Modify: `backend/src/CCE.Api.External/CCE.Api.External.csproj` (add post-build target)
- Modify: `backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj` (same)
- Create: `contracts/.gitkeep` (so the directory exists in git)
- Create: `scripts/generate-openapi.sh` (manual fallback)

**Rationale:** The Swashbuckle CLI tool reads the compiled API assembly + entry point + Swagger config and dumps the OpenAPI spec to a file. Hooking it into MSBuild ensures every backend build refreshes the contracts.

- [ ] **Step 1: Install Swashbuckle.AspNetCore.Cli as a local tool**

```bash
cd backend
dotnet new tool-manifest --force 2>&1 | tail -3
dotnet tool install Swashbuckle.AspNetCore.Cli --version 6.8.1 2>&1 | tail -3
cd ..
```

This creates `backend/.config/dotnet-tools.json` pinning the CLI version. Local-tools approach beats global because the version travels with the repo.

- [ ] **Step 2: Create `contracts/.gitkeep` and the script**

```bash
mkdir -p contracts
touch contracts/.gitkeep
```

`scripts/generate-openapi.sh`:

```bash
#!/usr/bin/env bash
# Manually regenerate OpenAPI specs from the built backend assemblies.
# Phase 16 CI runs this and asserts the working tree is clean afterwards.

set -euo pipefail

cd "$(dirname "$0")/.."

echo "==> dotnet build backend (Debug)"
dotnet build backend/CCE.sln --nologo -c Debug >/dev/null

echo "==> exporting openapi.external.json"
(cd backend && dotnet tool run swagger tofile \
  --output ../contracts/openapi.external.json \
  --serializeasv2 false \
  artifacts/bin/CCE.Api.External/Debug/net8.0/CCE.Api.External.dll \
  v1)

echo "==> exporting openapi.internal.json"
(cd backend && dotnet tool run swagger tofile \
  --output ../contracts/openapi.internal.json \
  --serializeasv2 false \
  artifacts/bin/CCE.Api.Internal/Debug/net8.0/CCE.Api.Internal.dll \
  v1)

echo "==> done"
ls -l contracts/
```

```bash
chmod +x scripts/generate-openapi.sh
```

- [ ] **Step 3: Run the script to verify export works**

```bash
./scripts/generate-openapi.sh 2>&1 | tail -10
```
Expected: prints "done" + lists `openapi.external.json` and `openapi.internal.json` with non-zero size.

- [ ] **Step 4: Sanity-check the JSON has expected paths**

```bash
jq -r '.paths | keys[]' contracts/openapi.external.json
jq -r '.paths | keys[]' contracts/openapi.internal.json
```
Expected (External): `/`, `/auth/echo`, `/health`, `/health/ready` (paths from Phase 08).
Expected (Internal): same structure plus `/health/authenticated`.

- [ ] **Step 5: Add MSBuild post-build hook (optional but useful for CI)**

Append to `backend/src/CCE.Api.External/CCE.Api.External.csproj` before `</Project>`:

```xml
  <Target Name="ExportOpenApiSpec" AfterTargets="Build" Condition="'$(SkipOpenApiExport)' != 'true' AND '$(Configuration)' == 'Debug'">
    <Exec Command="dotnet tool run swagger tofile --output $(MSBuildThisFileDirectory)../../../contracts/openapi.external.json --serializeasv2 false $(MSBuildThisFileDirectory)../../artifacts/bin/CCE.Api.External/Debug/net8.0/CCE.Api.External.dll v1"
          WorkingDirectory="$(MSBuildThisFileDirectory)../.."
          ContinueOnError="true" />
  </Target>
```

Same block in `CCE.Api.Internal.csproj` with paths swapped (`internal` instead of `external` in two places).

`ContinueOnError="true"` and the `Configuration='Debug'` guard prevent this from breaking release builds (e.g., when packaging in CI without the local tool installed).

- [ ] **Step 6: Verify a backend build refreshes the spec**

```bash
rm -f contracts/openapi.*.json
cd backend && dotnet build CCE.sln --nologo -c Debug 2>&1 | tail -5 && cd ..
ls -l contracts/openapi.*.json
```
Expected: both files regenerated.

- [ ] **Step 7: Commit**

```bash
git add backend/.config backend/src/CCE.Api.External/CCE.Api.External.csproj backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj contracts/ scripts/generate-openapi.sh
git -c commit.gpgsign=false commit -m "feat(phase-13): export OpenAPI specs to contracts/ via Swashbuckle CLI + MSBuild post-build target"
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
import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig([
  {
    input: '../../contracts/openapi.external.json',
    output: 'src/lib/generated/external',
    plugins: [
      '@hey-api/typescript',
      '@hey-api/sdk',
      '@hey-api/client-fetch',
    ],
  },
  {
    input: '../../contracts/openapi.internal.json',
    output: 'src/lib/generated/internal',
    plugins: [
      '@hey-api/typescript',
      '@hey-api/sdk',
      '@hey-api/client-fetch',
    ],
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
export * as ExternalApi from './lib/generated/external';
export * as InternalApi from './lib/generated/internal';
```

- [ ] **Step 3: Write the smoke import test**

`frontend/libs/api-client/src/lib/api-client.spec.ts`:

```typescript
import { ExternalApi, InternalApi } from '../index';

describe('api-client', () => {
  it('exports ExternalApi namespace', () => {
    expect(ExternalApi).toBeDefined();
  });

  it('exports InternalApi namespace', () => {
    expect(InternalApi).toBeDefined();
  });

  it('ExternalApi exposes at least one symbol from the generated SDK', () => {
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
