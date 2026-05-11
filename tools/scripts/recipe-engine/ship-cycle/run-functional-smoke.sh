#!/usr/bin/env bash
# Phase B.7d — Functional smoke gate.
# Runs Stage5VisualFunctional.FunctionalSmoke_* PlayMode tests.
# Hard-fails (exit 1) on dead-button or wrong-target defects.
# Skipped (exit 0) when no UI assets touched in stage diff.
#
# Args:
#   --slug      <slug>
#   --stage-id  <stage_id>
#   --touched-assets <json-array-string | empty>
set -euo pipefail

SLUG=""
STAGE_ID=""
TOUCHED_ASSETS=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)          SLUG="$2";           shift 2 ;;
    --stage-id)      STAGE_ID="$2";       shift 2 ;;
    --touched-assets) TOUCHED_ASSETS="$2"; shift 2 ;;
    *)               shift ;;
  esac
done

# Skip when no UI assets touched.
if [[ -z "$TOUCHED_ASSETS" ]] || [[ "$TOUCHED_ASSETS" == "[]" ]] || [[ "$TOUCHED_ASSETS" == "null" ]]; then
  if ! echo "$TOUCHED_ASSETS" | grep -q "Assets/" 2>/dev/null; then
    echo "[B.7d functional-smoke] No Assets/ touched — skipping."
    exit 0
  fi
fi

echo "[B.7d functional-smoke] Running FunctionalSmoke_* tests for slug=$SLUG stage=$STAGE_ID"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../../" && pwd)"
FILTER="Stage5VisualFunctional.FunctionalSmoke_"

npm --prefix "$REPO_ROOT" run unity:testmode-batch -- \
  --filter "$FILTER" \
  --testPlatform PlayMode \
  --output "/tmp/functional-smoke-result-${SLUG}-${STAGE_ID}.xml"

EXIT_CODE=$?
if [[ $EXIT_CODE -ne 0 ]]; then
  echo "[B.7d functional-smoke] FAIL — FunctionalSmoke tests returned exit $EXIT_CODE. Aborting before B.8 stage_commit."
  exit 1
fi

echo "[B.7d functional-smoke] PASS"
exit 0
