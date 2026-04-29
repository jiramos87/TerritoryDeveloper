#!/usr/bin/env bash
# release-rollout-track — Phase 0: validate row + column + marker.
# Args: --tracker <path> --row <slug> --col <a|b|c|d|e|f|g> --marker <glyph>
# Exit 0 + emits validated payload on stdout. Exit 1 on any failure.
set -euo pipefail

tracker=""
row=""
col=""
marker=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --tracker) tracker="$2"; shift 2 ;;
    --row)     row="$2";     shift 2 ;;
    --col)     col="$2";     shift 2 ;;
    --marker)  marker="$2";  shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$tracker" || -z "$row" || -z "$col" || -z "$marker" ]]; then
  echo "validate-row: missing required arg (--tracker/--row/--col/--marker)" >&2
  exit 1
fi

if [[ ! -f "$tracker" ]]; then
  echo "validate-row: tracker not found: $tracker" >&2
  exit 1
fi

# Strip parens if user passed (a) instead of a
col_norm="${col#(}"
col_norm="${col_norm%)}"
case "$col_norm" in
  a|b|c|d|e|f|g) ;;
  *) echo "validate-row: invalid TARGET_COL '$col' (want a|b|c|d|e|f|g)" >&2; exit 1 ;;
esac

case "$marker" in
  "✓"|"◐"|"—"|"❓"|"⚠️") ;;
  *) echo "validate-row: invalid NEW_MARKER '$marker' (want ✓|◐|—|❓|⚠️)" >&2; exit 1 ;;
esac

# Anchor on `| {row} |` literal (pipe-bounded cell match).
if ! grep -F -q "| ${row} |" "$tracker"; then
  echo "validate-row: row '${row}' not found in $tracker" >&2
  exit 1
fi

echo "OK ${tracker} ${row} (${col_norm}) ${marker}"
