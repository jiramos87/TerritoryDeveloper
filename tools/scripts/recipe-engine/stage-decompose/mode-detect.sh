#!/usr/bin/env bash
# stage-decompose Phase 0 — mode detection.
#
# Args:
#   --slug <plan-slug>
#   --stage-id <X.Y>
#   [--allow-overwrite]   Allow rewriting an already-decomposed Stage body.
#
# Output (stdout, exit 0):
#   mode=skeleton   — Stage body has no Task table → ready to decompose.
#   mode=overwrite  — Stage body already has Task table; --allow-overwrite asserted.
#
# Halt (exit 1):
#   stage not found / Stage body already decomposed (no override) /
#   Stage status is In Review / In Progress / Final (per SKILL.md guardrail).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"
# shellcheck disable=SC1091
source "${REPO_ROOT}/tools/scripts/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

slug=""
stage_id=""
allow_overwrite=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)             slug="$2"; shift 2 ;;
    --stage-id)         stage_id="$2"; shift 2 ;;
    --allow-overwrite)  allow_overwrite=1; shift ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$stage_id" || -z "${DATABASE_URL:-}" ]]; then
  echo "mode-detect: missing --slug / --stage-id / DATABASE_URL" >&2
  exit 1
fi

stage_norm="${stage_id#Stage }"
stage_norm="${stage_norm// /}"

row_query="SELECT COALESCE(status, 'Draft') || E'\t' || COALESCE(body, '') FROM ia_stages WHERE slug = '${slug//\'/\'\'}' AND stage_id = '${stage_norm//\'/\'\'}' LIMIT 1;"
row=$(psql "$DATABASE_URL" -tAF $'\t' -c "$row_query" 2>/dev/null || true)

if [[ -z "$row" ]]; then
  echo "mode-detect: stage ${stage_norm} not found for slug ${slug}" >&2
  exit 1
fi

status="${row%%	*}"
body="${row#*	}"

case "$status" in
  "In Review"|"In Progress"|"Final")
    echo "mode-detect: stage ${stage_norm} status=${status} — refuse to decompose advanced Stage" >&2
    exit 1
    ;;
esac

if echo "$body" | grep -qE '^\| T[0-9]+\.[0-9]+\.[0-9]+ \|'; then
  if [[ "$allow_overwrite" -eq 1 ]]; then
    echo "mode=overwrite status=${status}"
    exit 0
  fi
  echo "mode-detect: stage ${stage_norm} already has Task table — pass --allow-overwrite to rewrite" >&2
  exit 1
fi

echo "mode=skeleton status=${status}"
exit 0
