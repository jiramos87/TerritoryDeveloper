#!/usr/bin/env bash
# Chained post-implementation verification (default local closed loop).
# When invoked with --skip-node-checks (e.g. from verify-local.sh), step 1 is omitted — Node checks already ran.
#   1) npm run validate:all  (unless --skip-node-checks)
#   2) macOS: if this project is open in Unity Editor, Cmd+S then Cmd+Q; wait up to 30s for lock to clear
#   3) npm run unity:compile-check (batchmode — requires no Editor lock)
#   4) npm run db:migrate
#   5) npm run db:bridge-preflight
#   6) macOS: if Editor holds the project again before launch, save+quit + wait up to 30s
#   7) macOS: open Unity Editor on REPO_ROOT; wait up to 60s for Temp/UnityLockfile
#   8) npm run db:bridge-playmode-smoke (pass-through args, e.g. seed cell)
# Non-macOS: runs steps 1,3–5 only; prints how to run bridge smoke manually.
set -euo pipefail

SKIP_NODE_CHECKS=0
PASSTHROUGH=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-node-checks) SKIP_NODE_CHECKS=1; shift ;;
    *) PASSTHROUGH+=("$1"); shift ;;
  esac
done
set -- "${PASSTHROUGH[@]}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
cd "${REPO_ROOT}"
territory_load_repo_dotenv_files "${REPO_ROOT}"

LOCK_FILE="${REPO_ROOT}/Temp/UnityLockfile"

territory_resolve_unity_bin() {
  UNITY_BIN="${UNITY_EDITOR_PATH:-}"
  if [[ -z "${UNITY_BIN}" && "$(uname -s)" == "Darwin" ]]; then
    local _uv _cand
    _uv="$(grep -E '^m_EditorVersion:' "${REPO_ROOT}/ProjectSettings/ProjectVersion.txt" 2>/dev/null | head -1 | awk '{print $2}')"
    if [[ -n "${_uv}" ]]; then
      _cand="/Applications/Unity/Hub/Editor/${_uv}/Unity.app/Contents/MacOS/Unity"
      if [[ -x "${_cand}" ]]; then
        UNITY_BIN="${_cand}"
      fi
    fi
  fi
  if [[ -z "${UNITY_BIN}" ]]; then
    echo "post-implementation-verify: set UNITY_EDITOR_PATH in .env (see ProjectSettings/ProjectVersion.txt)." >&2
    return 1
  fi
  if [[ ! -x "${UNITY_BIN}" ]] && [[ ! -f "${UNITY_BIN}" ]]; then
    echo "post-implementation-verify: invalid UNITY_EDITOR_PATH: ${UNITY_BIN}" >&2
    return 1
  fi
  # Unity.app bundle path (for open -a): .../Unity.app/Contents/MacOS/Unity -> .../Unity.app
  UNITY_APP="$(cd "$(dirname "${UNITY_BIN}")/../.." && pwd)"
  return 0
}

territory_project_lock_present() {
  [[ -f "${LOCK_FILE}" ]]
}

# Best-effort: frontmost Unity, save, quit. Requires macOS Accessibility for System Events (may prompt once).
territory_unity_save_and_quit_applescript() {
  if [[ "$(uname -s)" != "Darwin" ]]; then
    return 0
  fi
  osascript <<'EOF' 2>/dev/null || true
tell application "System Events"
  if (exists process "Unity") then
    tell process "Unity"
      set frontmost to true
    end tell
    delay 0.4
    keystroke "s" using command down
    delay 0.6
    keystroke "q" using command down
  end if
end tell
EOF
}

# Wait until Unity releases the project lock (or timeout seconds).
territory_wait_lock_cleared() {
  local max_seconds="$1"
  local waited=0
  echo "post-implementation-verify: waiting up to ${max_seconds}s for Editor to release project lock..." >&2
  while (( waited < max_seconds )); do
    if ! territory_project_lock_present; then
      echo "post-implementation-verify: project lock cleared after ${waited}s." >&2
      return 0
    fi
    sleep 1
    waited=$((waited + 1))
  done
  echo "post-implementation-verify: timeout — ${LOCK_FILE} still present after ${max_seconds}s. Close Unity manually, or remove a stale lock after a crash, then re-run." >&2
  return 1
}

# Wait until Unity creates the project lock (Editor finished opening project), or timeout seconds.
territory_wait_lock_present() {
  local max_seconds="$1"
  local waited=0
  echo "post-implementation-verify: waiting up to ${max_seconds}s for Unity Editor to open project..." >&2
  while (( waited < max_seconds )); do
    if territory_project_lock_present; then
      echo "post-implementation-verify: project lock present after ${waited}s (Editor likely ready)." >&2
      sleep 5
      return 0
    fi
    sleep 1
    waited=$((waited + 1))
  done
  echo "post-implementation-verify: timeout — no ${LOCK_FILE} after ${max_seconds}s. Is Unity starting? Re-run smoke after Editor loads." >&2
  return 1
}

territory_if_locked_save_quit_and_wait() {
  if [[ "$(uname -s)" != "Darwin" ]]; then
    return 0
  fi
  if ! territory_project_lock_present; then
    return 0
  fi
  echo "post-implementation-verify: Unity has this project open — saving and quitting Editor before batch compile / relaunch." >&2
  territory_unity_save_and_quit_applescript
  sleep 2
  territory_wait_lock_cleared 30
}

territory_launch_unity_editor() {
  if [[ "$(uname -s)" != "Darwin" ]]; then
    return 1
  fi
  echo "post-implementation-verify: opening Unity Editor: ${UNITY_APP} -projectPath ${REPO_ROOT}" >&2
  open -a "${UNITY_APP}" --args -projectPath "${REPO_ROOT}"
}

run_step() {
  echo "post-implementation-verify: === $* ===" >&2
  "$@"
}

if [[ "${SKIP_NODE_CHECKS}" -eq 0 ]]; then
  run_step npm run validate:all
else
  echo "post-implementation-verify: --skip-node-checks — skipping npm run validate:all (already run via verify:local)." >&2
fi

if [[ "$(uname -s)" == "Darwin" ]]; then
  territory_if_locked_save_quit_and_wait
else
  echo "post-implementation-verify: non-macOS — skipping Editor save/quit orchestration." >&2
fi

run_step npm run unity:compile-check
run_step npm run db:migrate
run_step npm run db:bridge-preflight

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "post-implementation-verify: done (Node + batch compile + DB). On macOS, full script also runs db:bridge-playmode-smoke after opening Unity." >&2
  echo "post-implementation-verify: run Unity Editor on ${REPO_ROOT}, then: npm run db:bridge-playmode-smoke -- \"\$seed_cell\"" >&2
  exit 0
fi

territory_resolve_unity_bin || exit 2

# Before opening Editor for bridge: if something holds the project, save+quit again (max 30s clear).
territory_if_locked_save_quit_and_wait

territory_launch_unity_editor
territory_wait_lock_present 60

run_step npm run db:bridge-playmode-smoke -- "$@"

echo "post-implementation-verify: all steps completed OK." >&2
