#!/usr/bin/env bash
# Local compile smoke: Unity -batchmode import + script compile, then quit.
# Requires UNITY_EDITOR_PATH to the Unity binary (macOS example inside Unity.app/.../MacOS/Unity).
# Do not run while another Unity Editor instance has this project open (project lock).
set -euo pipefail
REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_BIN="${UNITY_EDITOR_PATH:-}"
if [[ -z "$UNITY_BIN" ]]; then
  echo "unity-compile-check: set UNITY_EDITOR_PATH to your Unity editor binary (see ProjectSettings/ProjectVersion.txt)." >&2
  exit 2
fi
if [[ ! -x "$UNITY_BIN" ]] && [[ ! -f "$UNITY_BIN" ]]; then
  echo "unity-compile-check: UNITY_EDITOR_PATH is not a valid Unity binary: $UNITY_BIN" >&2
  exit 2
fi
LOG_DIR="$REPO_ROOT/tools/reports"
mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/unity-compile-check-$(date -u +%Y%m%d-%H%M%S).log"
echo "unity-compile-check: project=$REPO_ROOT log=$LOG_FILE" >&2
"$UNITY_BIN" -batchmode -nographics -quit -projectPath "$REPO_ROOT" -logFile "$LOG_FILE"
echo "unity-compile-check: success log=$LOG_FILE" >&2
