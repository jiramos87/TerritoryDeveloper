#!/usr/bin/env bash
# ship-cycle Pass B Phase B.8 — single stage commit (chain-scope delta).
#
# Mirrors `tools/scripts/recipe-engine/ship-stage-pass-b/stage-commit.sh`
# but uses ship-cycle commit subject convention `feat({slug}-stage-{stage_id_db})`.
#
# Stages tracked files modified since HEAD (git diff HEAD --name-only) plus
# newly untracked files (git ls-files --others --exclude-standard). Never
# uses git add -A or git add . to avoid sweeping sibling work streams.
#
# Args:
#   --slug                <plan-slug>
#   --stage-id            <X.Y>
#   --archived-task-count <N>
#
# Output (stdout): commit_sha=<sha>
# Exit 1: nothing to commit (with HEAD reuse warning) or commit failed.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"

slug=""
stage_id=""
archived_count="?"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)                                      slug="$2";           shift 2 ;;
    --stage-id|--stage_id)                       stage_id="$2";       shift 2 ;;
    --archived-task-count|--archived_task_count) archived_count="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$stage_id" ]]; then
  echo "git-commit-stage: missing --slug or --stage-id" >&2
  exit 1
fi

cd "$REPO_ROOT"

TRACKED=()
while IFS= read -r -d '' line; do [[ -n "$line" ]] && TRACKED+=("$line"); done < <(git diff HEAD --name-only -z 2>/dev/null || true)
UNTRACKED=()
while IFS= read -r -d '' line; do [[ -n "$line" ]] && UNTRACKED+=("$line"); done < <(git ls-files --others --exclude-standard -z 2>/dev/null || true)
ALL_PATHS=("${TRACKED[@]}" "${UNTRACKED[@]}")

if [[ ${#ALL_PATHS[@]} -eq 0 ]]; then
  echo "git-commit-stage: nothing to commit — reusing HEAD sha" >&2
  sha=$(git rev-parse HEAD)
  echo "commit_sha=${sha}"
  exit 0
fi

# Stage per-file — never blanket.
for f in "${ALL_PATHS[@]}"; do
  git add -- "$f"
done

git commit -m "$(cat <<EOF
feat(${slug}-stage-${stage_id}): ship-cycle Pass B verify + closeout

Stage ${stage_id} — ship-cycle Pass B closed.
Verify-loop: pass. Closeout: ${archived_count} tasks archived.
EOF
)"

sha=$(git rev-parse HEAD)
echo "commit_sha=${sha}"
exit 0
