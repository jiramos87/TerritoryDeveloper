#!/usr/bin/env bash
# ship-final Phase 2 — assert zero open ia_section_claims rows for slug.
#
# Args:
#   --slug <plan-slug>
#
# Output (stdout, exit 0):
#   sections_open=0 sections_closed=<json-array of section_ids ever closed>
# Exit 1 (stderr): sections_open=N — STOP, run /section-closeout first.
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
  echo "assert-sections-closed: missing --slug / DATABASE_URL" >&2
  exit 1
fi

slug_safe="${slug//\'/\'\'}"

# Open section claims (released_at IS NULL) — must be 0.
open_count=$(psql "$DATABASE_URL" -tAc "SELECT COUNT(*) FROM ia_section_claims WHERE slug = '${slug_safe}' AND released_at IS NULL" 2>/dev/null | tr -d '[:space:]' || true)

if [[ -z "$open_count" ]]; then
  echo "assert-sections-closed: psql query failed" >&2
  exit 1
fi

if [[ "$open_count" -gt 0 ]]; then
  echo "sections_open=${open_count} — STOP. Run /section-closeout {SLUG} {SECTION_ID} for each open section, retry." >&2
  exit 1
fi

# Enumerate closed sections (released_at NOT NULL) for journal payload.
sections_closed_json=$(psql "$DATABASE_URL" -tAc "SELECT COALESCE(json_agg(DISTINCT section_id ORDER BY section_id), '[]'::json) FROM ia_section_claims WHERE slug = '${slug_safe}' AND released_at IS NOT NULL" 2>/dev/null | tr -d '\n' || true)

if [[ -z "$sections_closed_json" ]]; then
  sections_closed_json="[]"
fi

echo "sections_open=0 sections_closed=${sections_closed_json}"
exit 0
