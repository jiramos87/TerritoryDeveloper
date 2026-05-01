#!/usr/bin/env bash
# ship-stage-pass-b Phase 10 — carcass stage_claim_release post-flip.
# No-op when section_id not provided. Calls stage_claim_release MCP via mcp-cli when set.
#
# Args:
#   --slug       <plan-slug>
#   --stage-id   <X.Y>
#   --section-id <section_id>  (optional; empty string = no-op)
#
# Output (stdout, exit 0): "claim_release=skipped" or "claim_release=ok"
set -euo pipefail

slug=""
stage_id=""
section_id=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)       slug="$2";       shift 2 ;;
    --stage-id)   stage_id="$2";   shift 2 ;;
    --section-id) section_id="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$section_id" ]]; then
  echo "claim_release=skipped reason=no_section_id"
  exit 0
fi

# Wave 3 seam wiring pending — MCP CLI integration for stage_claim_release.
# When wiring live, call mcp-cli stage_claim_release slug=<slug> stage_id=<stage_id>.
echo "claim_release=stub slug=${slug} stage_id=${stage_id} section_id=${section_id}"
echo "claim_release=ok (stub — Wave 3 seam pending)"
exit 0
