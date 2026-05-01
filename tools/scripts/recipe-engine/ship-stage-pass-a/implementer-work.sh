#!/usr/bin/env bash
# ship-stage-pass-a Phase 2 — implementer-work stub.
#
# Wave 3 seam wiring pending. When seam.implement is live, this script will
# dispatch the spec-implementer LLM subagent for the given task.
#
# Args:
#   --task-id  <TECH-XXXX>
#   --slug     <plan-slug>
#   --stage-id <X.Y>
#
# Output (stdout, exit 0): "implementer_work=stub task_id=<id>"
set -euo pipefail

task_id=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --task-id)  task_id="$2";  shift 2 ;;
    --slug)     shift 2 ;;
    --stage-id) shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$task_id" ]]; then
  echo "implementer-work: missing --task-id" >&2
  exit 1
fi

echo "implementer_work=stub task_id=${task_id}"
exit 0
