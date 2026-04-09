#!/usr/bin/env bash
# Ensure Unity Editor is running on this project. If not, launch it and wait.
# Exit 0 = Editor running (lockfile present).
# Exit 1 = Timeout (lockfile did not appear within MAX_WAIT_SECONDS).
# Exit 2 = Not macOS (cannot launch via open -a).
# Exit 3 = Unity binary not found.
# Usage: bash tools/scripts/ensure-unity-editor.sh [max_wait_seconds] [grace_seconds]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
# shellcheck source=unity-editor-helpers.inc.sh
source "${SCRIPT_DIR}/unity-editor-helpers.inc.sh"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
territory_load_repo_dotenv_files "${REPO_ROOT}"

LOCK_FILE="${REPO_ROOT}/Temp/UnityLockfile"
_TERRITORY_LOG_PREFIX="ensure-unity-editor"

MAX_WAIT_SECONDS="${1:-90}"
GRACE_SECONDS="${2:-8}"

# 1. If lockfile exists, Editor already running.
if territory_project_lock_present; then
  echo "${_TERRITORY_LOG_PREFIX}: Unity Editor already running (lockfile present)." >&2
  exit 0
fi

# 2. Platform check.
if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "${_TERRITORY_LOG_PREFIX}: not macOS — cannot auto-launch Unity Editor. Open Unity on ${REPO_ROOT} manually." >&2
  exit 2
fi

# 3. Resolve Unity binary.
if ! territory_resolve_unity_bin; then
  exit 3
fi

# 4. Launch.
territory_launch_unity_editor

# 5. Wait for lockfile + grace.
territory_wait_lock_present "${MAX_WAIT_SECONDS}" "${GRACE_SECONDS}"
