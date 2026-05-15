#!/usr/bin/env bash
# ship-cycle Pass B Phase B.7 — refresh Unity AssetDatabase only when
# Assets/** touched in cumulative stage diff.
#
# Avoids spinning up bridge for non-Unity stages (typescript / IA / docs).
# When Assets/**/*.cs or Assets/**/*.{prefab,asset,unity,mat,png,…} touched
# vs HEAD, enqueue agent_bridge_job(kind=refresh_asset_database) directly
# via psql (mig 0008 schema). Cron drains; bridge picks it up.
#
# Args:
#   --slug     <plan-slug>
#   --stage-id <X.Y>
#
# Output (stdout):
#   touched_assets=true bridge_job_id=<uuid>
#   touched_assets=false (no-op)
# Exit 1: psql failure.
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
    --slug)                slug="$2";     shift 2 ;;
    --stage-id|--stage_id) stage_id="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$stage_id" ]]; then
  echo "maybe-refresh-asset-db: missing --slug or --stage-id" >&2
  exit 1
fi

cd "$REPO_ROOT"

# Detect Assets/** in cumulative stage diff (HEAD vs working tree, including untracked).
TRACKED=$(git diff HEAD --name-only 2>/dev/null | grep -E '^Assets/' || true)
UNTRACKED=$(git ls-files --others --exclude-standard 2>/dev/null | grep -E '^Assets/' || true)

if [[ -z "$TRACKED" && -z "$UNTRACKED" ]]; then
  echo "touched_assets=false"
  exit 0
fi

if [[ -z "${DATABASE_URL:-}" ]]; then
  echo "maybe-refresh-asset-db: DATABASE_URL unset — cannot enqueue bridge job" >&2
  exit 1
fi

# Enqueue refresh_asset_database via direct INSERT (mirror cron-server pattern).
# command_id assigned via gen_random_uuid() — pgcrypto extension required.
job_id=$(psql "$DATABASE_URL" -tAc "
  INSERT INTO agent_bridge_job (command_id, kind, status, request)
  VALUES (gen_random_uuid(), 'refresh_asset_database', 'pending', '{\"params\":{}}'::jsonb)
  RETURNING command_id
" 2>/dev/null | grep -E '^[0-9a-f-]{36}$' | head -1 | tr -d '[:space:]' || true)

if [[ -z "$job_id" ]]; then
  echo "maybe-refresh-asset-db: psql INSERT failed" >&2
  exit 1
fi

echo "touched_assets=true bridge_job_id=${job_id}"
exit 0
