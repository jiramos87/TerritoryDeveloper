#!/usr/bin/env bash
# stage-file Phase 0 — mode detection.
#
# Args: --slug <plan-slug> --stage-id <X.Y>
# Output (stdout, exit 0):  one line `mode=file pending=N`
# Halt (exit 1):            structured stderr — no-op / not-found.
#
# DB world: task_status enum = pending|implemented|verified|done|archived.
# Legacy `Draft` (markdown era) no longer exists; compress / mixed modes are
# vestigial — handled by a separate skill if reintroduced. For DB-backed
# stage-file the only modes are file (≥1 pending) or no-op (0 pending).
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

if [[ -z "$slug" || -z "$stage_id" ]]; then
  echo "mode-detect: missing --slug / --stage-id" >&2
  exit 1
fi

if [[ -z "${DATABASE_URL:-}" ]]; then
  echo "mode-detect: DATABASE_URL unset (load-repo-env.inc.sh failed)" >&2
  exit 1
fi

# Normalize stage_id: strip "Stage " prefix; allow X.Y or single-int (single-int → match X like "5" → "5%").
stage_norm="${stage_id#Stage }"
stage_norm="${stage_norm// /}"

# Resolve plan + stage by slug + stage_id literal (flat schema — no FK ids).
plan_exists_query="SELECT 1 FROM ia_master_plans WHERE slug = '${slug//\'/\'\'}' LIMIT 1;"
plan_exists=$(psql "$DATABASE_URL" -tAc "$plan_exists_query" 2>/dev/null | tr -d '[:space:]' || true)
if [[ -z "$plan_exists" ]]; then
  echo "mode-detect: master plan slug not found: ${slug}" >&2
  exit 1
fi

stage_exists_query="SELECT 1 FROM ia_stages WHERE slug = '${slug//\'/\'\'}' AND stage_id = '${stage_norm//\'/\'\'}' LIMIT 1;"
stage_exists=$(psql "$DATABASE_URL" -tAc "$stage_exists_query" 2>/dev/null | tr -d '[:space:]' || true)
if [[ -z "$stage_exists" ]]; then
  echo "mode-detect: stage ${stage_norm} not found for slug ${slug}" >&2
  exit 1
fi

count_query="SELECT status, COUNT(*) FROM ia_tasks WHERE slug = '${slug//\'/\'\'}' AND stage_id = '${stage_norm//\'/\'\'}' GROUP BY status;"
counts=$(psql "$DATABASE_URL" -tAc "$count_query" 2>/dev/null || true)

pending=0
while IFS='|' read -r status n; do
  status="${status// /}"
  n="${n// /}"
  case "$status" in
    pending) pending="$n" ;;
  esac
done <<< "$counts"

if [[ "$pending" -ge 1 ]]; then
  echo "mode=file pending=${pending}"
  exit 0
fi

echo "mode-detect: no-op (pending=0) — nothing to file" >&2
exit 1
