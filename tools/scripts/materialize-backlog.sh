#!/usr/bin/env bash
# materialize-backlog.sh
#
# Regenerate BACKLOG.md + BACKLOG-ARCHIVE.md from:
#   ia/backlog/*.yaml + ia/backlog-archive/*.yaml  (issue records)
#   ia/state/backlog-sections.json                 (section order + prose)
#   ia/state/backlog-archive-sections.json
#
# Round-trip safe: diff vs pre-migration files must be whitespace-only.
#
# Usage:
#   bash tools/scripts/materialize-backlog.sh [--check]
#
# --check: exit non-zero if generated output differs from on-disk files
#          (whitespace-normalized diff).
#
# CHECK_MODE env var: set to "1" to enable check mode (same as --check).
# CHECK_MODE=0 (default) always runs in write mode regardless of string content.
# NOTE: do NOT use ${CHECK_MODE:+--check} — bash :+ expands on any non-empty
#       string, so the default "0" would incorrectly enable check mode.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CHECK_MODE="${CHECK_MODE:-0}"
for arg in "$@"; do
  [[ "$arg" == "--check" ]] && CHECK_MODE=1
done

LOCK_FILE="${REPO_ROOT}/ia/state/.materialize-backlog.lock"

# Build extra args: only pass --check when CHECK_MODE equals exactly "1".
EXTRA_ARGS=()
[ "$CHECK_MODE" = "1" ] && EXTRA_ARGS+=(--check)

# Acquire exclusive lock to prevent concurrent materialization races.
# Mirror the reserve-id.sh pattern: flock fd 9, then delegate.
(
  flock -x 9
  # Delegate to Node (handles yaml parsing + section manifest assembly)
  node "${REPO_ROOT}/tools/scripts/materialize-backlog.mjs" ${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"} "$@"
) 9>"${LOCK_FILE}"
