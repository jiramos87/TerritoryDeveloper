#!/usr/bin/env bash
# Claude Code PreToolUse(Read) hook — Territory Developer
#
# Wired in .claude/settings.json under hooks.PreToolUse with matcher "Read".
# Reads {tool_name, tool_input:{file_path,...}, ...} from stdin and emits a
# caveman-style advisory on stderr when the read targets Assets/**/*.cs files
# exceeding 800 lines. Pure nudge; always exits 0 and never gates the Read.
#
# Bypass: set BIG_FILE_READ_OK=1 to silence the advisory for one Read call
# (legitimate full read). Mirrors the env-flag pattern used elsewhere in the
# hook layer.
#
# Pattern reuse:
#   - JSON stdin parse (jq primary + sed fallback): bash-denylist.sh:41-45
#   - Assets/**/*.cs glob match: cs-edit-reminder.sh:25
#
# Test from CLI (no Claude Code):
#   echo '{"tool_input":{"file_path":"'"$PWD"'/Assets/Scripts/Managers/GameManagers/TerrainManager.cs"}}' \
#     | tools/scripts/claude-hooks/big-file-read-warn.sh
#   echo $?   # → 0; advisory on stderr if file >800 lines
#
# Threshold: 800 lines. Fixed per docs/audit/compaction-loop-mitigation.md
# Tier A4. Do not change without revisiting the seed analysis.

set +e

THRESHOLD=800

input="$(cat)"

# Bypass: explicit env flag silences the hook for one Read call.
if [ "${BIG_FILE_READ_OK:-}" = "1" ]; then
  exit 0
fi

# Extract tool_input.file_path. jq when available (handles escaped quotes
# correctly); sed fallback otherwise. Same conservative-empty pattern as
# bash-denylist.sh: missing field → empty string → hook stays silent.
if command -v jq >/dev/null 2>&1; then
  file_path="$(printf '%s' "$input" | jq -r '.tool_input.file_path // ""' 2>/dev/null)"
else
  file_path="$(printf '%s' "$input" | sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
fi

[ -z "$file_path" ] && exit 0

# Glob-match Assets/**/*.cs. Mirrors cs-edit-reminder.sh:25 — accepts both
# absolute paths under .../Assets/.../*.cs and repo-relative Assets/*.cs.
case "$file_path" in
  *"/Assets/"*.cs|"Assets/"*.cs)
    ;;
  *)
    exit 0
    ;;
esac

# File must exist + be readable for line-count to be meaningful. Missing →
# silent (Read tool will surface its own error).
[ -r "$file_path" ] || exit 0

line_count="$(wc -l < "$file_path" 2>/dev/null | tr -d '[:space:]')"
[ -z "$line_count" ] && exit 0

# Numeric guard: if line_count is not an integer for any reason, stay silent.
case "$line_count" in
  ''|*[!0-9]*) exit 0 ;;
esac

if [ "$line_count" -gt "$THRESHOLD" ]; then
  cat >&2 <<EOF
[territory-developer · big-file-read-warn]
Big C# file read: $file_path ($line_count lines, threshold $THRESHOLD).
Use \`csharp_class_summary\` MCP tool first — returns class/method/field
outline at a fraction of the token cost. Edits: jump straight to the
target slice via spec_section / line range once you have the outline.
Bypass: BIG_FILE_READ_OK=1 to silence (legitimate full read).
EOF
fi

exit 0
