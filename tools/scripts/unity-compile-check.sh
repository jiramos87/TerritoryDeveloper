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

# TECH-30633: --cold flag wipes Library/ScriptAssemblies/ + Library/Bee/ before invoking
# Editor batch. Defeats stale-DLL false-greens (warm compile reuses cached assemblies and
# can silently pass even when asmdef graph is broken). Used by ship-cycle Pass B when
# .asmdef diff detected. Without --cold, behavior is unchanged (warm path preserved).
COLD_START="0"
COLD_DRY_RUN="0"
for arg in "$@"; do
  case "$arg" in
    --cold) COLD_START="1" ;;
    --cold-dry-run) COLD_DRY_RUN="1" ;;
    *) echo "unity-compile-check: unknown flag: $arg" >&2; exit 2 ;;
  esac
done

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

if [[ "$COLD_START" == "1" ]]; then
  echo "unity-compile-check: cold start — wiping Library/ScriptAssemblies + Library/Bee" >&2
  rm -rf "$REPO_ROOT/Library/ScriptAssemblies" "$REPO_ROOT/Library/Bee"
fi

if [[ "$COLD_DRY_RUN" == "1" ]]; then
  # Flag-parse sanity gate (TECH-30633 plan-digest Gate 3) — pre-empt the warm path,
  # echo banner, exit clean. No Unity invocation.
  echo "unity-compile-check: cold-dry-run — flag parser OK (would wipe ScriptAssemblies/+Bee/)" >&2
  exit 0
fi

territory_preserve_editor_layout
echo "unity-compile-check: project=$REPO_ROOT log=$LOG_FILE" >&2
"$UNITY_BIN" -batchmode -nographics -quit -projectPath "$REPO_ROOT" -logFile "$LOG_FILE"
echo "unity-compile-check: success log=$LOG_FILE" >&2
