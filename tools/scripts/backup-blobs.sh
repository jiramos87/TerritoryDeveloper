#!/usr/bin/env bash
# backup-blobs.sh — Stage 18.1 / TECH-8603
#
# Nightly rsync mirror of ${BLOB_ROOT:-var/blobs}/ to
# ${BACKUP_ROOT:-data/backups}/blobs/{YYYY-MM-DD}/. Emits file count log
# line for retention sweep + dashboard.
#
# Note on path: spec §Plan Digest references `data/blobs/`; repo
# convention (TECH-1435 / bootstrap-blob-root.sh) lives at `var/blobs/`.
# BLOB_ROOT defaults to the actual repo path; override via env.
#
# Usage:
#   tools/scripts/backup-blobs.sh [--date YYYY-MM-DD]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

BACKUP_ROOT="${BACKUP_ROOT:-${REPO_ROOT}/data/backups}"
BLOB_ROOT="${BLOB_ROOT:-${REPO_ROOT}/var/blobs}"

die() {
  echo "backup-blobs: error: $*" >&2
  exit 1
}

DATE_OVERRIDE=""
for arg in "$@"; do
  case "$arg" in
    --date) shift; DATE_OVERRIDE="${1:-}"; shift || true ;;
    --date=*) DATE_OVERRIDE="${arg#--date=}" ;;
    -h|--help) sed -n '1,/^set -euo/p' "$0" | head -n -1; exit 0 ;;
  esac
done

command -v rsync >/dev/null 2>&1 || die "rsync not on PATH"
[[ -d "${BLOB_ROOT}" ]] || die "BLOB_ROOT not a directory: ${BLOB_ROOT}"

DATE_UTC="${DATE_OVERRIDE:-$(date -u +%Y-%m-%d)}"
[[ "${DATE_UTC}" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}$ ]] || die "invalid date: ${DATE_UTC}"

OUT_DIR="${BACKUP_ROOT}/blobs/${DATE_UTC}"
LOG_DIR="${BACKUP_ROOT}/.log"
LOG_FILE="${LOG_DIR}/${DATE_UTC}.log"

mkdir -p "${OUT_DIR}" "${LOG_DIR}"

# Trailing slash on BLOB_ROOT mirrors contents (not the dir itself).
rsync -a --delete-after "${BLOB_ROOT}/" "${OUT_DIR}/"

FILE_COUNT=$(find "${OUT_DIR}" -type f | wc -l | tr -d ' ')
LINE="$(date -u +%FT%TZ) backup-blobs ok dest=${OUT_DIR} files=${FILE_COUNT}"
echo "${LINE}"
echo "${LINE}" >>"${LOG_FILE}"
