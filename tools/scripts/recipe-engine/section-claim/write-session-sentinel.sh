#!/usr/bin/env bash
# section-claim — Phase 3: write session sentinel inside worktree.
# Args: --slug <plan-slug> --section-id <id> --session-id <id> [--worktree-root <path>]
#
# Computes worktree path from slug + section_id (same convention as
# git-worktree-add.sh) unless --worktree-root override given. Writes a small
# JSON file at {worktree_path}/.parallel-section-claim.json so downstream
# skills (/ship-stage, /section-closeout) running inside that worktree can
# read the same session_id without re-deriving it.
#
# Idempotent: identical content → "noop"; mismatched content → overwrite +
# emit "rewritten" (resume after partial run).
#
# Emits one machine-readable line on stdout:
#   "written {path}"
#   "noop {path}"
#   "rewritten {path}"
set -euo pipefail

slug=""
section_id=""
session_id=""
worktree_root=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)          slug="$2";          shift 2 ;;
    --section-id)    section_id="$2";    shift 2 ;;
    --section_id)    section_id="$2";    shift 2 ;;
    --session-id)    session_id="$2";    shift 2 ;;
    --session_id)    session_id="$2";    shift 2 ;;
    --worktree-root) worktree_root="$2"; shift 2 ;;
    --worktree_root) worktree_root="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$section_id" || -z "$session_id" ]]; then
  echo "write-session-sentinel: missing --slug / --section-id / --session-id" >&2
  exit 1
fi

if [[ -z "$worktree_root" ]]; then
  repo_root="$(git rev-parse --show-toplevel)"
  parent_dir="$(dirname "$repo_root")"
  repo_name="$(basename "$repo_root")"
  worktree_root="${parent_dir}/${repo_name}.section-${section_id}"
fi

if [[ ! -d "$worktree_root" ]]; then
  echo "write-session-sentinel: worktree not found at ${worktree_root}" >&2
  exit 1
fi

sentinel="${worktree_root}/.parallel-section-claim.json"
new_content=$(printf '{"slug":"%s","section_id":"%s","session_id":"%s"}\n' \
  "$slug" "$section_id" "$session_id")

if [[ -f "$sentinel" ]]; then
  if [[ "$(cat "$sentinel")" == "$new_content" ]]; then
    echo "noop ${sentinel}"
    exit 0
  fi
  printf '%s' "$new_content" > "$sentinel"
  echo "rewritten ${sentinel}"
  exit 0
fi

printf '%s' "$new_content" > "$sentinel"
echo "written ${sentinel}"
