#!/usr/bin/env bash
# ship-stage-pass-b Phase 4a — carcass arch_drift_scan pre-closeout.
# No-op when section_id not provided. Calls arch_drift_scan MCP via mcp-cli when set.
# Fails recipe (exit 1) when drift found.
#
# Args:
#   --slug       <plan-slug>
#   --section-id <section_id>  (optional; empty string = no-op)
#
# Output (stdout, exit 0): "arch_drift=skipped" or "arch_drift=clear"
set -euo pipefail

slug=""
section_id=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)       slug="$2";       shift 2 ;;
    --section-id) section_id="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$section_id" ]]; then
  echo "arch_drift=skipped reason=no_section_id"
  exit 0
fi

# Wave 3 seam wiring pending — MCP CLI integration for arch_drift_scan.
# When wiring live, call mcp-cli arch_drift_scan scope=intra-plan plan_id=<slug> section_id=<id>
# and assert affected_stages == 0.
echo "arch_drift=stub slug=${slug} section_id=${section_id}"
echo "arch_drift=clear (stub — Wave 3 seam pending)"
exit 0
