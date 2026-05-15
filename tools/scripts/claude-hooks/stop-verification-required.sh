#!/usr/bin/env bash
# Claude Code Stop hook — Territory Developer
#
# Wired in .claude/settings.json under hooks.Stop[]. Reads the session-stop
# context JSON from stdin. Shape (Claude Code Stop hook contract):
#   { "touched_files": [...], "response_text": "..." }
#
# Decision:
#   IF any touched file matches ^(Assets/Scripts/.*\.cs|tools/mcp-ia-server/.*|Domains/.*)
#   AND response_text does NOT contain a Verification block JSON header
#   THEN exit 2 (block stop) with stderr reason.
#   ELSE exit 0.
#
# Verification block header regex: ```json followed by {"verification" anywhere in response.
#
# Override: set SKIP_STOP_VERIFICATION=1 to bypass (emergency use only).
#
# Test from CLI:
#   echo '{"touched_files":["Assets/Scripts/Foo.cs"],"response_text":"no block"}' | \
#     tools/scripts/claude-hooks/stop-verification-required.sh
#   echo $?   # → 2
set +e

if [ "${SKIP_STOP_VERIFICATION}" = "1" ]; then
  exit 0
fi

input="$(cat)"

# Extract touched_files array as newline-separated list.
if command -v jq >/dev/null 2>&1; then
  touched_files="$(printf '%s' "$input" | jq -r '.touched_files[]? // empty' 2>/dev/null)"
  response_text="$(printf '%s' "$input" | jq -r '.response_text // ""' 2>/dev/null)"
else
  # Fallback: conservative allow on parse failure.
  touched_files=""
  response_text=""
fi

# If no touched files, nothing to guard.
if [ -z "$touched_files" ]; then
  exit 0
fi

# Check if any touched file is in a guarded domain.
guarded=0
while IFS= read -r f; do
  case "$f" in
    Assets/Scripts/*.cs|Assets/Scripts/**/*.cs|\
    */Assets/Scripts/*.cs|*/Assets/Scripts/**/*.cs|\
    tools/mcp-ia-server/*|*/tools/mcp-ia-server/*|\
    Domains/*|*/Domains/*)
      guarded=1
      break
      ;;
  esac
done <<EOF
$touched_files
EOF

if [ "$guarded" -eq 0 ]; then
  exit 0
fi

# Check for Verification block header in response_text.
# Accept: response contains ```json (code fence) AND "verification" key anywhere.
# grep processes line-by-line so we check each condition independently — both
# must be present. This handles cases where jq decodes \n to real newlines
# causing the backtick line and the JSON line to be on separate grep lines.
has_verification=0
if printf '%s' "$response_text" | grep -q '```json' && \
   printf '%s' "$response_text" | grep -q '"verification"'; then
  has_verification=1
fi

if [ "$has_verification" -eq 1 ]; then
  exit 0
fi

cat >&2 <<'EOF'
[territory-developer · stop-verification-required] Verification block required.

  One or more touched files are in a guarded domain (Assets/Scripts/*.cs,
  tools/mcp-ia-server/*, Domains/*) but the response does not contain a
  Verification block JSON header.

  Fix: include a verification block in your response before stopping:

    ```json
    {"verification": {"path": "...", "rows": [...]}}
    ```

  Override (emergency): set SKIP_STOP_VERIFICATION=1 in the environment.
EOF
exit 2
