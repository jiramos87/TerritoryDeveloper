#!/usr/bin/env bash
# stage-file Phase 4 — resolve target manifest section.
#
# Args: --slug <plan-slug> --plan-title <title>
# Output (stdout, exit 0): single line `target_section=<header verbatim>`.
# Ambiguous (exit 1): stderr lists candidates; subagent prompts user; recipe re-invoked
#                     with explicit --target-section override (engine arg).
#
# Heuristic (matches ia/skills/stage-file/SKILL.md §Phase 4.1):
#   1. Normalize each manifest header (strip `## `, lowercase, kebab punct/space).
#   2. Candidate slugs: SLUG ; kebab(PLAN_TITLE) ; kebab(PLAN_TITLE − {-program,-lane,-roadmap}).
#   3. First UNIQUE substring/prefix match → emit. 0 or ≥2 hits → halt.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"

slug=""
plan_title=""
override=""
manifest="${REPO_ROOT}/ia/state/backlog-sections.json"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)             slug="$2"; shift 2 ;;
    --plan-title)       plan_title="$2"; shift 2 ;;
    --override-section) override="$2"; shift 2 ;;
    --manifest)         manifest="$2"; shift 2 ;;
    *) shift ;;
  esac
done

# Short-circuit: caller already disambiguated; trust override verbatim.
if [[ -n "$override" ]]; then
  echo "target_section=${override}"
  exit 0
fi

if [[ -z "$slug" || ! -f "$manifest" ]]; then
  echo "manifest-resolve: missing --slug or manifest not found ($manifest)" >&2
  exit 1
fi

kebab() {
  echo "$1" | tr '[:upper:]' '[:lower:]' | sed -E 's/[^a-z0-9]+/-/g; s/^-+|-+$//g'
}

slug_l=$(echo "$slug" | tr '[:upper:]' '[:lower:]')
title_kebab=""
title_kebab_stripped=""
if [[ -n "$plan_title" ]]; then
  title_kebab=$(kebab "$plan_title")
  title_kebab_stripped="${title_kebab%-program}"
  title_kebab_stripped="${title_kebab_stripped%-lane}"
  title_kebab_stripped="${title_kebab_stripped%-roadmap}"
fi

candidates=("$slug_l")
[[ -n "$title_kebab" && "$title_kebab" != "$slug_l" ]] && candidates+=("$title_kebab")
[[ -n "$title_kebab_stripped" && "$title_kebab_stripped" != "$title_kebab" ]] && candidates+=("$title_kebab_stripped")

# Build header list: header → normalized.
mapfile -t headers < <(jq -r '.sections[].header' "$manifest")

best_header=""
best_score=0
for hdr in "${headers[@]}"; do
  norm=$(kebab "${hdr#\#\# }")
  for cand in "${candidates[@]}"; do
    [[ -z "$cand" ]] && continue
    if [[ "$norm" == "$cand" ]]; then
      score=3
    elif [[ "$norm" == "$cand"* || "$norm" == *"$cand" ]]; then
      score=2
    elif [[ "$norm" == *"$cand"* ]]; then
      score=1
    else
      score=0
    fi
    if [[ "$score" -gt "$best_score" ]]; then
      best_score=$score
      best_header=$hdr
      best_count=1
    elif [[ "$score" -eq "$best_score" && "$score" -gt 0 && "$hdr" != "$best_header" ]]; then
      best_count=$((${best_count:-1} + 1))
    fi
  done
done

if [[ -z "$best_header" || "$best_score" -eq 0 ]]; then
  {
    echo "manifest-resolve: no match for slug=${slug} title=${plan_title}"
    echo "candidates:"
    for hdr in "${headers[@]}"; do echo "  - $hdr"; done
  } >&2
  exit 1
fi

if [[ "${best_count:-1}" -gt 1 ]]; then
  {
    echo "manifest-resolve: ambiguous (${best_count} headers tied at score=${best_score})"
    echo "candidates:"
    for hdr in "${headers[@]}"; do echo "  - $hdr"; done
  } >&2
  exit 1
fi

echo "target_section=${best_header}"
exit 0
