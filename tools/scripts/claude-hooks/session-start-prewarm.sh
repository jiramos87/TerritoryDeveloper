#!/usr/bin/env bash
# Claude Code SessionStart hook — Territory Developer
#
# Wired in .claude/settings.json under hooks.SessionStart.
# Runs once when a Claude Code session starts. Anything printed to stdout is
# injected as additional context for the model. Keep the output short and
# scannable — every line is consumed by the model context window.
#
# Behavior (TECH-85 / Stage 3 shrink pass):
#   - branch:        current git branch (or detached HEAD info)
#   - tree:          dirty file count from `git status --porcelain`
#   - verify:local:  exit code of the most recent `npm run verify:local`,
#                    read from .claude/last-verify-exit-code if present.
#                    Falls back to "unknown".
#   - bridge:        last db:bridge-preflight exit, read from
#                    .claude/last-bridge-preflight-exit-code if present.
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

verify_marker="$REPO_ROOT/.claude/last-verify-exit-code"
if [ -f "$verify_marker" ]; then
  last_verify="$(tr -d '\n[:space:]' < "$verify_marker")"
else
  last_verify="unknown"
fi

bridge_marker="$REPO_ROOT/.claude/last-bridge-preflight-exit-code"
if [ -f "$bridge_marker" ]; then
  last_bridge="$(tr -d '\n[:space:]' < "$bridge_marker")"
else
  last_bridge="not run"
fi

cat <<EOF
[territory-developer · prewarm]
branch: $branch   tree: $dirty_count dirty
verify:local: $last_verify   bridge-preflight: $last_bridge
memory: MEMORY.md + ~/.claude-personal/.../memory/
EOF

exit 0
