#!/usr/bin/env bash
# Regenerate OpenAPI specs by running each API on a port and curling /swagger/v1/swagger.json.
# Avoids the Swashbuckle.AspNetCore.Cli local tool (NuGet install proved flaky on the build host).
# Phase 16 CI runs this and asserts the working tree is clean afterwards.

set -euo pipefail

cd "$(dirname "$0")/.."

# Pick free ports that are unlikely to clash with the running stack.
EXT_PORT="${EXT_PORT:-15001}"
INT_PORT="${INT_PORT:-15002}"

# Cleanup any previous run
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
if [[ "$ext_ok" != "200" ]]; then
  echo "ERROR: External API swagger endpoint never returned 200 (last=$ext_ok)" >&2
  tail -30 /tmp/cce-api-external-export.log >&2
  exit 1
fi
if [[ "$int_ok" != "200" ]]; then
  echo "ERROR: Internal API swagger endpoint never returned 200 (last=$int_ok)" >&2
  tail -30 /tmp/cce-api-internal-export.log >&2
  exit 1
fi

echo "==> exporting contracts/openapi.external.json"
curl -s "http://localhost:$EXT_PORT/swagger/v1/swagger.json" | jq --sort-keys . > contracts/openapi.external.json
echo "==> exporting contracts/openapi.internal.json"
curl -s "http://localhost:$INT_PORT/swagger/v1/swagger.json" | jq --sort-keys . > contracts/openapi.internal.json

echo "==> done"
ls -l contracts/openapi.*.json
