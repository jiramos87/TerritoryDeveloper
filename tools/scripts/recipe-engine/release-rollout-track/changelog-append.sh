#!/usr/bin/env bash
# release-rollout-track — Phase 3: append row to ## Change log table.
# Args: --tracker <path> --row <slug> --col <a..g> --marker <glyph>
#       --ticket <text> --note <text> [--date YYYY-MM-DD]
# Appends:
#   | YYYY-MM-DD | {row} cell ({col}) → {marker}; ticket: {ticket} ({note}) | release-rollout-track |
# Idempotent best-effort: skips append if an identical line already present.
set -euo pipefail

tracker=""
row=""
col=""
marker=""
ticket=""
note=""
date_arg=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --tracker) tracker="$2"; shift 2 ;;
    --row)     row="$2";     shift 2 ;;
    --col)     col="$2";     shift 2 ;;
    --marker)  marker="$2";  shift 2 ;;
    --ticket)  ticket="$2";  shift 2 ;;
    --note)    note="$2";    shift 2 ;;
    --date)    date_arg="$2"; shift 2 ;;
    *) shift ;;
  esac
done

col_norm="${col#(}"
col_norm="${col_norm%)}"

if [[ -z "$date_arg" ]]; then
  date_arg="$(date +%Y-%m-%d)"
fi

new_line="| ${date_arg} | ${row} cell (${col_norm}) → ${marker}; ticket: ${ticket} (${note}) | release-rollout-track |"

if grep -F -q "$new_line" "$tracker"; then
  echo "noop changelog row already present"
  exit 0
fi

# Append directly to file end. Tracker convention: ## Change log table is last
# section; trailing rows accumulate at EOF. If file ends without newline, awk
# `END{print ""}` covers that.
{
  if [[ -s "$tracker" ]] && [[ -z "$(tail -c 1 "$tracker")" ]]; then
    :
  else
    printf '\n' >> "$tracker"
  fi
  printf '%s\n' "$new_line" >> "$tracker"
}

echo "appended ${date_arg} ${row} (${col_norm}) → ${marker}"
