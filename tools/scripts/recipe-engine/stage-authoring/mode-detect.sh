#!/usr/bin/env bash
# stage-authoring Phase 0 — mode detection.
#
# Args: --slug <plan-slug> --stage-id <X.Y>
# Output (stdout, exit 0):  one line `mode=author pending=N`
# Halt (exit 1):            stage not found / no pending tasks.
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
  echo "mode-detect: missing --slug / --stage-id / DATABASE_URL" >&2
  exit 1
fi

stage_norm="${stage_id#Stage }"
stage_norm="${stage_norm// /}"

stage_exists_query="SELECT 1 FROM ia_stages WHERE slug = '${slug//\'/\'\'}' AND stage_id = '${stage_norm//\'/\'\'}' LIMIT 1;"
stage_exists=$(psql "$DATABASE_URL" -tAc "$stage_exists_query" 2>/dev/null | tr -d '[:space:]' || true)
if [[ -z "$stage_exists" ]]; then
  echo "mode-detect: stage ${stage_norm} not found for slug ${slug}" >&2
  exit 1
fi

pending_query="SELECT COUNT(*) FROM ia_tasks WHERE slug = '${slug//\'/\'\'}' AND stage_id = '${stage_norm//\'/\'\'}' AND status = 'pending';"
pending=$(psql "$DATABASE_URL" -tAc "$pending_query" 2>/dev/null | tr -d '[:space:]' || true)
pending="${pending:-0}"

if [[ "$pending" -ge 1 ]]; then
  echo "mode=author pending=${pending}"
  exit 0
fi

echo "mode-detect: no-op (pending=0) — nothing to author" >&2
exit 1
