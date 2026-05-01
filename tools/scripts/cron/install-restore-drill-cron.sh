#!/usr/bin/env bash
# install-restore-drill-cron.sh — Stage 18.1 / TECH-8605
#
# Idempotent crontab installer for monthly DR restore drill.
# Schedule: 0 4 1 * * — 04:00 on the 1st of every month
# (after Stage 18.1 nightly backup at 02:00 has settled).
#
# Usage:
#   tools/scripts/cron/install-restore-drill-cron.sh
#   tools/scripts/cron/install-restore-drill-cron.sh --uninstall

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"

MARKER="# territory-developer:restore-drill-cron"
ENTRY="0 4 1 * * cd ${REPO_ROOT} && bash tools/scripts/verify-db-restore.sh ${MARKER}"

UNINSTALL=0
for arg in "$@"; do
  case "$arg" in
    --uninstall) UNINSTALL=1 ;;
    -h|--help) sed -n '1,/^set -euo/p' "$0" | head -n -1; exit 0 ;;
    *) echo "install-restore-drill-cron: unknown flag: $arg" >&2; exit 2 ;;
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
  echo "install-restore-drill-cron: removed (marker=${MARKER})"
  exit 0
fi

NEW_CRON=$(remove_marker_lines)
if [[ -n "${NEW_CRON}" ]]; then
  printf '%s\n%s\n' "${NEW_CRON}" "${ENTRY}" | crontab -
else
  printf '%s\n' "${ENTRY}" | crontab -
fi

echo "install-restore-drill-cron: installed → ${ENTRY}"
