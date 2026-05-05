#!/usr/bin/env bash
# ship-final Phase 3 — assert every ia_stages row for slug has status='done'.
# Stops on any 'partial' / 'pending' / 'in_progress'.
#
# Args:
#   --slug <plan-slug>
#   --stages <json...>  (recipe flattenArgs, ignored — psql is source of truth)
#
# Output (stdout, exit 0): stages_done=N stages_total=N
# Exit 1 (stderr): stages_not_done=<list>
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"
# shellcheck disable=SC1091
source "${REPO_ROOT}/tools/scripts/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

slug=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug) slug="$2"; shift 2 ;;
    --stages|--stages-json) shift 2 ;;  # consumed, source of truth = psql
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "${DATABASE_URL:-}" ]]; then
  echo "assert-stages-done: missing --slug / DATABASE_URL" >&2
  exit 1
fi

slug_safe="${slug//\'/\'\'}"

total=$(psql "$DATABASE_URL" -tAc "SELECT COUNT(*) FROM ia_stages WHERE slug = '${slug_safe}'" 2>/dev/null | tr -d '[:space:]' || true)
done_count=$(psql "$DATABASE_URL" -tAc "SELECT COUNT(*) FROM ia_stages WHERE slug = '${slug_safe}' AND status = 'done'" 2>/dev/null | tr -d '[:space:]' || true)

if [[ -z "$total" || -z "$done_count" ]]; then
  echo "assert-stages-done: psql query failed" >&2
  exit 1
fi

if [[ "$total" -eq 0 ]]; then
  echo "assert-stages-done: no stages for slug=${slug} — version_close requires ≥1 stage" >&2
  exit 1
fi

if [[ "$done_count" -ne "$total" ]]; then
  not_done=$(psql "$DATABASE_URL" -tAc "SELECT string_agg(stage_id || ':' || status, ', ' ORDER BY stage_id) FROM ia_stages WHERE slug = '${slug_safe}' AND status <> 'done'" 2>/dev/null || true)
  echo "stages_not_done=${not_done} — STOP. Ship remaining stages first." >&2
  exit 1
fi

echo "stages_done=${done_count} stages_total=${total}"
exit 0
