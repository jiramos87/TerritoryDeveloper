#!/usr/bin/env bash
# ship-plan Phase A.1 — parse handoff doc YAML frontmatter into JSON.
#
# Reads docs/explorations/{slug}.md frontmatter (between leading `---` fences)
# and emits a JSON blob the recipe engine consumes. Defers actual YAML parsing
# to the existing Node helper script — bash is just the recipe-engine shim.
#
# Args:
#   --slug         <plan-slug>
#   --handoff-path <optional override>
#
# Output (stdout): JSON shape:
#   {
#     "plan_version": <int>,
#     "plan": {...},
#     "stages": [...],
#     "tasks": [...],
#     "task_keys": [...],
#     "anchors": [...],
#     "glossary_terms": [...],
#     "lint_tasks": [{task_key, body, anchors, glossary_terms}, ...]
#   }
# Exit 1: missing handoff doc, no frontmatter, or parse error.
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
  echo "parse-handoff-yaml: missing --slug" >&2
  exit 1
fi

if [[ -z "$handoff_path" ]]; then
  handoff_path="docs/explorations/${slug}.md"
fi

cd "$REPO_ROOT"

if [[ ! -f "$handoff_path" ]]; then
  echo "parse-handoff-yaml: handoff doc not found: $handoff_path" >&2
  exit 1
fi

# Delegate to Node helper (uses js-yaml already in tools workspace).
node tools/scripts/recipe-engine/ship-plan/parse-handoff-yaml.mjs \
  --handoff-path "$handoff_path" \
  --slug "$slug"
