#!/usr/bin/env bash
# Claude Code PreToolUse(Bash) hook — Territory Developer
#
# Wired in .claude/settings.json under hooks.PreToolUse with matcher "Bash".
# Reads the tool input from stdin (JSON: {tool_name, tool_input:{command,...}})
# and decides one of three outcomes for the Bash call:
#
#   1. DENY  — destructive pattern matched. Exit 2 with stderr; surfaced to the
#              model so it can adjust. Belt-and-suspenders with permissions.deny.
#   2. ASK   — mutative git operation matched (commit / push / reset / etc.).
#              Emit a permissionDecision: "ask" JSON so Claude Code prompts the
#              human before running. Replaces the legacy permissions.ask bucket
#              for Bash because compound commands cannot be expressed as prefix
#              patterns in settings.json.
#   3. ALLOW — anything else. Emit a permissionDecision: "allow" JSON so the
#              command runs without prompting. This makes the hook the single
#              source of truth for Bash gating: the static permissions.allow
#              list in settings.json no longer needs to enumerate every safe
#              prefix, and compound commands (loops, pipelines, command
#              substitution) work without per-call human approval.
#
# Patterns blocked:
#   - git push --force / -f / --force-with-lease
#   - git reset --hard
#   - git clean -fd / -fdx
#   - rm -rf ia* / MEMORY.md* / .claude* / .git*
#   - rm -rf / / ~
#   - sudo *
#
# Test from CLI (no Claude Code):
#   echo '{"tool_input":{"command":"git push --force origin main"}}' | \
#     tools/scripts/claude-hooks/bash-denylist.sh
#   echo $?   # → 2
set +e

input="$(cat)"

# Extract the command field from the JSON input. Use jq when available (handles
# escaped quotes correctly); fall back to sed with conservative-deny behavior
# (escaped quotes → empty string → hook allows) when jq is not on PATH.
if command -v jq >/dev/null 2>&1; then
  command_str="$(printf '%s' "$input" | jq -r '.tool_input.command // ""' 2>/dev/null)"
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
  Why: destructive bash is enforced at the hook layer to protect
  ia / .claude / MEMORY.md / .git from accidental removal, and
  to prevent force-push to shared branches. If you genuinely need this,
  ask the human to run it manually outside Claude Code.
EOF
  exit 2
fi

# Mutative git operations that should still prompt the human even though they
# are not destructive enough to deny outright. Mirrors the legacy `ask` bucket
# in .claude/settings.json so commit / push / branch-state-changing operations
# never run silently. Substring match (anywhere in the command) so pipelines
# and `cd … && git …` chains still trigger the prompt.
ask_match=""
case "$command_str" in
  *"git add "*|*"git add"|*"git add."*)
    ask_match="git add" ;;
  *"git commit"*)
    ask_match="git commit" ;;
  *"git push"*)
    ask_match="git push" ;;
  *"git restore"*)
    ask_match="git restore" ;;
  *"git checkout"*)
    ask_match="git checkout" ;;
  *"git merge"*)
    ask_match="git merge" ;;
  *"git rebase"*)
    ask_match="git rebase" ;;
  *"git reset"*)
    ask_match="git reset" ;;
  *"git stash"*)
    ask_match="git stash" ;;
  *"git clean"*)
    ask_match="git clean" ;;
esac

if [ -n "$ask_match" ]; then
  # JSON-escape command_str for the reason field (backslash + double quote).
  esc_command="$(printf '%s' "$command_str" | sed 's/\\/\\\\/g; s/"/\\"/g')"
  printf '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"ask","permissionDecisionReason":"bash-denylist hook: \"%s\" is a mutative git operation; confirm before running: %s"}}\n' "$ask_match" "$esc_command"
  exit 0
fi

# Auto-approve any Bash command that is not on the denylist or the ask list.
# Compound commands (for/while loops, pipelines, command substitution) cannot
# be expressed as prefix patterns in .claude/settings.json `allow`, so we
# delegate the decision to this hook. The denylist above is the single source
# of truth for what is blocked; the ask list is the single source of truth for
# what still prompts the human; everything else is allowed without prompting.
cat <<'JSON'
{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"allow","permissionDecisionReason":"bash-denylist hook: command not on project denylist or ask list"}}
JSON
exit 0
