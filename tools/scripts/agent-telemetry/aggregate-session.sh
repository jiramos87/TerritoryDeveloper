#!/usr/bin/env bash
# aggregate-session.sh (TECH-537 — Stage 1.3 B7-extended aggregation)
#
# Reads per-tool-call JSONL for one session (produced by TECH-536 session-hook.sh)
# and emits per-tool p50/p99 duration + call counts as JSON on stdout.
#
# Usage:
#   tools/scripts/agent-telemetry/aggregate-session.sh <session-id>
#   tools/scripts/agent-telemetry/aggregate-session.sh <session-id> --out path.json
#
# Input: .claude/telemetry/<session-id>.jsonl — lines with shape
#   {"kind":"tool-call","ts":…,"session_id":…,"tool":…,"duration_ms":…,…}
#
# Output (stdout or --out file):
#   {
#     "schema_version": "1.0.0",
#     "session_id": "<id>",
#     "generated_at": "<iso-8601>",
#     "tool_call_count": <int>,
#     "tools": {
#       "<tool>": {
#         "calls": <int>,
#         "duration_ms": { "p50": <n>, "p95": <n>, "p99": <n> }
#       }, …
#     },
#     "totals": { "duration_ms_sum": <n> }
#   }
#
# Exits 0 on success; 1 on missing session file / arg; 2 on jq absence.

set -euo pipefail

if ! command -v jq >/dev/null 2>&1; then
  echo "aggregate-session: jq required on PATH" >&2
  exit 2
fi

if [ $# -lt 1 ]; then
  echo "usage: aggregate-session.sh <session-id> [--out path]" >&2
  exit 1
fi

SESSION_ID="$1"; shift
OUT=""
while [ $# -gt 0 ]; do
  case "$1" in
    --out) OUT="$2"; shift 2 ;;
    *) echo "unknown arg: $1" >&2; exit 1 ;;
  esac
done

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
JSONL="${REPO_ROOT}/.claude/telemetry/${SESSION_ID}.jsonl"

if [ ! -f "${JSONL}" ]; then
  echo "aggregate-session: missing ${JSONL}" >&2
  exit 1
fi

GENERATED_AT="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

# jq-driven aggregation. Percentiles computed via nearest-rank method on
# sorted per-tool duration arrays.
REPORT="$(jq -s --arg sid "${SESSION_ID}" --arg ts "${GENERATED_AT}" '
  # Filter per-tool-call rows only
  map(select(.kind == "tool-call"))
  | . as $rows
  | ($rows | length) as $n
  | ($rows | group_by(.tool)) as $g
  | {
      schema_version: "1.0.0",
      session_id: $sid,
      generated_at: $ts,
      tool_call_count: $n,
      tools: (
        $g
        | map(
            . as $calls
            | ($calls | map(.duration_ms)) | sort as $ds
            | ($ds | length) as $c
            | {
                ($calls[0].tool): {
                  calls: $c,
                  duration_ms: {
                    p50: ( $ds[ (($c - 1) * 0.50 | floor) ] // 0 ),
                    p95: ( $ds[ (($c - 1) * 0.95 | floor) ] // 0 ),
                    p99: ( $ds[ (($c - 1) * 0.99 | floor) ] // 0 )
                  }
                }
              }
          )
        | add // {}
      ),
      totals: {
        duration_ms_sum: ( $rows | map(.duration_ms) | add // 0 )
      }
    }
' "${JSONL}")"

if [ -n "${OUT}" ]; then
  printf '%s\n' "${REPORT}" > "${OUT}"
else
  printf '%s\n' "${REPORT}"
fi
