#!/usr/bin/env bash
# backup-db.sh — Stage 18.1 / TECH-8603
#
# Nightly pg_dump (custom format) of territory_ia_dev to a dated directory
# under ${BACKUP_ROOT:-data/backups}/db/{YYYY-MM-DD}/. Emits size + sha256
# log line for retention sweep + dashboard.
#
# Reads connection from config/postgres-dev.json (same source as
# freeze-db-snapshot.sh + setup-territory-ia-postgres.sh).
#
# Usage:
#   tools/scripts/backup-db.sh [--date YYYY-MM-DD]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

CONFIG_JSON="${REPO_ROOT}/config/postgres-dev.json"
BACKUP_ROOT="${BACKUP_ROOT:-${REPO_ROOT}/data/backups}"

die() {
  echo "backup-db: error: $*" >&2
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

[[ -f "${CONFIG_JSON}" ]] || die "missing ${CONFIG_JSON}"
command -v pg_dump >/dev/null 2>&1 || die "pg_dump not on PATH"
command -v shasum  >/dev/null 2>&1 || command -v sha256sum >/dev/null 2>&1 || die "shasum/sha256sum required"

DB_URL="$(
  CONFIG_JSON="${CONFIG_JSON}" node -e \
    'console.log(JSON.parse(require("fs").readFileSync(process.env.CONFIG_JSON, "utf8")).database_url)'
)"
[[ -n "${DB_URL}" ]] || die "database_url missing in ${CONFIG_JSON}"

DATE_UTC="${DATE_OVERRIDE:-$(date -u +%Y-%m-%d)}"
[[ "${DATE_UTC}" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}$ ]] || die "invalid date: ${DATE_UTC}"

OUT_DIR="${BACKUP_ROOT}/db/${DATE_UTC}"
LOG_DIR="${BACKUP_ROOT}/.log"
LOG_FILE="${LOG_DIR}/${DATE_UTC}.log"
DUMP_FILE="${OUT_DIR}/territory_ia_dev.dump"

mkdir -p "${OUT_DIR}" "${LOG_DIR}"

pg_dump --format=custom --no-owner --no-privileges --dbname="${DB_URL}" --file="${DUMP_FILE}"

SIZE_BYTES=$(wc -c <"${DUMP_FILE}" | tr -d ' ')
if command -v shasum >/dev/null 2>&1; then
  HASH=$(shasum -a 256 "${DUMP_FILE}" | awk '{print $1}')
else
  HASH=$(sha256sum "${DUMP_FILE}" | awk '{print $1}')
fi

LINE="$(date -u +%FT%TZ) backup-db ok dump=${DUMP_FILE} size=${SIZE_BYTES} sha256=${HASH}"
echo "${LINE}"
echo "${LINE}" >>"${LOG_FILE}"
