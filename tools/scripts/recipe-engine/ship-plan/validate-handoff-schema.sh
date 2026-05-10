#!/usr/bin/env bash
# ship-plan Phase A.2 — validate handoff doc YAML frontmatter against schema.
#
# Thin recipe-engine shim around `tools/scripts/validate-handoff-schema.mjs`
# (TECH-12634). Exits non-zero on any schema violation so the recipe stops
# before the bundle dispatch.
#
# Args:
#   --slug         <plan-slug>
#   --handoff-path <optional override; defaults to docs/explorations/{slug}.md>
#
# Output (stdout): single JSON line `{"validated": true, "handoff_path": "..."}`.
# Exit 1 (stderr): per-violation JSON line forwarded from validator.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"

slug=""
handoff_path=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)         slug="$2";         shift 2 ;;
    --handoff-path) handoff_path="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" ]]; then
  echo "validate-handoff-schema: missing --slug" >&2
  exit 1
fi

if [[ -z "$handoff_path" ]]; then
  handoff_path="docs/explorations/${slug}.md"
fi

cd "$REPO_ROOT"

if [[ ! -f "$handoff_path" ]]; then
  echo "validate-handoff-schema: handoff doc not found: $handoff_path" >&2
  exit 1
fi

# Delegate to TECH-12634 validator. Forwards stderr verbatim. Non-zero exit
# from validator → non-zero exit here → recipe stops.
node tools/scripts/validate-handoff-schema.mjs "$handoff_path"

# All-pass — emit success line.
printf '{"validated":true,"handoff_path":"%s"}\n' "$handoff_path"
