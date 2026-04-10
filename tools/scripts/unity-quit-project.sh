#!/usr/bin/env bash
# Best-effort: terminate Unity Editor process(es) holding a lock on REPO_ROOT (macOS v1).
# Uses Temp/UnityLockfile + lsof when present. Does not use broad pkill (see --help).
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
territory_load_repo_dotenv_files "$REPO_ROOT"

usage() {
  cat <<'EOF'
Usage: unity-quit-project.sh [--repo-root PATH] [--wait-seconds N]

Best-effort SIGTERM to Unity Editor PIDs that hold REPO_ROOT/Temp/UnityLockfile
(via lsof). Waits until the lockfile disappears or --wait-seconds elapses, then
SIGKILL remaining holders if any.

Exit codes:
  0  No lockfile, or lock released within the wait budget
  3  Lock still held after SIGTERM/SIGKILL (or lsof could not resolve PIDs)
  1  Invalid arguments

Do not use this against shared machines with unrelated Unity projects unless you
verified the lockfile path is exclusive to this repo.

Optional fallback (not invoked by this script): osascript -e 'tell application "Unity" to quit'
quits every Unity instance — unsafe when multiple editors are open.
EOF
}

REPO="${REPO_ROOT}"
WAIT_SECS=45

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    --repo-root)
      REPO="$(cd "$2" && pwd)"
      shift 2
      ;;
    --wait-seconds)
      WAIT_SECS="$2"
      shift 2
      ;;
    *)
      echo "unity-quit-project: unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

LOCK="${REPO}/Temp/UnityLockfile"
if [[ ! -f "$LOCK" ]]; then
  echo "unity-quit-project: no lockfile at $LOCK (nothing to quit)." >&2
  exit 0
fi

pids_for_lock() {
  lsof -t "$LOCK" 2>/dev/null || true
}

_pids=()
while IFS= read -r _pid; do
  [[ -n "$_pid" ]] && _pids+=("$_pid")
done < <(pids_for_lock)

if [[ ${#_pids[@]} -eq 0 ]]; then
  echo "unity-quit-project: lockfile exists but lsof returned no PIDs; cannot safely signal." >&2
  exit 3
fi

for pid in "${_pids[@]}"; do
  if ps -p "$pid" -o comm= 2>/dev/null | grep -qi 'Unity'; then
    echo "unity-quit-project: SIGTERM pid=$pid" >&2
    kill -TERM "$pid" 2>/dev/null || true
  fi
done

_end=$((SECONDS + WAIT_SECS))
while [[ $SECONDS -lt $_end ]]; do
  [[ ! -f "$LOCK" ]] && exit 0
  sleep 1
done

if [[ ! -f "$LOCK" ]]; then
  exit 0
fi

_pids2=()
while IFS= read -r _pid; do
  [[ -n "$_pid" ]] && _pids2+=("$_pid")
done < <(pids_for_lock)

# With set -u, Bash 5.x errors on "${_pids2[@]}" when the array is empty.
if [[ ${#_pids2[@]} -gt 0 ]]; then
  for pid in "${_pids2[@]}"; do
    echo "unity-quit-project: SIGKILL pid=$pid" >&2
    kill -KILL "$pid" 2>/dev/null || true
  done
fi

sleep 2
if [[ -f "$LOCK" ]]; then
  echo "unity-quit-project: lockfile still present after kill: $LOCK" >&2
  exit 3
fi
exit 0
