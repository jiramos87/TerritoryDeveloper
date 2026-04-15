#!/usr/bin/env bash
# unity-run-tests.sh — runs Unity NUnit tests in batchmode and parses results.
# Sources repo .env / .env.local for UNITY_EDITOR_PATH; macOS Hub fallback matches unity-compile-check.sh.
# AI agents: do not skip because $UNITY_EDITOR_PATH is unset — dotenv is loaded here.
# If the Unity Editor already has REPO_ROOT open, pass --quit-editor-first before batch — see docs/agent-led-verification-policy.md Path A.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
territory_load_repo_dotenv_files "$REPO_ROOT"
# shellcheck source=unity-editor-helpers.inc.sh
source "${SCRIPT_DIR}/unity-editor-helpers.inc.sh"

usage() {
  cat <<EOF
Usage: unity-run-tests.sh [options]

Runs Unity NUnit tests in -batchmode -nographics and parses the NUnit 3 XML result.

Options:
  --platform {editmode|playmode}   Test platform (default: editmode)
  --results-path PATH              Output XML path (default: tools/reports/unity-tests/{platform}-results.xml)
  --quit-editor-first              Quit Unity Editor on REPO_ROOT before launching batch
  --wait-quit-seconds N            Seconds to wait for Editor quit (default: 45)
  -h, --help                       This message

Output:
  Passed: N  Failed: M  Errors: K  Skipped: S
  FAILED: <fullname> (one per line for each failing test)

Exit codes:
  0  All tests passed
  1  One or more tests failed or errored
  2  Unity binary not found / not executable or missing results XML
  3  --quit-editor-first failed (lock still held)

Windows/Linux: set UNITY_EDITOR_PATH in .env or environment; Hub inference is macOS-only.
EOF
}

PLATFORM="editmode"
RESULTS_PATH=""
QUIT_FIRST=false
WAIT_QUIT_SECS=45

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    --platform)
      # Lowercase via tr for bash 3.2 compatibility (macOS default /bin/bash).
      PLATFORM="$(printf '%s' "$2" | tr '[:upper:]' '[:lower:]')"
      shift 2
      ;;
    --results-path)
      RESULTS_PATH="$2"
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
    *)
      echo "unity-run-tests: unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ "$PLATFORM" != "editmode" && "$PLATFORM" != "playmode" ]]; then
  echo "unity-run-tests: --platform must be 'editmode' or 'playmode', got: $PLATFORM" >&2
  exit 1
fi

if [[ -z "$RESULTS_PATH" ]]; then
  RESULTS_PATH="${REPO_ROOT}/tools/reports/unity-tests/${PLATFORM}-results.xml"
fi

# Resolve Unity binary (mirrors unity-compile-check.sh)
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
  echo "unity-run-tests: set UNITY_EDITOR_PATH in .env or export it (see ProjectSettings/ProjectVersion.txt)." >&2
  exit 2
fi
if [[ ! -x "$UNITY_BIN" ]] && [[ ! -f "$UNITY_BIN" ]]; then
  echo "unity-run-tests: UNITY_EDITOR_PATH is not a valid Unity binary: $UNITY_BIN" >&2
  exit 2
fi

# Quit editor if requested (mirrors unity-testmode-batch.sh pattern)
if [[ "$QUIT_FIRST" == true ]]; then
  if ! bash "${SCRIPT_DIR}/unity-quit-project.sh" --repo-root "$REPO_ROOT" --wait-seconds "$WAIT_QUIT_SECS"; then
    exit 3
  fi
fi

# Map lowercase platform to Unity CLI value
case "$PLATFORM" in
  editmode) UNITY_PLATFORM="EditMode" ;;
  playmode) UNITY_PLATFORM="PlayMode" ;;
esac

mkdir -p "$(dirname "$RESULTS_PATH")"

territory_preserve_editor_layout
echo "unity-run-tests: platform=$UNITY_PLATFORM project=$REPO_ROOT results=$RESULTS_PATH" >&2

set +e
"$UNITY_BIN" \
  -batchmode \
  -nographics \
  -runTests \
  -projectPath "$REPO_ROOT" \
  -testPlatform "$UNITY_PLATFORM" \
  -testResults "$RESULTS_PATH" \
  -logFile -
_unity_rc=$?
set -e

echo "unity-run-tests: Unity exit=$_unity_rc" >&2

if [[ ! -f "$RESULTS_PATH" ]]; then
  echo "unity-run-tests: no results XML produced at $RESULTS_PATH" >&2
  exit 2
fi

# Parse XML and emit human-readable summary; propagate non-zero from parser.
set +e
node "${SCRIPT_DIR}/parse-nunit-xml.mjs" "$RESULTS_PATH" --format=text
_parse_rc=$?
set -e

# Non-zero from parser (failures) OR Unity process itself (crash/error)
if [[ $_parse_rc -ne 0 || $_unity_rc -ne 0 ]]; then
  exit 1
fi
exit 0
