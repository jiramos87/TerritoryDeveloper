#!/usr/bin/env bash
# install-gc-cron.sh — Stage 18.1 / TECH-8604
#
# Idempotent crontab installer for nightly catalog GC sweep.
# Schedule: 0 3 * * * — daily 03:00 local.
# Runs: gc-catalog.ts (both modes — retired + orphan).
#
# Usage:
#   tools/scripts/cron/install-gc-cron.sh
#   tools/scripts/cron/install-gc-cron.sh --uninstall

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"

MARKER="# territory-developer:gc-cron"
ENTRY="0 3 * * * cd ${REPO_ROOT} && bash tools/scripts/gc-catalog.sh ${MARKER}"

UNINSTALL=0
for arg in "$@"; do
  case "$arg" in
    --uninstall) UNINSTALL=1 ;;
    -h|--help) sed -n '1,/^set -euo/p' "$0" | head -n -1; exit 0 ;;
    *) echo "install-gc-cron: unknown flag: $arg" >&2; exit 2 ;;
  esac
done

current_crontab() {
  crontab -l 2>/dev/null || true
}

remove_marker_lines() {
  current_crontab | grep -vF "${MARKER}" || true
}

if [[ "${UNINSTALL}" -eq 1 ]]; then
  remove_marker_lines | crontab -
  echo "install-gc-cron: removed (marker=${MARKER})"
  exit 0
fi

NEW_CRON=$(remove_marker_lines)
if [[ -n "${NEW_CRON}" ]]; then
  printf '%s\n%s\n' "${NEW_CRON}" "${ENTRY}" | crontab -
else
  printf '%s\n' "${ENTRY}" | crontab -
fi

echo "install-gc-cron: installed → ${ENTRY}"
