#!/usr/bin/env bash
# section-closeout — Phase 1b: assert zero intra-plan arch drift.
# Args: [--affected-stages <json>] ... (one --affected-stages per affected stage)
#
# Recipe engine flattenArgs passes an array as repeated --key value pairs.
# Zero occurrences = no drift = gate passes.
# Any occurrence = drift found = exit 1 (stops recipe, blocks closeout).
#
# Emits one machine-readable line on stdout:
#   "drift-gate: ok — 0 affected stages"
#   (stderr on failure: "drift-gate: FAIL — N affected stage(s); ...")
set -euo pipefail

count=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --affected-stages|--affected_stages)
      count=$((count + 1))
      shift 2
      ;;
    *)
      shift
      ;;
  esac
done

if [[ $count -gt 0 ]]; then
  echo "drift-gate: FAIL — ${count} affected stage(s); resolve arch drift before section closeout" >&2
  exit 1
fi

echo "drift-gate: ok — 0 affected stages"
