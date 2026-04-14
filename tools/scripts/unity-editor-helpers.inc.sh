# Territory — shared Unity Editor helpers for repo-root scripts.
# Usage: source "${SCRIPT_DIR}/unity-editor-helpers.inc.sh"
# Requires: $REPO_ROOT and $LOCK_FILE set by the sourcing script before calling functions.
# Optional: $_TERRITORY_LOG_PREFIX (default "territory") for log lines.
[[ -n "${_TERRITORY_EDITOR_HELPERS_LOADED:-}" ]] && return 0
_TERRITORY_EDITOR_HELPERS_LOADED=1

# Resolve UNITY_BIN (binary) and UNITY_APP (.app bundle) from UNITY_EDITOR_PATH or macOS Hub path.
territory_resolve_unity_bin() {
  local _prefix="${_TERRITORY_LOG_PREFIX:-territory}"
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
    echo "${_prefix}: set UNITY_EDITOR_PATH in .env (see ProjectSettings/ProjectVersion.txt)." >&2
    return 1
  fi
  if [[ ! -x "${UNITY_BIN}" ]] && [[ ! -f "${UNITY_BIN}" ]]; then
    echo "${_prefix}: invalid UNITY_EDITOR_PATH: ${UNITY_BIN}" >&2
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
  local _prefix="${_TERRITORY_LOG_PREFIX:-territory}"
  local max_seconds="$1"
  local waited=0
  echo "${_prefix}: waiting up to ${max_seconds}s for Editor to release project lock..." >&2
  while (( waited < max_seconds )); do
    if ! territory_project_lock_present; then
      echo "${_prefix}: project lock cleared after ${waited}s." >&2
      return 0
    fi
    sleep 1
    waited=$((waited + 1))
  done
  echo "${_prefix}: timeout — ${LOCK_FILE} still present after ${max_seconds}s. Close Unity manually, or remove a stale lock after a crash, then re-run." >&2
  return 1
}

# Wait until Unity creates the project lock (Editor finished opening project), or timeout seconds.
# $1 = max_seconds, $2 = grace_seconds after lockfile appears (default 5).
territory_wait_lock_present() {
  local _prefix="${_TERRITORY_LOG_PREFIX:-territory}"
  local max_seconds="$1"
  local grace_seconds="${2:-5}"
  local waited=0
  echo "${_prefix}: waiting up to ${max_seconds}s for Unity Editor to open project..." >&2
  while (( waited < max_seconds )); do
    if territory_project_lock_present; then
      echo "${_prefix}: project lock present after ${waited}s (Editor likely ready)." >&2
      sleep "${grace_seconds}"
      return 0
    fi
    sleep 1
    waited=$((waited + 1))
  done
  echo "${_prefix}: timeout — no ${LOCK_FILE} after ${max_seconds}s. Is Unity starting? Re-run smoke after Editor loads." >&2
  return 1
}

territory_if_locked_save_quit_and_wait() {
  local _prefix="${_TERRITORY_LOG_PREFIX:-territory}"
  if [[ "$(uname -s)" != "Darwin" ]]; then
    return 0
  fi
  if ! territory_project_lock_present; then
    return 0
  fi
  echo "${_prefix}: Unity has this project open — saving and quitting Editor before batch compile / relaunch." >&2
  territory_unity_save_and_quit_applescript
  sleep 2
  territory_wait_lock_cleared 30
}

# Preserve UserSettings/Layouts/default-2022.dwlt across a -batchmode Unity run.
# Call once per script before invoking Unity. Restores on any exit (success, error, or signal).
territory_preserve_editor_layout() {
  local _layout_file="${REPO_ROOT}/UserSettings/Layouts/default-2022.dwlt"
  local _backup="${REPO_ROOT}/UserSettings/Layouts/.default-2022.dwlt.batchbak"
  if [[ ! -f "${_layout_file}" ]]; then return 0; fi
  cp "${_layout_file}" "${_backup}"
  # SC2064: intentional — expand paths now so the trap string captures concrete values.
  # shellcheck disable=SC2064
  trap "cp -f '${_backup}' '${_layout_file}' 2>/dev/null; rm -f '${_backup}'" EXIT
}

territory_launch_unity_editor() {
  local _prefix="${_TERRITORY_LOG_PREFIX:-territory}"
  if [[ "$(uname -s)" != "Darwin" ]]; then
    return 1
  fi
  echo "${_prefix}: opening Unity Editor: ${UNITY_APP} -projectPath ${REPO_ROOT}" >&2
  open -a "${UNITY_APP}" --args -projectPath "${REPO_ROOT}"
}
