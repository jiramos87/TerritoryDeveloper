#!/usr/bin/env bash
# ship-stage-pass-a Phase 2 — compile-gate.
#
# Runs unity:compile-check fast-fail gate for the given task.
# Non-zero exit aborts the Pass A loop before task_status_flip.
#
# Args:
#   --task-id  <TECH-XXXX>
#
# Output (stdout, exit 0): "compile_gate=pass task_id=<id>"
# Exit 1: compile check failed.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"

task_id=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --task-id) task_id="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$task_id" ]]; then
  echo "compile-gate: missing --task-id" >&2
  exit 1
fi

cd "$REPO_ROOT"
if npm run unity:compile-check 2>&1; then
  echo "compile_gate=pass task_id=${task_id}"
  exit 0
else
  echo "compile-gate: unity:compile-check failed for task ${task_id}" >&2
  exit 1
fi
