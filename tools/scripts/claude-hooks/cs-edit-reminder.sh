#!/usr/bin/env bash
# Claude Code PostToolUse(Edit|Write|MultiEdit) hook — Territory Developer
#
# Wired in .claude/settings.json under hooks.PostToolUse with matcher
# "Edit|Write|MultiEdit". Reads {tool_name, tool_input:{file_path,...}, ...}
# from stdin and prints an advisory reminder when the edited file matches
# Assets/**/*.cs.
#
# Always exits 0. This is a soft reminder, not a gate. The model can still
# defer the compile until verification time.

set +e

input="$(cat)"

if command -v jq >/dev/null 2>&1; then
  file_path="$(printf '%s' "$input" | jq -r '.tool_input.file_path // ""' 2>/dev/null)"
else
  file_path="$(printf '%s' "$input" | sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
fi

[ -z "$file_path" ] && exit 0

case "$file_path" in
  *"/Assets/"*.cs|"Assets/"*.cs)
    cat <<EOF
[territory-developer · cs-edit-reminder]
You just edited a Unity C# file: $file_path
Before declaring the change verified, run:
  npm run unity:compile-check
(see CLAUDE.md → Unity batch compile; or docs/agent-led-verification-policy.md
for the full Verification block requirements when Assets/**/*.cs changed.)
EOF
    ;;
esac

exit 0
