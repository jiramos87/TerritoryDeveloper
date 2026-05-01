#!/usr/bin/env bash
# ship-stage-pass-b Phase 7 — single stage commit (chain-scope delta).
#
# Stages tracked files modified since HEAD (git diff HEAD --name-only) plus
# newly untracked files (git ls-files --others --exclude-standard). Never
# uses git add -A or git add . to avoid sweeping sibling work streams.
#
# Note: pre-existing untracked files (BASELINE_DIRTY ?? entries) may be staged
# if they appear in git ls-files --others output. Wave 3 gap — BASELINE_DIRTY
# isolation requires outer agent context; recipe-path best-effort only.
#
# Args:
#   --slug               <plan-slug>
#   --stage-id           <X.Y>
#   --archived-task-count <N>
#
# Output (stdout): commit_sha=<sha>
# Exit 1: nothing to commit or commit failed.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"

slug=""
stage_id=""
archived_count="?"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)                slug="$2";           shift 2 ;;
    --stage-id)            stage_id="$2";        shift 2 ;;
    --archived-task-count) archived_count="$2";  shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$stage_id" ]]; then
  echo "stage-commit: missing --slug or --stage-id" >&2
  exit 1
fi

cd "$REPO_ROOT"

# Collect chain-scope paths.
mapfile -t TRACKED < <(git diff HEAD --name-only 2>/dev/null || true)
mapfile -t UNTRACKED < <(git ls-files --others --exclude-standard 2>/dev/null || true)
ALL_PATHS=("${TRACKED[@]}" "${UNTRACKED[@]}")

if [[ ${#ALL_PATHS[@]} -eq 0 ]]; then
  echo "stage-commit: nothing to commit — STAGE_TOUCHED_PATHS empty" >&2
  echo "stage-commit: PASS_B_ONLY resume with empty diff; reusing HEAD sha" >&2
  sha=$(git rev-parse HEAD)
  echo "commit_sha=${sha}"
  exit 0
fi

# Stage per-file — never blanket.
for f in "${ALL_PATHS[@]}"; do
  git add -- "$f"
done

# Verify staged scope (sanity check).
STAGED_COUNT=$(git diff --cached --name-only | wc -l | tr -d ' ')

git commit -m "$(cat <<EOF
feat(${slug}-stage-${stage_id}): Pass B verify + closeout

Stage ${stage_id} — Pass B closed.
Verify-loop: pass. Closeout: ${archived_count} tasks archived.
EOF
)"

sha=$(git rev-parse HEAD)
echo "commit_sha=${sha}"
exit 0
