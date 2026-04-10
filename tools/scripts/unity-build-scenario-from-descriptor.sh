#!/usr/bin/env bash
# Unity -batchmode: apply scenario_descriptor_v1 then write GameSaveData JSON.
# Sources repo .env / .env.local for UNITY_EDITOR_PATH (same as unity-testmode-batch.sh).
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
territory_load_repo_dotenv_files "$REPO_ROOT"

usage() {
  cat <<EOF
Usage: unity-build-scenario-from-descriptor.sh --descriptor PATH --output PATH [options] [-- extra Unity args]

Runs Unity in -batchmode -nographics with -executeMethod Territory.Testing.ScenarioDescriptorBatchBuilder.Run
(no -quit: C# calls EditorApplication.Exit when finished).

Required:
  --descriptor PATH   Absolute or repo-relative path to scenario_descriptor_v1 JSON
  --output PATH       Absolute or repo-relative path for generated save.json

Optional:
  --base-save PATH    Base GameSaveData JSON to load first (default: reference-flat-32x32)
  --quit-editor-first -> run tools/scripts/unity-quit-project.sh before launch
  --wait-quit-seconds N  (default: 45)
  -h, --help

Exit codes: same family as unity-testmode-batch.sh (4 bad args / missing files, 6 apply failure, 7 play stop timeout).
EOF
}

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
  echo "unity-build-scenario-from-descriptor: set UNITY_EDITOR_PATH in .env or export it." >&2
  exit 2
fi

DESCR=""
OUT=""
BASE_SAVE=""
QUIT_FIRST=false
WAIT_QUIT_SECS=45
EXTRA=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    --descriptor)
      DESCR="$2"
      shift 2
      ;;
    --output)
      OUT="$2"
      shift 2
      ;;
    --base-save)
      BASE_SAVE="$2"
      shift 2
      ;;
    --quit-editor-first)
      QUIT_FIRST=true
      shift
      ;;
    --wait-quit-seconds)
      WAIT_QUIT_SECS="$2"
      shift 2
      ;;
    --)
      shift
      EXTRA=("$@")
      break
      ;;
    *)
      echo "unity-build-scenario-from-descriptor: unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ -z "$DESCR" || -z "$OUT" ]]; then
  echo "unity-build-scenario-from-descriptor: --descriptor and --output are required." >&2
  exit 1
fi

if [[ "$DESCR" != /* ]]; then
  DESCR="${REPO_ROOT}/${DESCR}"
fi
if [[ "$OUT" != /* ]]; then
  OUT="${REPO_ROOT}/${OUT}"
fi
if [[ -n "$BASE_SAVE" && "$BASE_SAVE" != /* ]]; then
  BASE_SAVE="${REPO_ROOT}/${BASE_SAVE}"
fi

if [[ "$QUIT_FIRST" == true ]]; then
  if ! bash "${SCRIPT_DIR}/unity-quit-project.sh" --repo-root "$REPO_ROOT" --wait-seconds "$WAIT_QUIT_SECS"; then
    exit 3
  fi
fi

mkdir -p "${REPO_ROOT}/tools/reports"
LOG_FILE="${REPO_ROOT}/tools/reports/unity-build-scenario-$(date -u +%Y%m%d-%H%M%S).log"

UNITY_ARGS=(
  -batchmode
  -nographics
  -projectPath "$REPO_ROOT"
  -logFile "$LOG_FILE"
  -executeMethod Territory.Testing.ScenarioDescriptorBatchBuilder.Run
  -scenarioDescriptorPath "$DESCR"
  -outputScenarioSavePath "$OUT"
)

if [[ -n "$BASE_SAVE" ]]; then
  UNITY_ARGS+=(-baseScenarioSavePath "$BASE_SAVE")
fi

if [[ ${#EXTRA[@]} -gt 0 ]]; then
  UNITY_ARGS+=("${EXTRA[@]}")
fi

echo "unity-build-scenario-from-descriptor: log=$LOG_FILE" >&2
set +e
"$UNITY_BIN" "${UNITY_ARGS[@]}"
_rc=$?
set -e
echo "unity-build-scenario-from-descriptor: Unity exit=$_rc log=$LOG_FILE" >&2
exit "$_rc"
