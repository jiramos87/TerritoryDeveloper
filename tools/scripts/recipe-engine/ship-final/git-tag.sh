#!/usr/bin/env bash
# ship-final Phase 5 — annotated git tag `{slug}-v{N}`. Local only.
#
# Args:
#   --slug    <plan-slug>
#   --version <N>
#
# Output (stdout, exit 0): tag=<tagname> sha=<HEAD-sha>
# Exit 1: tag already exists (manual `git tag -d {tag}` for retry — destructive).
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
  echo "git-tag: missing --slug or --version" >&2
  exit 1
fi

cd "$REPO_ROOT"

tag="${slug}-v${version}"

head_sha=$(git rev-parse HEAD)

if git rev-parse "$tag" >/dev/null 2>&1; then
  # Idempotent path: tag already exists. Compare against HEAD — recipe can
  # safely resume only when the existing tag points at HEAD (otherwise the
  # plan's closure SHA would diverge from current branch tip and journal_append
  # would log a misleading sha).
  tag_sha=$(git rev-list -n 1 "$tag")
  if [[ "$tag_sha" == "$head_sha" ]]; then
    echo "git-tag: tag=${tag} already exists at HEAD (idempotent re-entry)." >&2
    echo "tag=${tag} sha=${tag_sha}"
    exit 0
  fi
  echo "git-tag: tag=${tag} exists at ${tag_sha} but HEAD is ${head_sha} — STOP. Run \`git tag -d ${tag}\` to retry (destructive)." >&2
  exit 1
fi

git tag -a "$tag" -m "Close ${slug} v${version}"

echo "tag=${tag} sha=${head_sha}"
exit 0
