#!/usr/bin/env bash
# Claude Code PreToolUse(Edit|Write|MultiEdit) hook — Territory Developer
#
# Wired in .claude/settings.json under hooks.PreToolUse with matcher
# "Edit|Write|MultiEdit". Reads the tool input from stdin (JSON shape:
# {tool_name, tool_input:{file_path,...}}; for MultiEdit also tool_input.edits[]
# but the file_path field is set on the wrapper too) and decides:
#
#   1. DENY — file_path matches `.claude/agents/{slug}.md` or
#             `.claude/commands/{slug}.md` (excluding `_retired/` subdirs).
#             Exit 2 with stderr; surfaced to the model so it switches to
#             editing the canonical SKILL.md frontmatter + body partials.
#   2. ALLOW — anything else; exit 0 silently.
#
# Override: set SKILL_TOOLS_DIRECT_EDIT=1 in the env to bypass the gate (used
# during skill-tools migration when authoring partials in lockstep).
#
# Surfaces under `.claude/agents/` and `.claude/commands/` are GENERATED from
# `ia/skills/{slug}/SKILL.md` frontmatter via `tools/scripts/skill-tools/`.
# Direct edits drift instantly because `npm run validate:skill-drift` (in
# `validate:all`) regenerates and diffs on every CI run. The right edit
# surface is:
#   - frontmatter changes  → `ia/skills/{slug}/SKILL.md`
#   - prose / body changes → `ia/skills/{slug}/agent-body.md` (or
#                            `command-body.md`) under the same directory
# Then run `npm run skill:sync:all` to regenerate.
#
# Test from CLI (no Claude Code):
#   echo '{"tool_input":{"file_path":".claude/agents/ship.md"}}' | \
#     tools/scripts/claude-hooks/skill-surface-guard.sh
#   echo $?   # → 2
set +e

# Override hatch — caller has already accepted the drift implications.
if [ "${SKILL_TOOLS_DIRECT_EDIT}" = "1" ]; then
  exit 0
fi

input="$(cat)"

# Extract file_path. jq when available; sed fallback (conservative-allow on
# parse failure — never block on hook self-error).
if command -v jq >/dev/null 2>&1; then
  file_path="$(printf '%s' "$input" | jq -r '.tool_input.file_path // ""' 2>/dev/null)"
else
  file_path="$(printf '%s' "$input" | sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
fi

[ -z "$file_path" ] && exit 0

# Normalize: strip leading project root or `./`. Match against trailing path.
case "$file_path" in
  *"/.claude/agents/_retired/"*|*"/.claude/commands/_retired/"*) exit 0 ;;
  *"/.claude/agents/_preamble/"*|*"/.claude/commands/_preamble/"*) exit 0 ;;
  *".claude/agents/_retired/"*|*".claude/commands/_retired/"*) exit 0 ;;
  *".claude/agents/_preamble/"*|*".claude/commands/_preamble/"*) exit 0 ;;
esac

surface=""
case "$file_path" in
  */.claude/agents/*.md|.claude/agents/*.md)
    surface="agent" ;;
  */.claude/commands/*.md|.claude/commands/*.md)
    surface="command" ;;
esac

if [ -z "$surface" ]; then
  exit 0
fi

# Resolve slug from filename stem.
slug="$(printf '%s' "$file_path" | sed -E 's|.*/||; s|\.md$||')"

surface_dir="${surface}s"

cat >&2 <<EOF
[territory-developer · skill-surface-guard] BLOCKED: direct edit of generated $surface file.
  file_path: $file_path
  surface:   $surface
  Why: \`.claude/$surface_dir/{slug}.md\` is GENERATED from \`ia/skills/$slug/SKILL.md\`
  frontmatter via \`tools/scripts/skill-tools/\`. Direct edits drift on next
  \`npm run skill:sync:all\` and are caught by \`npm run validate:skill-drift\`
  (in \`validate:all\`).
  Fix: edit the canonical surface instead.
    - frontmatter  → ia/skills/$slug/SKILL.md
    - prose/body   → ia/skills/$slug/${surface}-body.md (under same directory)
    - regenerate   → npm run skill:sync:all
  Override (advanced, e.g. mid-migration): set SKILL_TOOLS_DIRECT_EDIT=1.
EOF
exit 2
