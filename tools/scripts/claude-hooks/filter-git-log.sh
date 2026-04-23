#!/usr/bin/env bash
# Claude Code PostToolUse(Bash) hook — Territory Developer
#
# Output-filter: when the Bash command is a git log call, print a condensed
# one-line-per-commit summary (sha + subject only) to cap context bloat.
#
# Always exits 0 (advisory only).

set +e

input="$(cat)"

if command -v jq >/dev/null 2>&1; then
  command_str="$(printf '%s' "$input" | jq -r '.tool_input.command // ""' 2>/dev/null)"
  output_str="$(printf '%s' "$input" | jq -r '.tool_response // .tool_result // ""' 2>/dev/null)"
else
  command_str="$(printf '%s' "$input" | sed -n 's/.*"command"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
  output_str=""
fi

case "$command_str" in
  *"git log"*|*"git --no-pager log"*)
    ;;
  *)
    exit 0
    ;;
esac

# Emit at most 20 lines condensed
condensed="$(printf '%s' "$output_str" | grep -E "^commit |^[a-f0-9]{7,}|feat|fix|chore|refactor|docs|style|test|build|ci" | head -40 | paste - - 2>/dev/null | head -20)"
[ -z "$condensed" ] && condensed="$(printf '%s' "$output_str" | head -20)"

cat <<EOF
[filter-git-log · condensed — first 20 entries]
$condensed
EOF

exit 0
