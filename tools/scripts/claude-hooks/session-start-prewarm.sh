#!/usr/bin/env bash
# Claude Code SessionStart hook — Territory Developer
#
# Wired in .claude/settings.json under hooks.SessionStart.
# Runs once when a Claude Code session starts. Anything printed to stdout is
# injected as additional context for the model. Keep the output short and
# scannable — every line is consumed by the model context window.
#
# Behavior:
#   - branch:        current git branch (or detached HEAD info)
#   - tree:          dirty file count from `git status --porcelain`
#   - verify:local:  exit code of the most recent `npm run verify:local`,
#                    read from ia/state/runtime-state.json if present.
#                    Falls back to "unknown".
#   - bridge:        last db:bridge-preflight exit, read from
#                    ia/state/runtime-state.json if present.
#                    Falls back to "not run".
#
# Failure mode: this hook is advisory. It must never block a session start.
# If anything fails, print a single warning line and exit 0.

set +e

REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
cd "$REPO_ROOT" || exit 0

branch="$(git symbolic-ref --short HEAD 2>/dev/null || git rev-parse --short HEAD 2>/dev/null || echo unknown)"

dirty_count="$(git status --porcelain 2>/dev/null | wc -l | tr -d ' ')"
[ -z "$dirty_count" ] && dirty_count="?"

rs_json="$REPO_ROOT/ia/state/runtime-state.json"
if [ -f "$rs_json" ] && command -v jq >/dev/null 2>&1; then
  lv="$(jq -r '.last_verify_exit_code // empty' "$rs_json" 2>/dev/null)"
  if [ -z "$lv" ] || [ "$lv" = "null" ] || [ "$lv" = "-1" ]; then
    last_verify="unknown"
  else
    last_verify="$lv"
  fi
  lb="$(jq -r '.last_bridge_preflight_exit_code // empty' "$rs_json" 2>/dev/null)"
  if [ -z "$lb" ] || [ "$lb" = "null" ] || [ "$lb" = "-1" ]; then
    last_bridge="not run"
  else
    last_bridge="$lb"
  fi
else
  last_verify="unknown"
  last_bridge="not run"
fi

cat <<EOF
[territory-developer · prewarm]
branch: $branch   tree: $dirty_count dirty
verify:local: $last_verify   bridge-preflight: $last_bridge
memory: MEMORY.md + ~/.claude-personal/projects/-Users-javier-bacayo-studio-territory-developer/memory/MEMORY.md
EOF

exit 0
