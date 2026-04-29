#!/usr/bin/env bash
# release-rollout-track — Phase 1b: column (f) filed-signal verify.
# Args: --slug <child-master-plan-slug> [--root <repo-root>]
# Filed signal logic (per SKILL.md):
#   - ia/backlog/{ID}.yaml present AND ia/projects/{ID}*.md present for at least
#     one issue id whose spec/yaml mentions the slug → ✓
#   - At least one ia/backlog/{ID}.yaml present but matching spec absent → ◐
#   - Zero yaml records mentioning slug → —
# This is a coarse heuristic — recipe caller MAY override based on real evidence.
# Stdout: single glyph (✓|◐|—) suitable for binding into cell-flip --marker.
set -euo pipefail

slug=""
root=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug) slug="$2"; shift 2 ;;
    --root) root="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" ]]; then
  echo "filed-signal: missing --slug" >&2
  exit 1
fi
if [[ -z "$root" ]]; then
  root="$(pwd)"
fi

backlog_dir="${root}/ia/backlog"
projects_dir="${root}/ia/projects"

if [[ ! -d "$backlog_dir" || ! -d "$projects_dir" ]]; then
  echo "—"
  exit 0
fi

# Collect yaml ids whose body references slug (related-master-plan or comment text).
yaml_hits=()
while IFS= read -r f; do
  yaml_hits+=("$f")
done < <(grep -l -F "$slug" "$backlog_dir"/*.yaml 2>/dev/null || true)

if [[ ${#yaml_hits[@]} -eq 0 ]]; then
  echo "—"
  exit 0
fi

paired=0
unpaired=0
for y in "${yaml_hits[@]}"; do
  base="$(basename "$y" .yaml)"
  if compgen -G "${projects_dir}/${base}*.md" > /dev/null; then
    paired=$((paired + 1))
  else
    unpaired=$((unpaired + 1))
  fi
done

if [[ $paired -gt 0 && $unpaired -eq 0 ]]; then
  echo "✓"
elif [[ $paired -gt 0 ]]; then
  echo "◐"
else
  echo "◐"
fi
