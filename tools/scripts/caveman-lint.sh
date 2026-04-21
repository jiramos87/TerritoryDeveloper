#!/usr/bin/env bash
# caveman-lint.sh — warn-only soft-lint for caveman authoring surfaces.
# Detects article/hedging/sentence-length drift in prose sections of
# skill/agent/command/project-spec files. Exits 0 always.
#
# Input: reads stdin when piped; falls back to `git diff HEAD` when stdin is TTY.
# Scope: ia/skills/*/SKILL.md  .claude/agents/*.md  .claude/commands/*.md  ia/projects/*.md
# Skips: fenced code blocks (``` delimiters), YAML frontmatter (--- blocks).
#
# Output per hit: file:line:indicator
# Footer: N indicators found in M files (warn-only).
#
# Usage:
#   git diff HEAD | bash tools/scripts/caveman-lint.sh
#   bash tools/scripts/caveman-lint.sh            # auto git diff HEAD fallback
#   git diff HEAD~3..HEAD | bash tools/scripts/caveman-lint.sh
#
# Note: bash 3.2 compatible (macOS default shell).

set +e

# ---------------------------------------------------------------------------
# Read diff input
# ---------------------------------------------------------------------------
if [ -t 0 ]; then
  diff_input="$(git diff HEAD 2>/dev/null)"
else
  diff_input="$(cat)"
fi

# ---------------------------------------------------------------------------
# State variables
# ---------------------------------------------------------------------------
in_scope_file=0
current_file=""
current_line=0
in_fence=0
in_frontmatter=0

total_hits=0
# Track unique files with hits using a newline-separated string
files_with_hits_list=""

output_hit() {
  local file="$1"
  local lineno="$2"
  local indicator="$3"
  printf '%s:%s:%s\n' "$file" "$lineno" "$indicator"
  total_hits=$(( total_hits + 1 ))
  # Record file if not already present
  if ! printf '%s\n' "$files_with_hits_list" | grep -qxF "$file"; then
    if [ -z "$files_with_hits_list" ]; then
      files_with_hits_list="$file"
    else
      files_with_hits_list="${files_with_hits_list}
${file}"
    fi
  fi
}

emit_indicators() {
  local file="$1"
  local lineno="$2"
  local text="$3"

  # Remove inline code spans to avoid false positives inside backticks
  local prose
  prose=$(printf '%s' "$text" | sed 's/`[^`]*`//g')

  # 1. Sentence length: count words; flag if > 12
  local word_count
  word_count=$(printf '%s' "$prose" | wc -w | tr -d ' ')
  if [ "$word_count" -gt 12 ]; then
    output_hit "$file" "$lineno" "sentence_length"
  fi

  # 2. Articles: standalone the / a / an
  # macOS bash 3.2: no \b — use space or line boundary anchors via grep -E
  if printf '%s' "$prose" | grep -qiE '(^|[[:space:]])the([[:space:]]|$)'; then
    output_hit "$file" "$lineno" "article(the)"
  fi
  if printf '%s' "$prose" | grep -qiE '(^|[[:space:]])a([[:space:]]|$)'; then
    output_hit "$file" "$lineno" "article(a)"
  fi
  if printf '%s' "$prose" | grep -qiE '(^|[[:space:]])an([[:space:]]|$)'; then
    output_hit "$file" "$lineno" "article(an)"
  fi

  # 3. Hedging verbs: should / might / could / would
  for verb in should might could would; do
    if printf '%s' "$prose" | grep -qiE "(^|[[:space:]])${verb}([[:space:]]|\$|[,\.!?;:])"; then
      output_hit "$file" "$lineno" "hedging(${verb})"
    fi
  done
}

process_diff_line() {
  local raw_line="$1"

  # Detect new file header (+++ b/path)
  case "$raw_line" in
    '+++ b/'*)
      current_file="${raw_line#+++ b/}"
      current_line=0
      in_fence=0
      in_frontmatter=0
      # Scope check using case patterns
      case "$current_file" in
        ia/skills/*/SKILL.md|\
        .claude/agents/*.md|\
        .claude/commands/*.md|\
        ia/projects/*.md)
          in_scope_file=1 ;;
        *)
          in_scope_file=0 ;;
      esac
      return ;;
  esac

  [ "$in_scope_file" -eq 0 ] && return

  # Track hunk headers: @@ -N,M +S,L @@
  case "$raw_line" in
    '@@ '*)
      # Extract +S from the hunk header
      local hunk_start
      hunk_start=$(printf '%s' "$raw_line" | sed -n 's/^@@ -[0-9]*\(,[0-9]*\)\? +\([0-9]*\).*/\2/p')
      if [ -n "$hunk_start" ]; then
        # Decrement: first '+' line will increment before processing
        current_line=$(( hunk_start - 1 ))
      fi
      in_fence=0
      return ;;
  esac

  # Only process added lines (starting with +, not +++)
  case "$raw_line" in
    '++'*) return ;;
    '+'*)
      local line_text="${raw_line#?}"  # strip leading '+'
      current_line=$(( current_line + 1 ))

      # Frontmatter state machine: --- at line 1 opens, next --- closes
      if [ "$current_line" -eq 1 ] && [ "$line_text" = "---" ]; then
        in_frontmatter=1
        return
      fi
      if [ "$in_frontmatter" -eq 1 ]; then
        if [ "$line_text" = "---" ]; then
          in_frontmatter=0
        fi
        return
      fi

      # Fenced block state machine
      case "$line_text" in
        '```'*)
          if [ "$in_fence" -eq 0 ]; then
            in_fence=1
          else
            in_fence=0
          fi
          return ;;
      esac
      [ "$in_fence" -eq 1 ] && return

      # Skip blank lines
      case "$line_text" in
        '') return ;;
      esac

      # Skip comment lines (# ...)
      case "$line_text" in
        '#'*) return ;;
      esac

      # Skip markdown headings
      case "$line_text" in
        '# '*|'## '*|'### '*|'#### '*|'##### '*|'###### '*) return ;;
      esac

      # Skip table rows
      case "$line_text" in
        '|'*) return ;;
      esac

      # Emit indicators
      emit_indicators "$current_file" "$current_line" "$line_text"
      ;;
  esac
}

# ---------------------------------------------------------------------------
# Process each line of the diff
# ---------------------------------------------------------------------------
while IFS= read -r raw_line; do
  process_diff_line "$raw_line"
done <<< "$diff_input"

# ---------------------------------------------------------------------------
# Footer
# ---------------------------------------------------------------------------
file_count=0
if [ -n "$files_with_hits_list" ]; then
  file_count=$(printf '%s\n' "$files_with_hits_list" | wc -l | tr -d ' ')
fi
printf '%d indicators found in %d files (warn-only).\n' "$total_hits" "$file_count"

# Always exit 0 — warn-only posture
exit 0
