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

# ── tests/scenarios denylist branch ──────────────────────────────────────────
# Deny Write/MultiEdit on ^(tests|tools/fixtures/scenarios)/.* unless
# TD_ALLOW_TEST_EDIT={ISSUE_ID} env var is set.
# For Edit: also deny if old_string contains [Test] / it( / test( declaration
# tokens AND new_string does NOT (i.e. the edit removes those tokens) — without
# env var override.

if command -v jq >/dev/null 2>&1; then
  tool_name="$(printf '%s' "$input" | jq -r '.tool_name // ""' 2>/dev/null)"
  file_path_guard="$(printf '%s' "$input" | jq -r '.tool_input.file_path // ""' 2>/dev/null)"
  old_string="$(printf '%s' "$input" | jq -r '.tool_input.old_string // ""' 2>/dev/null)"
  new_string="$(printf '%s' "$input" | jq -r '.tool_input.new_string // ""' 2>/dev/null)"
else
  tool_name=""
  file_path_guard=""
  old_string=""
  new_string=""
fi

_is_test_path=0
case "$file_path_guard" in
  tests/*|*/tests/*|tools/fixtures/scenarios/*|*/tools/fixtures/scenarios/*)
    _is_test_path=1 ;;
esac

if [ "$_is_test_path" -eq 1 ]; then
  case "$tool_name" in
    Write|MultiEdit)
      if [ -z "${TD_ALLOW_TEST_EDIT}" ]; then
        cat >&2 <<EOF
[territory-developer · skill-surface-guard] BLOCKED: direct Write/MultiEdit on test path.
  file_path: $file_path_guard
  tool:      $tool_name
  Why: Writing to tests/ or tools/fixtures/scenarios/ is gated to prevent
  accidental deletion or replacement of test assertions during ship-cycle.
  Fix: set TD_ALLOW_TEST_EDIT={ISSUE_ID} in the environment to override.
    e.g. export TD_ALLOW_TEST_EDIT=TECH-36112
EOF
        exit 2
      fi
      ;;
    Edit)
      if [ -z "${TD_ALLOW_TEST_EDIT}" ]; then
        # Deny if old_string contains declaration tokens but new_string does not.
        _old_has_decl=0
        _new_has_decl=0
        if printf '%s' "$old_string" | grep -qE '\[Test\]|[[:space:]]it\(|[[:space:]]test\(|^it\(|^test\('; then
          _old_has_decl=1
        fi
        if printf '%s' "$new_string" | grep -qE '\[Test\]|[[:space:]]it\(|[[:space:]]test\(|^it\(|^test\('; then
          _new_has_decl=1
        fi
        if [ "$_old_has_decl" -eq 1 ] && [ "$_new_has_decl" -eq 0 ]; then
          cat >&2 <<EOF
[territory-developer · skill-surface-guard] BLOCKED: Edit removes test/it declaration token.
  file_path: $file_path_guard
  Why: The edit removes [Test], it(, or test( declaration tokens, which would
  silently delete test coverage. Gate ensures test removal is intentional.
  Fix: set TD_ALLOW_TEST_EDIT={ISSUE_ID} in the environment to override.
    e.g. export TD_ALLOW_TEST_EDIT=TECH-36112
EOF
          exit 2
        fi
      fi
      ;;
  esac
fi
# ── end tests/scenarios denylist branch ──────────────────────────────────────

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
