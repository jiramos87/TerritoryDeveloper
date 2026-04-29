#!/usr/bin/env bash
# section-claim — Phase 1: open git worktree for parallel section work.
# Args: --slug <plan-slug> --section-id <id> [--worktree-root <path>] [--base-branch <ref>]
#
# Behavior:
#   - worktree_path defaults to ../territory-developer.section-{section_id}
#     (relative to repo root parent dir).
#   - branch name = feature/{slug}-section-{section_id}.
#   - Idempotent: if worktree path already exists AND HEAD is on the expected
#     branch → emit "noop" + exit 0. If path exists on a different branch →
#     exit 1 (caller decides — usually means stale claim or naming clash).
#   - base-branch defaults to current HEAD (so caller pins via checkout before
#     invoking the recipe). Override with --base-branch to fork from elsewhere.
#
# Emits one machine-readable line on stdout for recipe binding:
#   "created {worktree_path} branch={branch}"
#   "noop {worktree_path} branch={branch}"
set -euo pipefail

slug=""
section_id=""
worktree_root=""
base_branch=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)          slug="$2";          shift 2 ;;
    --section-id)    section_id="$2";    shift 2 ;;
    --section_id)    section_id="$2";    shift 2 ;;
    --worktree-root) worktree_root="$2"; shift 2 ;;
    --worktree_root) worktree_root="$2"; shift 2 ;;
    --base-branch)   base_branch="$2";   shift 2 ;;
    --base_branch)   base_branch="$2";   shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$section_id" ]]; then
  echo "git-worktree-add: missing --slug or --section-id" >&2
  exit 1
fi

repo_root="$(git rev-parse --show-toplevel)"
parent_dir="$(dirname "$repo_root")"
repo_name="$(basename "$repo_root")"

if [[ -z "$worktree_root" ]]; then
  worktree_root="${parent_dir}/${repo_name}.section-${section_id}"
fi

branch="feature/${slug}-section-${section_id}"

# Idempotent path check.
if [[ -d "$worktree_root" ]]; then
  current_branch="$(git -C "$worktree_root" rev-parse --abbrev-ref HEAD 2>/dev/null || echo "")"
  if [[ "$current_branch" == "$branch" ]]; then
    echo "noop ${worktree_root} branch=${branch}"
    exit 0
  fi
  echo "git-worktree-add: ${worktree_root} exists on branch '${current_branch}', expected '${branch}' — refusing to clobber" >&2
  exit 1
fi

# Resolve base ref (default = current HEAD).
if [[ -z "$base_branch" ]]; then
  base_branch="$(git rev-parse --abbrev-ref HEAD)"
fi

# Branch may already exist (re-claim after release). Use -B to reset, --force
# only when explicitly required is dangerous; prefer plain checkout-or-create.
if git show-ref --verify --quiet "refs/heads/${branch}"; then
  git worktree add "$worktree_root" "$branch" >/dev/null
else
  git worktree add -b "$branch" "$worktree_root" "$base_branch" >/dev/null
fi

echo "created ${worktree_root} branch=${branch}"
