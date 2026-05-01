#!/usr/bin/env bash
# ship-stage-pass-b Phase 2 — verify-loop wrapper stub.
#
# Wave 3 seam wiring pending. When seam.verify-loop is live, this script will
# dispatch the verify-loop LLM subagent on cumulative git diff HEAD.
#
# Args:
#   --slug     <plan-slug>
#   --stage-id <X.Y>
#
# Output (stdout, exit 0): "verify_loop=stub slug=<slug> stage_id=<id>"
set -euo pipefail

slug=""
stage_id=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)     slug="$2";     shift 2 ;;
    --stage-id) stage_id="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$stage_id" ]]; then
  echo "verify-loop: missing --slug or --stage-id" >&2
  exit 1
fi

echo "verify_loop=stub slug=${slug} stage_id=${stage_id}"
exit 0
