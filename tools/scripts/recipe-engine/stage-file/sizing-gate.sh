#!/usr/bin/env bash
# stage-file Phase 2.4 — sizing gate.
#
# Args: --slug <plan-slug> --stage-id <X.Y>
# Source rule: ia/rules/stage-sizing-gate.md — H1..H6 heuristics.
#
# Phase D MVP scope: this helper applies only the structural heuristics that
# are decidable from DB state without subagent prose inspection:
#   H1 (task count ≤ 8)        — FAIL if > 8 tasks → recipe halt → split.
#   H2 (task name diversity)   — heuristic stub (skip until corpus signal).
#   H3..H6                     — punt to subagent (prose / surface-touch reads).
#
# Verdict policy:
#   PASS → exit 0 + stdout `sizing=PASS hits=...`.
#   WARN → exit 0 + stdout `sizing=WARN hits=...` (subagent surfaces).
#   FAIL → exit 1 + stderr `sizing=FAIL ...` (recipe halts; route /stage-decompose).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"
# shellcheck disable=SC1091
source "${REPO_ROOT}/tools/scripts/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

slug=""
stage_id=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)     slug="$2"; shift 2 ;;
    --stage-id) stage_id="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$stage_id" || -z "${DATABASE_URL:-}" ]]; then
  echo "sizing-gate: missing --slug / --stage-id / DATABASE_URL" >&2
  exit 1
fi

stage_norm="${stage_id#Stage }"
stage_norm="${stage_norm// /}"

task_count_query=$(cat <<SQL
SELECT COUNT(*)
  FROM ia_tasks
 WHERE slug = '${slug//\'/\'\'}'
   AND stage_id = '${stage_norm//\'/\'\'}'
   AND status IN ('pending','draft');
SQL
)
task_count=$(psql "$DATABASE_URL" -tAc "$task_count_query" 2>/dev/null | tr -d '[:space:]' || true)

if [[ "$task_count" -gt 8 ]]; then
  echo "sizing=FAIL H1 task_count=${task_count} > 8 — split Stage ${stage_norm} via /stage-decompose" >&2
  exit 1
fi

if [[ "$task_count" -ge 7 ]]; then
  echo "sizing=WARN H1 task_count=${task_count} approaching cap; consider split"
  exit 0
fi

echo "sizing=PASS H1 task_count=${task_count}"
exit 0
