#!/usr/bin/env bash
# stage-file Phase 2.3 — cardinality gate.
#
# Args: --slug <plan-slug> --stage-id <X.Y>
# Pass: pending count ≥ 2 → exit 0 + stdout `cardinality=PASS pending=N`.
# Pause: pending == 1     → exit 1 + stderr `cardinality=PAUSE pending=1` (subagent prompts user).
#
# Source rule: ia/rules/cardinality-gate.md — Stage with 1 task ≈ Stage redundancy
# (rare but legitimate; subagent confirms).
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
  echo "cardinality-gate: missing --slug / --stage-id / DATABASE_URL" >&2
  exit 1
fi

stage_norm="${stage_id#Stage }"
stage_norm="${stage_norm// /}"

slug_safe="${slug//\'/\'\'}"
stage_safe="${stage_norm//\'/\'\'}"
pending=$(psql "$DATABASE_URL" -tAc "SELECT COUNT(*) FROM ia_tasks WHERE slug = '${slug_safe}' AND stage_id = '${stage_safe}' AND status = 'pending'" 2>/dev/null | tr -d '[:space:]' || true)

if [[ "$pending" -ge 2 ]]; then
  echo "cardinality=PASS pending=${pending}"
  exit 0
fi

echo "cardinality=PAUSE pending=${pending} — Stage with <2 pending tasks; subagent must confirm with user before proceeding" >&2
exit 1
