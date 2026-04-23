#!/usr/bin/env bash
# Claude Code PostToolUse(Bash) hook — Territory Developer
#
# Output-filter: when the Bash command included "unity:compile-check", print a
# condensed error count + first error lines so the model skips verbose output.
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
  *unity:compile-check*|*"compile-check"*|*"unity:compile"*)
    ;;
  *)
    exit 0
    ;;
esac

errors="$(printf '%s' "$output_str" | grep -iE "error CS|error:|Error " | head -15)"
warnings="$(printf '%s' "$output_str" | grep -iE "warning CS|warning:" | head -5)"
status="$(printf '%s' "$output_str" | grep -iE "compile|success|fail|exit" | tail -3)"

cat <<EOF
[filter-unity-compile · condensed]
CS errors: $(printf '%s' "$errors" | grep -c .)  CS warnings: $(printf '%s' "$warnings" | grep -c .)
$([ -n "$errors" ] && printf 'FIRST ERRORS:\n%s\n' "$errors")
$([ -n "$status" ] && printf 'STATUS: %s\n' "$status")
EOF

exit 0
