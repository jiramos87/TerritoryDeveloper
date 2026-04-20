#!/usr/bin/env bash
# session-hook.sh (TECH-536 — Stage 1.3 B7-extended telemetry harness)
#
# PostToolUse hook — appends per-tool-call JSONL row to
# .claude/telemetry/{session-id}.jsonl for downstream aggregation
# (TECH-537 aggregate-session.sh) + post-stage sweep (TECH-538).
#
# Hook contract (Claude Code PostToolUse):
#   Reads JSON from stdin with fields including:
#     - session_id    (string)
#     - tool_name     (string)
#     - tool_duration_ms (number, optional)
#     - tool_use_id   (string, optional)
#   Optional env fallback: CLAUDE_SESSION_ID, CLAUDE_TOOL_NAME,
#     CLAUDE_TOOL_DURATION_MS, CLAUDE_AGENT_SLUG, CLAUDE_LIFECYCLE_STAGE.
#
# Output row fields (per-tool-call, distinct from per-session aggregate rows
# produced by baseline-collect.sh; validate:telemetry-schema tolerates by
# whitelist — per-tool-call rows carry `kind: "tool-call"`):
#   { "kind": "tool-call", "ts", "session_id", "tool", "duration_ms",
#     "agent", "lifecycle_stage" }
#
# Guarantees:
#   - Non-blocking: exit 0 always, including on jq absence / parse errors /
#     missing fields. Telemetry is observability, never a gate.
#   - No stdout / stderr on happy path (hooks run on every tool call).
#   - Creates .claude/telemetry/ dir on demand.
#
# Schema versioning note: TECH-510 baseline JSONL rows are per-session
# aggregate (8 numeric metric fields). These per-tool-call rows carry a
# discriminator `kind: "tool-call"` so validate:telemetry-schema can skip
# them (see follow-up in TECH-537 for validator tolerance update).

set +e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
TELEMETRY_DIR="${REPO_ROOT}/.claude/telemetry"
mkdir -p "${TELEMETRY_DIR}" 2>/dev/null

STDIN_JSON=""
if [ ! -t 0 ]; then
  STDIN_JSON="$(cat 2>/dev/null || true)"
fi

SESSION_ID=""
TOOL_NAME=""
DURATION_MS="0"
AGENT_SLUG="${CLAUDE_AGENT_SLUG:-}"
LIFECYCLE_STAGE="${CLAUDE_LIFECYCLE_STAGE:-}"

if command -v jq >/dev/null 2>&1 && [ -n "${STDIN_JSON}" ]; then
  SESSION_ID="$(printf '%s' "${STDIN_JSON}" | jq -r '.session_id // ""' 2>/dev/null)"
  TOOL_NAME="$(printf '%s' "${STDIN_JSON}" | jq -r '.tool_name // ""' 2>/dev/null)"
  DURATION_MS="$(printf '%s' "${STDIN_JSON}" | jq -r '.tool_duration_ms // 0' 2>/dev/null)"
fi

# Env fallbacks
[ -z "${SESSION_ID}" ] && SESSION_ID="${CLAUDE_SESSION_ID:-}"
[ -z "${TOOL_NAME}" ] && TOOL_NAME="${CLAUDE_TOOL_NAME:-}"
[ -z "${DURATION_MS}" ] && DURATION_MS="${CLAUDE_TOOL_DURATION_MS:-0}"

# Last-resort session id
[ -z "${SESSION_ID}" ] && SESSION_ID="unknown-session"

# Epoch ms UTC — match baseline-collect.sh: BSD/macOS date lacks real %3N and
# can emit a literal trailing "N", which breaks JSON numbers. Also avoid the
# old guard that compared two separate `date +%s` calls (second-boundary race
# could keep malformed TS).
TS="$(date -u +%s%3N 2>/dev/null)"
if [[ -z "$TS" || "$TS" == *N* ]]; then
  TS="$(python3 -c 'import time; print(int(time.time()*1000))' 2>/dev/null \
    || perl -MTime::HiRes=time -e 'printf("%d\n", time()*1000)' 2>/dev/null \
    || echo $(( $(date -u +%s) * 1000 )))"
fi
TS="${TS//[^0-9]/}"

OUT_FILE="${TELEMETRY_DIR}/${SESSION_ID}.jsonl"

# Escape strings (minimal — hook runs on every tool call, avoid jq cost).
esc() { printf '%s' "${1}" | sed 's/\\/\\\\/g; s/"/\\"/g'; }

printf '{"kind":"tool-call","ts":%s,"session_id":"%s","tool":"%s","duration_ms":%s,"agent":"%s","lifecycle_stage":"%s"}\n' \
  "${TS}" \
  "$(esc "${SESSION_ID}")" \
  "$(esc "${TOOL_NAME}")" \
  "${DURATION_MS:-0}" \
  "$(esc "${AGENT_SLUG}")" \
  "$(esc "${LIFECYCLE_STAGE}")" \
  >> "${OUT_FILE}" 2>/dev/null

exit 0
