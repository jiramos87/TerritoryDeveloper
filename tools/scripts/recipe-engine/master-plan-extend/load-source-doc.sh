#!/usr/bin/env bash
# master-plan-extend Phase 2 — load source exploration/extensions doc.
#
# Args:
#   --path <repo-relative-path>
#
# Output (stdout, exit 0): "source_doc_lines=N" — line count of read doc.
# Exit 1: file not found.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"

doc_path=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --path) doc_path="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$doc_path" ]]; then
  echo "load-source-doc: missing --path" >&2
  exit 1
fi

full_path="${REPO_ROOT}/${doc_path}"
if [[ ! -f "$full_path" ]]; then
  echo "load-source-doc: file not found: ${doc_path}" >&2
  exit 1
fi

line_count=$(wc -l < "$full_path")
echo "source_doc_lines=${line_count}"
exit 0
