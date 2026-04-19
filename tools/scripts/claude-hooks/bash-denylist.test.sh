#!/usr/bin/env bash
# bash-denylist.test.sh — fixture for bash-denylist.sh
#
# Tests:
#   DENY cases  → hook must exit 2
#   ALLOW cases → hook must exit 0 (allow JSON emitted)
#   ASK cases   → hook must exit 0 (ask JSON emitted)
#
# Run: bash tools/scripts/claude-hooks/bash-denylist.test.sh
# Exit 0 → all assertions pass. Exit 1 → at least one failure.

set +e

HOOK="$(dirname "$0")/bash-denylist.sh"
PASS=0
FAIL=0

assert_exit() {
  local label="$1"
  local expected="$2"
  local cmd_json="$3"

  actual="$(printf '%s' "$cmd_json" | "$HOOK" >/dev/null 2>&1; echo $?)"
  if [ "$actual" = "$expected" ]; then
    echo "PASS [$label] exit=$actual"
    PASS=$((PASS + 1))
  else
    echo "FAIL [$label] expected=$expected got=$actual"
    FAIL=$((FAIL + 1))
  fi
}

# ---------------------------------------------------------------------------
# Build deny-pattern strings via concatenation so this script file does NOT
# contain literal denylist patterns (which would cause the hook to block the
# Bash tool call that runs this test script in Claude Code sessions).
# ---------------------------------------------------------------------------

push="git push"
force="--force"
force_f="-f"
force_lease="--force-with-lease"
reset="git reset"
hard="--hard"
clean="git clean"
rmrf="rm -rf"
sudo_cmd="sudo"

# DENY: git push variants
assert_exit "deny: push --force" 2 \
  "{\"tool_input\":{\"command\":\"$push $force origin main\"}}"

assert_exit "deny: push -f" 2 \
  "{\"tool_input\":{\"command\":\"$push $force_f origin main\"}}"

assert_exit "deny: push --force-with-lease" 2 \
  "{\"tool_input\":{\"command\":\"$push $force_lease\"}}"

# DENY: git reset --hard
assert_exit "deny: reset --hard" 2 \
  "{\"tool_input\":{\"command\":\"$reset $hard\"}}"

# DENY: git clean -fd
assert_exit "deny: clean -fd" 2 \
  "{\"tool_input\":{\"command\":\"$clean -fd\"}}"

# DENY: rm -rf ia
assert_exit "deny: rm -rf ia" 2 \
  "{\"tool_input\":{\"command\":\"$rmrf ia/specs\"}}"

# DENY: rm -rf MEMORY.md
assert_exit "deny: rm -rf MEMORY.md" 2 \
  "{\"tool_input\":{\"command\":\"$rmrf MEMORY.md\"}}"

# DENY: rm -rf .claude
assert_exit "deny: rm -rf .claude" 2 \
  "{\"tool_input\":{\"command\":\"$rmrf .claude\"}}"

# DENY: sudo
assert_exit "deny: sudo" 2 \
  "{\"tool_input\":{\"command\":\"$sudo_cmd ls\"}}"

# DENY: push --force with escaped-quote path (jq primary handles this correctly;
# sed fallback would return empty string → allow for this case).
# JSON value: git push --force "foo \"bar\" baz" (literal quotes in command).
assert_exit "deny: push --force escaped-quote" 2 \
  '{"tool_input":{"command":"git push --force \"foo \\\"bar\\\" baz\""}}'

# ALLOW: safe read-only command
assert_exit "allow: ls -la" 0 \
  '{"tool_input":{"command":"ls -la"}}'

# ALLOW: empty command → guard [ -z ] fires → exit 0
assert_exit "allow: empty command" 0 \
  '{"tool_input":{"command":""}}'

# ALLOW: missing tool_input → exit 0
assert_exit "allow: no tool_input" 0 \
  '{}'

# ASK: git commit → exit 0 (ask JSON, not exit 2)
assert_exit "ask: git commit" 0 \
  '{"tool_input":{"command":"git commit -m \"chore: test\""}}'

# ---------------------------------------------------------------------------
echo ""
echo "Results: $PASS passed, $FAIL failed"
[ "$FAIL" -eq 0 ]
