#!/usr/bin/env bash
# release-rollout-track — Phase 2: cell flip (idempotent).
# Args: --tracker <path> --row <slug> --col <a|b|c|d|e|f|g> --marker <glyph> [--ticket <text>]
# Locates header row containing `(a) ... (g)` markers, computes target column index
# by matching `({col}) ` prefix in header cells, then in-place replaces the Nth
# pipe-separated cell of the row matching `| {row} |`.
# Idempotent: prints "noop" + exits 0 when target cell already equals desired text.
set -euo pipefail

tracker=""
row=""
col=""
marker=""
ticket=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --tracker) tracker="$2"; shift 2 ;;
    --row)     row="$2";     shift 2 ;;
    --col)     col="$2";     shift 2 ;;
    --marker)  marker="$2";  shift 2 ;;
    --ticket)  ticket="$2";  shift 2 ;;
    *) shift ;;
  esac
done

col_norm="${col#(}"
col_norm="${col_norm%)}"

# Compose target text. Empty ticket → marker only.
if [[ -n "$ticket" ]]; then
  new_cell="${marker} (${ticket})"
else
  new_cell="${marker}"
fi

# awk pass: locate header column index, then rewrite matching row.
tmp="$(mktemp)"
ROW="$row" COL="$col_norm" NEW_CELL="$new_cell" TRACKER="$tracker" \
awk -v row="$row" -v col="$col_norm" -v new_cell="$new_cell" '
function trim(s) { sub(/^[ \t]+/, "", s); sub(/[ \t]+$/, "", s); return s }
BEGIN { col_idx = -1; flipped = 0; noop = 0; done = 0 }
{
  line = $0
  if (done) { print line; next }
  # Detect header row by presence of `(a) ` and `(g) ` markers.
  if (col_idx < 0 && line ~ /\| *\(a\) / && line ~ /\| *\(g\) /) {
    n = split(line, cells, "|")
    target_prefix = "(" col ")"
    for (i = 1; i <= n; i++) {
      c = trim(cells[i])
      if (index(c, target_prefix) == 1) { col_idx = i; break }
    }
    print line
    next
  }
  # Match data row: pipe-bounded slug cell. Scope = FIRST match after header only.
  if (col_idx > 0 && index(line, "| " row " |") > 0) {
    n = split(line, cells, "|")
    if (col_idx <= n) {
      current = trim(cells[col_idx])
      desired = trim(new_cell)
      if (current == desired) {
        noop = 1
        done = 1
        print line
        next
      }
      # Preserve leading/trailing single space inside cell when present.
      cells[col_idx] = " " new_cell " "
      out = cells[1]
      for (i = 2; i <= n; i++) out = out "|" cells[i]
      print out
      flipped = 1
      done = 1
      next
    }
  }
  print line
}
END {
  if (col_idx < 0) { exit 2 }
  if (flipped) { print "FLIPPED" > "/dev/stderr" }
  else if (noop) { print "NOOP" > "/dev/stderr" }
  else { print "ROW_NOT_MATCHED" > "/dev/stderr"; exit 3 }
}
' "$tracker" > "$tmp" 2> "${tmp}.err" || {
  rc=$?
  if [[ $rc -eq 2 ]]; then
    echo "cell-flip: header row with (a)..(g) markers not found in $tracker" >&2
  elif [[ $rc -eq 3 ]]; then
    echo "cell-flip: row '${row}' not matched in $tracker" >&2
  fi
  rm -f "$tmp" "${tmp}.err"
  exit $rc
}

mv "$tmp" "$tracker"
status="$(cat "${tmp}.err" 2>/dev/null || true)"
rm -f "${tmp}.err"

# Emit machine-readable result for recipe binding.
case "$status" in
  *FLIPPED*) echo "flipped ${row} (${col_norm}) → ${new_cell}" ;;
  *NOOP*)    echo "noop ${row} (${col_norm}) already at ${new_cell}" ;;
  *)         echo "unknown ${row} (${col_norm})" ;;
esac
