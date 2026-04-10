#!/usr/bin/env bash
# Claude Code SessionStart hook — Territory Developer
#
# Wired in .claude/settings.json under hooks.SessionStart.
# Runs once when a Claude Code session starts. Anything printed to stdout is
# injected as additional context for the model. Keep the output short and
# scannable — every line is consumed by the model context window.
#
# Behavior (TECH-85 / Phase 1.3):
#   - branch:        current git branch (or detached HEAD info)
#   - last verify:   exit code of the most recent `npm run verify:local`,
#                    read from .claude/last-verify-exit-code if present (the
#                    verify-local helper writes this marker file in Stage 1+).
#                    Falls back to "unknown".
#   - in-progress:   top in-progress issue ids from BACKLOG.md (`In progress`
#                    section, first 3 ids — best-effort grep).
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

in_progress=""
if [ -f "$REPO_ROOT/BACKLOG.md" ]; then
  in_progress="$(awk '
    /^## / { in_section = ($0 ~ /[Ii]n progress/); next }
    in_section && /^- \[ \] \*\*[A-Z]+-[0-9]+[a-z0-9]*\*\*/ {
      match($0, /\*\*[A-Z]+-[0-9]+[a-z0-9]*\*\*/)
      id = substr($0, RSTART+2, RLENGTH-4)
      print id
      n++
      if (n >= 3) exit
    }
  ' "$REPO_ROOT/BACKLOG.md" | tr '\n' ' ' | sed 's/[[:space:]]*$//')"
fi
[ -z "$in_progress" ] && in_progress="(none detected)"

cat <<EOF
[territory-developer · session-start-prewarm]
branch:           $branch
last verify:local exit: $last_verify
last bridge preflight exit: $last_bridge
top in-progress: $in_progress
hooks:           SessionStart, PreToolUse(Bash) denylist, PostToolUse(Edit|Write) cs-reminder, Stop verification reminder
project memory:  MEMORY.md (root)
slash commands:  /kickoff /implement /verify /testmode /closeout (Stage 1 stubs — coming in Stage 4)
EOF

exit 0
