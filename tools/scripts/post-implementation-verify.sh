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
# shellcheck source=unity-editor-helpers.inc.sh
source "${SCRIPT_DIR}/unity-editor-helpers.inc.sh"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
cd "${REPO_ROOT}"
territory_load_repo_dotenv_files "${REPO_ROOT}"

LOCK_FILE="${REPO_ROOT}/Temp/UnityLockfile"
_TERRITORY_LOG_PREFIX="post-implementation-verify"

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
