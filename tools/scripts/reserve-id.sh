#!/usr/bin/env bash
# reserve-id.sh — atomically reserve the next id for a given prefix.
#
# Usage:
#   ./tools/scripts/reserve-id.sh TECH        → prints "TECH-290"
#   ./tools/scripts/reserve-id.sh FEAT        → prints "FEAT-54"
#   ./tools/scripts/reserve-id.sh BUG N       → prints N ids, one per line (batch)
#
# Requires: flock (Linux util-linux; macOS: brew install util-linux).
# On macOS without flock → hard error with install hint (no degraded mode).
#
# id-counter.json schema: { "TECH": 289, "FEAT": 53, "BUG": 56, "ART": 4, "AUDIO": 1 }
# Counter holds the LAST RESERVED id. Next id = counter + 1.

set -euo pipefail

# macOS: Homebrew util-linux flock may not be on $PATH in minimal agent shells.
# Prepend well-known location so `command -v flock` resolves without manual PATH export.
if [[ "$(uname)" == "Darwin" ]]; then
  export PATH="/opt/homebrew/opt/util-linux/bin:${PATH}"
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# Allow test overrides via env vars (e.g. reserve-id-concurrent.sh uses temp copies).
COUNTER_FILE="${IA_COUNTER_FILE:-${REPO_ROOT}/ia/state/id-counter.json}"
LOCK_FILE="${IA_COUNTER_LOCK:-${REPO_ROOT}/ia/state/.id-counter.lock}"

PREFIX="${1:-}"
COUNT="${2:-1}"

if [[ -z "${PREFIX}" ]]; then
  echo "Usage: reserve-id.sh <PREFIX> [count]" >&2
  echo "  PREFIX = TECH | FEAT | BUG | ART | AUDIO" >&2
  exit 1
fi

# Validate prefix
case "${PREFIX}" in
  TECH|FEAT|BUG|ART|AUDIO) ;;
  *)
    echo "ERROR: unknown prefix '${PREFIX}'. Must be one of: TECH FEAT BUG ART AUDIO" >&2
    exit 1
    ;;
esac

# Validate count
if ! [[ "${COUNT}" =~ ^[1-9][0-9]*$ ]]; then
  echo "ERROR: count must be a positive integer, got '${COUNT}'" >&2
  exit 1
fi

# Ensure flock is available
if ! command -v flock >/dev/null 2>&1; then
  echo "ERROR: flock not found. Install via: brew install util-linux" >&2
  echo "  (macOS ships without flock; util-linux provides it)" >&2
  exit 1
fi

# Ensure state dir + counter file exist
mkdir -p "$(dirname "${COUNTER_FILE}")"
if [[ ! -f "${COUNTER_FILE}" ]]; then
  echo '{"TECH":0,"FEAT":0,"BUG":0,"ART":0,"AUDIO":0}' > "${COUNTER_FILE}"
fi

# Reserve N ids atomically under flock
# Lock fd 200 → LOCK_FILE
(
  flock -x 200

  # Read current counter
  CURRENT_JSON=$(cat "${COUNTER_FILE}")
  CURRENT=$(echo "${CURRENT_JSON}" | node -e "
    const d = JSON.parse(require('fs').readFileSync('/dev/stdin','utf8'));
    process.stdout.write(String(d['${PREFIX}'] || 0));
  ")

  # Compute new ids
  NEXT=$((CURRENT + 1))
  LAST=$((CURRENT + COUNT))

  # Emit reserved ids (before writing — safe because lock held)
  for (( i=NEXT; i<=LAST; i++ )); do
    echo "${PREFIX}-${i}"
  done

  # Write updated counter
  node -e "
    const fs = require('fs');
    const d = JSON.parse(fs.readFileSync('${COUNTER_FILE}','utf8'));
    d['${PREFIX}'] = ${LAST};
    fs.writeFileSync('${COUNTER_FILE}', JSON.stringify(d, null, 2) + '\n', 'utf8');
  "

) 200>"${LOCK_FILE}"
