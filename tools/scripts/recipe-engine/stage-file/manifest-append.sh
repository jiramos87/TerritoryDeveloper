#!/usr/bin/env bash
# stage-file Phase 5.D — append issue entry to manifest.
#
# Args:
#   --section-header  "## Some Section"
#   --issue-id        "TECH-123"
#   --checklist-line  "- [ ] **TECH-123 — title** — notes."
#   [--manifest path/to/backlog-sections.json]
#
# Idempotent: if {type:issue, id:ISSUE_ID} already present in target section → no-op.
# Exit 0 always on idempotent success/no-op; non-zero on missing section / bad args.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"

section=""
issue_id=""
checklist_line=""
manifest="${REPO_ROOT}/ia/state/backlog-sections.json"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --section-header)  section="$2"; shift 2 ;;
    --issue-id)        issue_id="$2"; shift 2 ;;
    --checklist-line)  checklist_line="$2"; shift 2 ;;
    --manifest)        manifest="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$section" || -z "$issue_id" || -z "$checklist_line" || ! -f "$manifest" ]]; then
  echo "manifest-append: missing --section-header / --issue-id / --checklist-line or manifest not found" >&2
  exit 1
fi

# Use jq to verify section exists + check idempotent presence.
section_exists=$(jq --arg h "$section" '[.sections[] | select(.header == $h)] | length' "$manifest")
if [[ "$section_exists" -eq 0 ]]; then
  echo "manifest-append: section not found in manifest: $section" >&2
  exit 1
fi

already=$(jq --arg h "$section" --arg id "$issue_id" '
  [.sections[] | select(.header == $h) | .items[] | select(.type == "issue" and .id == $id)] | length
' "$manifest")
if [[ "$already" -gt 0 ]]; then
  echo "manifest-append: ${issue_id} already in ${section} (idempotent no-op)"
  exit 0
fi

tmp=$(mktemp)
jq --arg h "$section" --arg id "$issue_id" --arg line "$checklist_line" '
  .sections |= map(
    if .header == $h then
      .items += [{type: "issue", id: $id, checklist_line: $line, trailing_blanks: 1}]
    else . end
  )
' "$manifest" > "$tmp"
mv "$tmp" "$manifest"

echo "manifest-append: appended ${issue_id} → ${section}"
exit 0
