#!/usr/bin/env bash
# Claude Code PostToolUse(Bash) hook — Territory Developer
#
# Output-filter: when the Bash command included "validate:all", parse the tool
# output from stdin and print a condensed error/warning summary to stdout so
# the model can skip scanning the full verbose output.
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
  *validate:all*|*"validate:all"*)
    ;;
  *)
    exit 0
    ;;
esac

# Produce condensed summary from raw output_str
errors="$(printf '%s' "$output_str" | grep -iE "error:|✗|FAIL|Error" | head -20)"
warnings="$(printf '%s' "$output_str" | grep -iE "warning:|WARN" | head -10)"
passed="$(printf '%s' "$output_str" | grep -iE "passed|✓|ok " | tail -5)"
exit_hint="$(printf '%s' "$output_str" | grep -iE "exit [0-9]|exitCode|npm ERR" | head -3)"

cat <<EOF
[filter-validate-all · condensed]
Errors: $(printf '%s' "$errors" | grep -c .)  Warnings: $(printf '%s' "$warnings" | grep -c .)
$([ -n "$errors" ] && printf 'TOP ERRORS:\n%s\n' "$errors")
$([ -n "$passed" ] && printf 'PASSED:\n%s\n' "$passed")
$([ -n "$exit_hint" ] && printf 'EXIT: %s\n' "$exit_hint")
EOF

exit 0
