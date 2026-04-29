#!/usr/bin/env bash
# section-closeout — Phase 3: merge section branch into base_branch + remove worktree.
# Args: --slug <slug> --section-id <id> --base-branch <ref> [--worktree-root <path>]
#
# Preconditions (fail-fast):
#   1. Main worktree current branch == base_branch.
#   2. Section branch exists.
#   3. Worktree path exists.
#
# Actions:
#   1. git merge --no-ff feature/{slug}-section-{section_id} into base_branch.
#   2. git worktree remove {worktree_root} --force.
#
# Emits one machine-readable line on stdout:
#   "merged {branch} → {base_branch} worktree-removed {path}"
set -euo pipefail

slug=""
section_id=""
base_branch=""
worktree_root=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)          slug="$2";          shift 2 ;;
    --section-id)    section_id="$2";    shift 2 ;;
    --section_id)    section_id="$2";    shift 2 ;;
    --base-branch)   base_branch="$2";   shift 2 ;;
    --base_branch)   base_branch="$2";   shift 2 ;;
    --worktree-root) worktree_root="$2"; shift 2 ;;
    --worktree_root) worktree_root="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$section_id" || -z "$base_branch" ]]; then
  echo "git-merge-section: missing --slug / --section-id / --base-branch" >&2
  exit 1
fi

section_branch="feature/${slug}-section-${section_id}"

repo_root="$(git rev-parse --show-toplevel)"
if [[ -z "$worktree_root" ]]; then
  parent_dir="$(dirname "$repo_root")"
  repo_name="$(basename "$repo_root")"
  worktree_root="${parent_dir}/${repo_name}.section-${section_id}"
fi

# Precondition 1: current branch must be base_branch
current_branch="$(git -C "$repo_root" rev-parse --abbrev-ref HEAD)"
if [[ "$current_branch" != "$base_branch" ]]; then
  echo "git-merge-section: current branch '${current_branch}' ≠ base_branch '${base_branch}'; checkout ${base_branch} first" >&2
  exit 1
fi

# Precondition 2: section branch must exist
if ! git -C "$repo_root" rev-parse --verify "refs/heads/${section_branch}" >/dev/null 2>&1; then
  echo "git-merge-section: branch '${section_branch}' not found" >&2
  exit 1
fi

# Precondition 3: worktree path must exist
if [[ ! -d "$worktree_root" ]]; then
  echo "git-merge-section: worktree not found at ${worktree_root}" >&2
  exit 1
fi

# Merge
git -C "$repo_root" merge --no-ff "$section_branch" --message "merge(${slug}-section-${section_id}): close section into ${base_branch}"

# Remove worktree
git -C "$repo_root" worktree remove "$worktree_root" --force

echo "merged ${section_branch} → ${base_branch} worktree-removed ${worktree_root}"
