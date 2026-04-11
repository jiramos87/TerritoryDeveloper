#!/usr/bin/env bash
# Claude Code Stop hook — Territory Developer
#
# Wired in .claude/settings.json under hooks.Stop. Runs once when the model
# stops responding to the user. Prints an advisory reminder that the final
# message should include a Verification block per
# docs/agent-led-verification-policy.md when the session touched
# Assets/**/*.cs or tools/mcp-ia-server/**.
#
# Heuristic: scan `git status --porcelain` for modified/added paths under
# Assets/**/*.cs or tools/mcp-ia-server/**. This is best-effort and advisory;
# always exits 0.

set +e

REPO_ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
cd "$REPO_ROOT" || exit 0

changed="$(git status --porcelain 2>/dev/null)"
[ -z "$changed" ] && exit 0

touched_cs=0
touched_mcp=0
while IFS= read -r line; do
  path="${line:3}"
  case "$path" in
    Assets/*.cs|*/Assets/*.cs|"Assets/"*) touched_cs=1 ;;
  esac
  case "$path" in
    tools/mcp-ia-server/*|"tools/mcp-ia-server/"*) touched_mcp=1 ;;
  esac
done <<EOF
$changed
EOF

if [ "$touched_cs" -eq 0 ] && [ "$touched_mcp" -eq 0 ]; then
  exit 0
fi

reasons=""
[ "$touched_cs" -eq 1 ] && reasons="$reasons Assets/**/*.cs"
[ "$touched_mcp" -eq 1 ] && reasons="$reasons tools/mcp-ia-server/**"

cat <<EOF
[territory-developer · verification-reminder]
Session touched:$reasons
Per docs/agent-led-verification-policy.md, your final message should include a
Verification block with at minimum:
  - Node / IA: npm run validate:all (exit code)
  - Unity compile: npm run unity:compile-check (when Assets/**/*.cs changed)
  - Path A or B: as applicable
Mark any check N/A with a one-line reason.
EOF

exit 0
