#!/usr/bin/env bash
# Phase B.7c — Visual diff gate.
# Runs Stage5VisualFunctional.VisualDiff_* PlayMode tests via testmode-batch.
# Hard-fails (exit 1) when any SSIM < 0.95 for panels with existing baselines.
# Skipped (exit 0) when no Assets/Resources/UI/Generated/** touched in stage diff.
#
# Args (env or positional):
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

# Skip when no UI/Generated assets touched.
if [[ -z "$TOUCHED_ASSETS" ]] || [[ "$TOUCHED_ASSETS" == "[]" ]] || [[ "$TOUCHED_ASSETS" == "null" ]]; then
  if ! echo "$TOUCHED_ASSETS" | grep -q "Assets/Resources/UI/Generated" 2>/dev/null; then
    echo "[B.7c visual-diff] No UI/Generated assets touched — skipping."
    exit 0
  fi
fi

echo "[B.7c visual-diff] Running VisualDiff_* tests for slug=$SLUG stage=$STAGE_ID"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../../" && pwd)"
FILTER="Stage5VisualFunctional.VisualDiff_"

# Delegate to unity:testmode-batch helper.
npm --prefix "$REPO_ROOT" run unity:testmode-batch -- \
  --filter "$FILTER" \
  --testPlatform PlayMode \
  --output "/tmp/visual-diff-result-${SLUG}-${STAGE_ID}.xml"

EXIT_CODE=$?
if [[ $EXIT_CODE -ne 0 ]]; then
  echo "[B.7c visual-diff] FAIL — VisualDiff tests returned exit $EXIT_CODE. Aborting before B.8 stage_commit."
  exit 1
fi

echo "[B.7c visual-diff] PASS"
exit 0
