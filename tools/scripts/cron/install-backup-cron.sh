#!/usr/bin/env bash
# install-backup-cron.sh — Stage 18.1 / TECH-8603
#
# Idempotent crontab installer for nightly backup chain.
# Schedule: 0 2 * * * — daily 02:00 local.
# Runs: backup-db.sh → backup-blobs.sh → backup-retention.sh sequentially,
# all under a shared timestamped log line.
#
# Usage:
#   tools/scripts/cron/install-backup-cron.sh
#   tools/scripts/cron/install-backup-cron.sh --uninstall

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"

MARKER="# territory-developer:backup-cron"
ENTRY="0 2 * * * cd ${REPO_ROOT} && bash tools/scripts/backup-db.sh && bash tools/scripts/backup-blobs.sh && bash tools/scripts/backup-retention.sh ${MARKER}"

UNINSTALL=0
for arg in "$@"; do
  case "$arg" in
    --uninstall) UNINSTALL=1 ;;
    -h|--help) sed -n '1,/^set -euo/p' "$0" | head -n -1; exit 0 ;;
    *) echo "install-backup-cron: unknown flag: $arg" >&2; exit 2 ;;
  esac
done

current_crontab() {
  crontab -l 2>/dev/null || true
}

remove_marker_lines() {
  # Strip any existing line bearing our marker.
  current_crontab | grep -vF "${MARKER}" || true
}

if [[ "${UNINSTALL}" -eq 1 ]]; then
  remove_marker_lines | crontab -
  echo "install-backup-cron: removed (marker=${MARKER})"
  exit 0
fi

NEW_CRON=$(remove_marker_lines)
if [[ -n "${NEW_CRON}" ]]; then
  printf '%s\n%s\n' "${NEW_CRON}" "${ENTRY}" | crontab -
else
  printf '%s\n' "${ENTRY}" | crontab -
fi

echo "install-backup-cron: installed → ${ENTRY}"
