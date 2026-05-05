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

if git rev-parse "$tag" >/dev/null 2>&1; then
  echo "git-tag: tag=${tag} already exists — STOP. Run \`git tag -d ${tag}\` to retry (destructive)." >&2
  exit 1
fi

sha=$(git rev-parse HEAD)
git tag -a "$tag" -m "Close ${slug} v${version}"

echo "tag=${tag} sha=${sha}"
exit 0
