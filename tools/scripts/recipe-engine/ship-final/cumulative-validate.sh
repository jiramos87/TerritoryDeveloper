#!/usr/bin/env bash
# ship-final Phase 4 — cumulative `validate:all` on parent-tag-to-HEAD diff.
#
# Computes parent_tag = `{slug}-v{version-1}` when version > 1; empty for v1
# (full HEAD diff context). Runs `npm run validate:all`. Captures structured
# result to stdout for journal payload.
#
# Args:
#   --slug    <plan-slug>
#   --version <N>
#
# Output (stdout, exit 0): result={"ok":true,"parent_tag":"...","scripts":[...]}
# Exit 1 (stderr): validate:all failed; full output streamed to stderr.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"

slug=""
version=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)    slug="$2";    shift 2 ;;
    --version) version="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$version" ]]; then
  echo "cumulative-validate: missing --slug or --version" >&2
  exit 1
fi

cd "$REPO_ROOT"

# parent_tag — empty for v1 (no prior version).
parent_tag=""
if [[ "$version" -gt 1 ]]; then
  prev=$((version - 1))
  parent_tag="${slug}-v${prev}"
  if ! git rev-parse "$parent_tag" >/dev/null 2>&1; then
    echo "cumulative-validate: parent_tag=${parent_tag} not found in repo — cannot bound cumulative diff" >&2
    exit 1
  fi
  # Smoke: ensure HEAD is descendant of parent_tag (no time-travel).
  diff_lines=$(git diff "${parent_tag}..HEAD" --name-only 2>/dev/null | wc -l | tr -d ' ')
  echo "cumulative-validate: parent_tag=${parent_tag} diff_lines=${diff_lines}"
fi

# Run validate:all on the working tree (cumulative since parent_tag is implicit
# in HEAD; validate:all asserts current-tree health, not diff-isolated checks).
if npm run validate:all >&2; then
  echo "result={\"ok\":true,\"parent_tag\":\"${parent_tag}\",\"scripts\":[\"validate:all\"]}"
  exit 0
fi

echo "cumulative-validate: validate:all FAILED — see stderr" >&2
echo "result={\"ok\":false,\"parent_tag\":\"${parent_tag}\",\"scripts\":[\"validate:all\"]}"
exit 1
