#!/usr/bin/env bash
# Local compile smoke: Unity -batchmode import + script compile, then quit.
# UNITY_EDITOR_PATH: repo .env / .env.local (sourced below), else macOS Unity Hub path derived from ProjectSettings/ProjectVersion.txt.
# AI agents: do not skip running this script because $UNITY_EDITOR_PATH is unset in the parent shell — dotenv is loaded here.
# Do not run while another Unity Editor instance has this project open (project lock).
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
territory_load_repo_dotenv_files "$REPO_ROOT"
# shellcheck source=unity-editor-helpers.inc.sh
source "${SCRIPT_DIR}/unity-editor-helpers.inc.sh"

UNITY_BIN="${UNITY_EDITOR_PATH:-}"
if [[ -z "$UNITY_BIN" && "$(uname -s)" == "Darwin" ]]; then
  _unity_ver="$(grep -E '^m_EditorVersion:' "${REPO_ROOT}/ProjectSettings/ProjectVersion.txt" 2>/dev/null | head -1 | awk '{print $2}')"
  if [[ -n "$_unity_ver" ]]; then
    _unity_cand="/Applications/Unity/Hub/Editor/${_unity_ver}/Unity.app/Contents/MacOS/Unity"
    if [[ -x "$_unity_cand" ]]; then
      UNITY_BIN="$_unity_cand"
    fi
  fi
  unset _unity_ver _unity_cand
fi
if [[ -z "$UNITY_BIN" ]]; then
  echo "unity-compile-check: set UNITY_EDITOR_PATH in .env or export it (see ProjectSettings/ProjectVersion.txt)." >&2
  exit 2
fi
if [[ ! -x "$UNITY_BIN" ]] && [[ ! -f "$UNITY_BIN" ]]; then
  echo "unity-compile-check: UNITY_EDITOR_PATH is not a valid Unity binary: $UNITY_BIN" >&2
  exit 2
fi
LOG_DIR="$REPO_ROOT/tools/reports"
mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/unity-compile-check-$(date -u +%Y%m%d-%H%M%S).log"
territory_preserve_editor_layout
echo "unity-compile-check: project=$REPO_ROOT log=$LOG_FILE" >&2
"$UNITY_BIN" -batchmode -nographics -quit -projectPath "$REPO_ROOT" -logFile "$LOG_FILE"
echo "unity-compile-check: success log=$LOG_FILE" >&2
