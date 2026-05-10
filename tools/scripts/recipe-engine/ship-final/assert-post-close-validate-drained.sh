#!/usr/bin/env bash
# ship-final Phase 4.5 — assert cron_validate_post_close_jobs has zero
# queued/running rows for slug.
#
# Stops if drainer is behind so close blocks until verdict lands. Operator
# can re-run /ship-final after drainer catches up.
#
# Args:
#   --slug <plan-slug>
#
# Output (stdout, exit 0): drained=true queued=0 running=0
# Exit 1 (stderr): drained=false queued=N running=M — STOP.
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
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "${DATABASE_URL:-}" ]]; then
  echo "assert-post-close-validate-drained: missing --slug / DATABASE_URL" >&2
  exit 1
fi

slug_safe="${slug//\'/\'\'}"

# Count queued + running rows for slug.
counts=$(psql "$DATABASE_URL" -tAc "
  SELECT
    COALESCE(SUM(CASE WHEN status = 'queued' THEN 1 ELSE 0 END), 0) || ',' ||
    COALESCE(SUM(CASE WHEN status = 'running' THEN 1 ELSE 0 END), 0)
  FROM cron_validate_post_close_jobs
  WHERE slug = '${slug_safe}'
" 2>/dev/null | tr -d '[:space:]' || true)

if [[ -z "$counts" ]]; then
  echo "assert-post-close-validate-drained: psql query failed" >&2
  exit 1
fi

queued="${counts%%,*}"
running="${counts##*,}"

if [[ "$queued" -gt 0 || "$running" -gt 0 ]]; then
  echo "assert-post-close-validate-drained: drained=false queued=${queued} running=${running} — STOP. Cron drainer behind; re-run /ship-final after drained." >&2
  exit 1
fi

echo "drained=true queued=0 running=0"
exit 0
