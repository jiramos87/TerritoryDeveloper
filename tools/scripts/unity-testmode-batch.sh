#!/usr/bin/env bash
# Unity -batchmode test-mode load smoke: -executeMethod Territory.Testing.AgentTestModeBatchRunner.Run
# Sources repo .env / .env.local for UNITY_EDITOR_PATH; macOS Hub fallback matches unity-compile-check.sh.
# AI agents: do not skip because $UNITY_EDITOR_PATH is unset — dotenv is loaded here.
# If the Unity Editor already has REPO_ROOT open, pass --quit-editor-first (or quit Editor) before batch — see docs/agent-led-verification-policy.md Path A.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
territory_load_repo_dotenv_files "$REPO_ROOT"

usage() {
  cat <<EOF
Usage: unity-testmode-batch.sh [options] [-- extra Unity args]

Runs Unity in -batchmode -nographics (no -quit: the C# runner calls EditorApplication.Exit when finished;
using -quit here would exit before Play Mode pumps could run).

Scenario (pass at least one; same resolution as TestModeCommandLineBootstrap):
  --scenario-id ID          -> forwarded as -testScenarioId ID
  --scenario-path PATH      -> forwarded as -testScenarioPath PATH (prefer absolute paths)

Options:
  --simulation-ticks N      -> forwarded as -testSimulationTicks N (default: 0, max 10000 in C#)
  --golden-path PATH        -> forwarded as -testGoldenPath PATH (committed JSON; mismatch exits 8 — see scenarios README)
  --new-game                -> forwarded as -testNewGame; skip LoadGame, call NewGame + scripted interstate build instead.
                               Scenario id / path become optional labels only (no save file required).
  --test-seed N             -> forwarded as -testSeed N; pins MapGenerationSeed for deterministic new-game smoke.
                               Only meaningful with --new-game.
  --quit-editor-first       -> run tools/scripts/unity-quit-project.sh before launch
  --wait-quit-seconds N     -> passed to unity-quit-project.sh (default: 45)
  -h, --help                -> this message

Default if neither --scenario-id nor --scenario-path is set (and --new-game not given):
  --scenario-id reference-flat-32x32

Extra arguments after -- are appended to the Unity command line (e.g. more -test* flags).

Exit codes (shell):
  0  Unity exited 0
  1  Bad script args or missing scenario
  2  Unity binary not found / not executable
  3  --quit-editor-first failed (lock still held)
  4  Propagated from batch runner: bad args, missing save, MainScene open failure
  6  Load / simulation failure inside Unity
  7  Timed out waiting for Play Mode to stop (C# runner)
  8  Golden CityStats snapshot mismatch (-testGoldenPath)

Other non-zero: Unity process exit code from EditorApplication.Exit (see tools/reports/agent-testmode-batch-*.json).

Windows/Linux: Unity path must be set via UNITY_EDITOR_PATH; Hub inference is macOS-only today.

macOS lock: Temp/UnityLockfile under REPO_ROOT (see unity-quit-project.sh).
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
  echo "unity-testmode-batch: set UNITY_EDITOR_PATH in .env or export it (see ProjectSettings/ProjectVersion.txt)." >&2
  exit 2
fi
if [[ ! -x "$UNITY_BIN" ]] && [[ ! -f "$UNITY_BIN" ]]; then
  echo "unity-testmode-batch: UNITY_EDITOR_PATH is not a valid Unity binary: $UNITY_BIN" >&2
  exit 2
fi

SCENARIO_ID=""
SCENARIO_PATH=""
SIM_TICKS=""
GOLDEN_PATH=""
NEW_GAME=false
TEST_SEED=""
QUIT_FIRST=false
WAIT_QUIT_SECS=45
EXTRA=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    --scenario-id)
      SCENARIO_ID="$2"
      shift 2
      ;;
    --scenario-path)
      SCENARIO_PATH="$2"
      shift 2
      ;;
    --simulation-ticks)
      SIM_TICKS="$2"
      shift 2
      ;;
    --golden-path)
      GOLDEN_PATH="$2"
      shift 2
      ;;
    --new-game)
      NEW_GAME=true
      shift
      ;;
    --test-seed)
      TEST_SEED="$2"
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
      echo "unity-testmode-batch: unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ -z "$SCENARIO_ID" && -z "$SCENARIO_PATH" && "$NEW_GAME" != true ]]; then
  SCENARIO_ID="reference-flat-32x32"
fi
if [[ -n "$SCENARIO_ID" && -n "$SCENARIO_PATH" ]]; then
  echo "unity-testmode-batch: pass only one of --scenario-id or --scenario-path." >&2
  exit 1
fi

if [[ "$QUIT_FIRST" == true ]]; then
  if ! bash "${SCRIPT_DIR}/unity-quit-project.sh" --repo-root "$REPO_ROOT" --wait-seconds "$WAIT_QUIT_SECS"; then
    exit 3
  fi
fi

mkdir -p "${REPO_ROOT}/tools/reports"
LOG_FILE="${REPO_ROOT}/tools/reports/unity-testmode-batch-$(date -u +%Y%m%d-%H%M%S).log"

UNITY_ARGS=(
  -batchmode
  -nographics
  -projectPath "$REPO_ROOT"
  -logFile "$LOG_FILE"
  -executeMethod Territory.Testing.AgentTestModeBatchRunner.Run
)

if [[ -n "$SCENARIO_PATH" ]]; then
  UNITY_ARGS+=(-testScenarioPath "$SCENARIO_PATH")
else
  UNITY_ARGS+=(-testScenarioId "$SCENARIO_ID")
fi

if [[ -n "$SIM_TICKS" ]]; then
  UNITY_ARGS+=(-testSimulationTicks "$SIM_TICKS")
fi

if [[ -n "$GOLDEN_PATH" ]]; then
  UNITY_ARGS+=(-testGoldenPath "$GOLDEN_PATH")
fi

if [[ "$NEW_GAME" == true ]]; then
  UNITY_ARGS+=(-testNewGame)
fi

if [[ -n "$TEST_SEED" ]]; then
  UNITY_ARGS+=(-testSeed "$TEST_SEED")
fi

if [[ ${#EXTRA[@]} -gt 0 ]]; then
  UNITY_ARGS+=("${EXTRA[@]}")
fi

echo "unity-testmode-batch: project=$REPO_ROOT log=$LOG_FILE" >&2
set +e
"$UNITY_BIN" "${UNITY_ARGS[@]}"
_rc=$?
set -e
echo "unity-testmode-batch: Unity exit=$_rc log=$LOG_FILE" >&2
exit "$_rc"
