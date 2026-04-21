#!/usr/bin/env bash
# Merge a JSON patch into ia/state/runtime-state.json under flock (Guardrail 3).
# Usage: REPO_ROOT=/path tools/scripts/runtime-state-write.sh /path/to/patch.json
# Requires: jq, flock (brew install util-linux on macOS).

set -euo pipefail

if [[ "$(uname)" == "Darwin" ]]; then
  export PATH="/opt/homebrew/opt/util-linux/bin:${PATH}"
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${REPO_ROOT:-$(cd "${SCRIPT_DIR}/../.." && pwd)}"
PATCH_FILE="${1:?usage: runtime-state-write.sh <patch-json-file>}"

if ! command -v flock >/dev/null 2>&1; then
  echo "ERROR: flock not found. Install via: brew install util-linux" >&2
  exit 1
fi
if ! command -v jq >/dev/null 2>&1; then
  echo "ERROR: jq not found" >&2
  exit 1
fi

LOCK="$REPO_ROOT/ia/state/.runtime-state.lock"
STATE="$REPO_ROOT/ia/state/runtime-state.json"
mkdir -p "$(dirname "$STATE")"
touch "$LOCK"

PATCH_JSON="$(cat "$PATCH_FILE")"
TS="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
# Default shape matches tools/schemas/runtime-state.schema.json (merged on first write).
BASE='{"last_verify_exit_code":-1,"last_bridge_preflight_exit_code":-1,"queued_test_scenario_id":null}'

exec 200>"$LOCK"
flock 200

if [[ -f "$STATE" ]]; then
  jq --argjson p "$PATCH_JSON" --arg ts "$TS" '(. * $p) | .updated_at = $ts' "$STATE" > "${STATE}.tmp" && mv "${STATE}.tmp" "$STATE"
else
  jq -n --argjson base "$BASE" --argjson p "$PATCH_JSON" --arg ts "$TS" '($base * $p) | .updated_at = $ts' > "$STATE"
fi
