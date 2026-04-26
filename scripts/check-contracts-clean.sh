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
