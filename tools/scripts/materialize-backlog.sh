#!/usr/bin/env bash
# materialize-backlog.sh
#
# Regenerate BACKLOG.md + BACKLOG-ARCHIVE.md.
#
# Default source (post-Step-5 of ia-dev-db-refactor): IA Postgres DB
#   (ia_tasks.raw_markdown) via tools/scripts/materialize-backlog-from-db.mjs.
# Legacy source (pre-Step-5 / fallback): ia/backlog/*.yaml +
#   ia/backlog-archive/*.yaml via tools/scripts/materialize-backlog.mjs.
# Ordering manifests (shared by both paths):
#   ia/state/backlog-sections.json
#   ia/state/backlog-archive-sections.json
#
# Round-trip safe: diff vs pre-migration files must be whitespace-only.
#
# Usage:
#   bash tools/scripts/materialize-backlog.sh [--check]
#
# Source selection (env):
#   MATERIALIZE_BACKLOG_SOURCE=db        (default — DB-sourced)
#   MATERIALIZE_BACKLOG_SOURCE=yaml      (legacy yaml path)
#
# --check: exit non-zero if generated output differs from on-disk files
#          (whitespace-normalized diff).
#
# CHECK_MODE env var: set to "1" to enable check mode (same as --check).
# CHECK_MODE=0 (default) always runs in write mode regardless of string content.
# NOTE: do NOT use ${CHECK_MODE:+--check} — bash :+ expands on any non-empty
#       string, so the default "0" would incorrectly enable check mode.

set -euo pipefail

# macOS: Homebrew util-linux flock may not be on $PATH in minimal agent shells.
# Prepend well-known location so `flock` resolves without manual PATH export.
if [[ "$(uname)" == "Darwin" ]]; then
  export PATH="/opt/homebrew/opt/util-linux/bin:${PATH}"
fi

# Ensure flock is available (matches reserve-id.sh contract).
if ! command -v flock >/dev/null 2>&1; then
  echo "ERROR: flock not found. Install via: brew install util-linux" >&2
  echo "  (macOS ships without flock; util-linux provides it)" >&2
  exit 1
fi

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CHECK_MODE="${CHECK_MODE:-0}"
for arg in "$@"; do
  [[ "$arg" == "--check" ]] && CHECK_MODE=1
done

LOCK_FILE="${REPO_ROOT}/ia/state/.materialize-backlog.lock"

# Build extra args: only pass --check when CHECK_MODE equals exactly "1".
EXTRA_ARGS=()
[ "$CHECK_MODE" = "1" ] && EXTRA_ARGS+=(--check)

SOURCE="${MATERIALIZE_BACKLOG_SOURCE:-db}"
case "$SOURCE" in
  db)    DELEGATE="${REPO_ROOT}/tools/postgres-ia/materialize-backlog-from-db.mjs" ;;
  yaml)  DELEGATE="${REPO_ROOT}/tools/scripts/materialize-backlog.mjs" ;;
  *)
    echo "ERROR: unknown MATERIALIZE_BACKLOG_SOURCE='${SOURCE}' (expected 'db' or 'yaml')" >&2
    exit 2
    ;;
esac

# Acquire exclusive lock to prevent concurrent materialization races.
# Mirror the reserve-id.sh pattern: flock fd 9, then delegate.
(
  flock -x 9
  # Delegate to Node (DB-sourced by default; yaml fallback via
  # MATERIALIZE_BACKLOG_SOURCE=yaml). Both honour --check for diff mode.
  node "${DELEGATE}" ${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"} "$@"
) 9>"${LOCK_FILE}"
