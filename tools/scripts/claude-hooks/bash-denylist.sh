#!/usr/bin/env bash
# Claude Code PreToolUse(Bash) hook — Territory Developer
#
# Wired in .claude/settings.json under hooks.PreToolUse with matcher "Bash".
# Reads the tool input from stdin (JSON: {tool_name, tool_input:{command,...}})
# and exits with code 2 to **block** the tool call when the command matches a
# destructive pattern. Exit 2 surfaces the stderr to the model so it can adjust.
#
# Belt-and-suspenders with .claude/settings.json permissions.deny — the hook
# runs even when permissions are bypassed and gives a single canonical place
# to extend the denylist without re-deploying settings.
#
# Patterns blocked (TECH-85 / Phase 1.3):
#   - git push --force / -f / --force-with-lease
#   - git reset --hard
#   - git clean -fd / -fdx
#   - rm -rf .cursor* / ia* / MEMORY.md* / .claude* / .git*
#   - rm -rf / / ~
#   - sudo *
#
# Test from CLI (no Claude Code):
#   echo '{"tool_input":{"command":"git push --force origin main"}}' | \
#     tools/scripts/claude-hooks/bash-denylist.sh
#   echo $?   # → 2
set +e

input="$(cat)"

# Extract the command field from the JSON input. Prefer python3 if available
# for safe parsing; fall back to a forgiving sed/grep when not.
if command -v python3 >/dev/null 2>&1; then
  command_str="$(printf '%s' "$input" | python3 -c '
import json, sys
try:
    data = json.loads(sys.stdin.read() or "{}")
except Exception:
    print("")
    sys.exit(0)
ti = data.get("tool_input") or {}
print(ti.get("command", "") if isinstance(ti, dict) else "")
')"
else
  command_str="$(printf '%s' "$input" | sed -n 's/.*"command"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
fi

[ -z "$command_str" ] && exit 0

deny_match=""
case "$command_str" in
  *"git push --force"*|*"git push -f "*|*"git push -f"|*"git push --force-with-lease"*)
    deny_match="git push --force / -f / --force-with-lease" ;;
  *"git reset --hard"*)
    deny_match="git reset --hard" ;;
  *"git clean -fd"*|*"git clean -fdx"*)
    deny_match="git clean -fd / -fdx" ;;
  *"rm -rf .cursor"*|*"rm -rf ./.cursor"*)
    deny_match="rm -rf .cursor*" ;;
  *"rm -rf ia"*|*"rm -rf ./ia"*)
    deny_match="rm -rf ia*" ;;
  *"rm -rf MEMORY.md"*|*"rm -rf ./MEMORY.md"*)
    deny_match="rm -rf MEMORY.md*" ;;
  *"rm -rf .claude"*|*"rm -rf ./.claude"*)
    deny_match="rm -rf .claude*" ;;
  *"rm -rf .git"*|*"rm -rf ./.git"*)
    deny_match="rm -rf .git*" ;;
  "rm -rf /"*|*" rm -rf /"*)
    deny_match="rm -rf /" ;;
  *"rm -rf ~"*)
    deny_match="rm -rf ~" ;;
  "sudo "*|*" sudo "*)
    deny_match="sudo *" ;;
esac

if [ -n "$deny_match" ]; then
  cat >&2 <<EOF
[territory-developer · bash-denylist] BLOCKED: '$deny_match' is on the project denylist.
  command: $command_str
  Why: TECH-85 / Phase 1.3 — destructive bash is enforced at the hook layer to
  protect .cursor / ia / .claude / MEMORY.md / .git from accidental removal,
  and to prevent force-push to shared branches. If you genuinely need this,
  ask the human to run it manually outside Claude Code.
EOF
  exit 2
fi

exit 0
