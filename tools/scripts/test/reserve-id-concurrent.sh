#!/usr/bin/env bash
# reserve-id-concurrent.sh — smoke test: 8 parallel reserve-id.sh TECH calls.
#
# Assertions:
#   1. 8 distinct ids emitted (no duplicates).
#   2. All ids are in sorted/sequential order (no gaps — each increment by 1).
#   3. Counter file ends at BASELINE + 8.
#
# Usage:
#   bash tools/scripts/test/reserve-id-concurrent.sh

set -euo pipefail

# Ensure flock available
if ! command -v flock >/dev/null 2>&1; then
  # Try homebrew util-linux path
  export PATH="/opt/homebrew/opt/util-linux/bin:/opt/homebrew/opt/util-linux/sbin:${PATH}"
  if ! command -v flock >/dev/null 2>&1; then
    echo "SKIP: flock not available (install: brew install util-linux)" >&2
    exit 0
  fi
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"
RESERVE="${REPO_ROOT}/tools/scripts/reserve-id.sh"
REAL_COUNTER_FILE="${REPO_ROOT}/ia/state/id-counter.json"

# Use a temp copy of the counter so the test does not advance the production counter.
TMPSTATE=$(mktemp -d)
COUNTER_FILE="${TMPSTATE}/id-counter.json"
LOCK_FILE="${TMPSTATE}/.id-counter.lock"
cp "${REAL_COUNTER_FILE}" "${COUNTER_FILE}"
trap "rm -rf ${TMPSTATE}" EXIT

# Override the counter + lock paths for child reserve-id.sh processes.
export IA_COUNTER_FILE="${COUNTER_FILE}"
export IA_COUNTER_LOCK="${LOCK_FILE}"

PREFIX="TECH"
PARALLEL=8

# Read baseline counter value
BASELINE=$(node -e "
  const d = JSON.parse(require('fs').readFileSync('${COUNTER_FILE}','utf8'));
  process.stdout.write(String(d['${PREFIX}']));
")

echo "[reserve-id-concurrent] Baseline ${PREFIX} counter: ${BASELINE}"
echo "[reserve-id-concurrent] Spawning ${PARALLEL} parallel reservations..."

# Collect all reserved ids
TMPDIR_OUT=$(mktemp -d)
trap "rm -rf ${TMPDIR_OUT}" EXIT

pids=()
for i in $(seq 1 ${PARALLEL}); do
  bash "${RESERVE}" "${PREFIX}" > "${TMPDIR_OUT}/${i}.out" &
  pids+=($!)
done

# Wait for all
FAILED=0
for pid in "${pids[@]}"; do
  if ! wait "${pid}"; then
    echo "ERROR: reservation subprocess ${pid} failed" >&2
    FAILED=1
  fi
done

if [[ "${FAILED}" == "1" ]]; then
  echo "FAIL: one or more subprocesses failed" >&2
  exit 1
fi

# Collect all ids
ALL_IDS=$(cat "${TMPDIR_OUT}"/*.out | sort)
COUNT=$(echo "${ALL_IDS}" | wc -l | tr -d ' ')
UNIQUE_COUNT=$(echo "${ALL_IDS}" | sort -u | wc -l | tr -d ' ')

echo "[reserve-id-concurrent] Ids reserved:"
echo "${ALL_IDS}"

# Assert count
if [[ "${COUNT}" != "${PARALLEL}" ]]; then
  echo "FAIL: expected ${PARALLEL} ids, got ${COUNT}" >&2
  exit 1
fi

# Assert uniqueness
if [[ "${COUNT}" != "${UNIQUE_COUNT}" ]]; then
  echo "FAIL: duplicate ids detected (${UNIQUE_COUNT} unique out of ${COUNT})" >&2
  exit 1
fi

# Assert counter incremented by exactly PARALLEL
FINAL=$(node -e "
  const d = JSON.parse(require('fs').readFileSync('${COUNTER_FILE}','utf8'));
  process.stdout.write(String(d['${PREFIX}']));
")
EXPECTED=$((BASELINE + PARALLEL))

if [[ "${FINAL}" != "${EXPECTED}" ]]; then
  echo "FAIL: expected counter=${EXPECTED}, got ${FINAL}" >&2
  exit 1
fi

echo "[reserve-id-concurrent] PASS — ${PARALLEL} distinct ids, counter: ${BASELINE} → ${FINAL}"
exit 0
