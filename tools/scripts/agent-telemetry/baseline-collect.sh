#!/usr/bin/env bash
# baseline-collect.sh — append one JSONL telemetry row per invocation.
#
# Schema (8 fields):
#   ts                 — epoch ms UTC
#   session_id         — CLAUDE_SESSION_ID env or fallback pid-stamped id
#   total_input_tokens — TOTAL_INPUT_TOKENS env (integer, 0 if absent)
#   cache_read_tokens  — CACHE_READ_TOKENS env (integer, 0 if absent)
#   cache_write_tokens — CACHE_WRITE_TOKENS env (integer, 0 if absent)
#   mcp_cold_start_ms  — MCP_COLD_START_MS env (integer, 0 if absent)
#   hook_fork_count    — HOOK_FORK_COUNT env (integer, 0 if absent)
#   hook_fork_total_ms — HOOK_FORK_TOTAL_MS env (integer, 0 if absent)
#
# Missing env vars → zero values. Always exits 0.
# Append is O_APPEND atomic on POSIX for lines <4KB (row is well under that).

set -euo pipefail

# --- helpers ---------------------------------------------------------------

# Coerce empty/absent env var to integer 0.
int_or_zero() {
  local v="${1:-0}"
  # Strip non-numeric chars to guard against stray strings.
  v="${v//[^0-9]/}"
  printf '%s' "${v:-0}"
}

# --- timestamp --------------------------------------------------------------
# Epoch ms UTC. Cross-platform: GNU date supports %3N directly; BSD date
# (macOS) lacks %3N, so fall back to python/perl, else seconds*1000.
TS="$(date -u +%s%3N 2>/dev/null)"
if [[ -z "$TS" || "$TS" == *N* ]]; then
  # macOS / BSD fallback — python3 gives true ms; final fallback is s*1000.
  TS="$(python3 -c 'import time; print(int(time.time()*1000))' 2>/dev/null \
    || perl -MTime::HiRes=time -e 'printf("%d\n", time()*1000)' 2>/dev/null \
    || echo $(( $(date -u +%s) * 1000 )))"
fi
# Trim to pure digits (guard against stray chars).
TS="${TS//[^0-9]/}"

# --- session id -------------------------------------------------------------
SESSION_ID="${CLAUDE_SESSION_ID:-}"
if [[ -z "$SESSION_ID" ]]; then
  # Fallback: unix seconds + PID to stay unique across parallel sessions.
  SESSION_ID="$(date -u +%s)-$$"
fi

# --- metric fields ----------------------------------------------------------
TOTAL_INPUT_TOKENS="$(int_or_zero "${TOTAL_INPUT_TOKENS:-}")"
CACHE_READ_TOKENS="$(int_or_zero "${CACHE_READ_TOKENS:-}")"
CACHE_WRITE_TOKENS="$(int_or_zero "${CACHE_WRITE_TOKENS:-}")"
MCP_COLD_START_MS="$(int_or_zero "${MCP_COLD_START_MS:-}")"
HOOK_FORK_COUNT="$(int_or_zero "${HOOK_FORK_COUNT:-}")"
HOOK_FORK_TOTAL_MS="$(int_or_zero "${HOOK_FORK_TOTAL_MS:-}")"

# --- output dir + file ------------------------------------------------------
TELEMETRY_DIR="${CLAUDE_TELEMETRY_DIR:-.claude/telemetry}"
mkdir -p "$TELEMETRY_DIR"

OUT_FILE="${TELEMETRY_DIR}/${SESSION_ID}.jsonl"

# --- emit JSONL row ---------------------------------------------------------
# Single compact line; no trailing newline on the object — printf adds \n.
printf '{"ts":%s,"session_id":"%s","total_input_tokens":%s,"cache_read_tokens":%s,"cache_write_tokens":%s,"mcp_cold_start_ms":%s,"hook_fork_count":%s,"hook_fork_total_ms":%s}\n' \
  "$TS" \
  "$SESSION_ID" \
  "$TOTAL_INPUT_TOKENS" \
  "$CACHE_READ_TOKENS" \
  "$CACHE_WRITE_TOKENS" \
  "$MCP_COLD_START_MS" \
  "$HOOK_FORK_COUNT" \
  "$HOOK_FORK_TOTAL_MS" \
  >> "$OUT_FILE"

exit 0
