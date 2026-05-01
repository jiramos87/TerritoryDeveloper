#!/usr/bin/env bash
# verify-db-restore.sh — Stage 18.1 / TECH-8605
#
# Monthly DR drill: restores latest nightly pg_dump from TECH-8603 into an
# ephemeral DB `territory_ia_restore_test_{unix_ts}`, runs the catalog-spine
# validator against it, drops the ephemeral DB (always — trap-guarded), and
# writes drill outcome JSON to data/state/restore-drill/{YYYY-MM-DD}.json.
#
# Usage:
#   tools/scripts/verify-db-restore.sh [--date YYYY-MM-DD]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
# verify-db-restore.sh sits under tools/scripts/, so REPO_ROOT must climb 2.
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

CONFIG_JSON="${REPO_ROOT}/config/postgres-dev.json"
BACKUP_ROOT="${BACKUP_ROOT:-${REPO_ROOT}/data/backups}"
DRILL_DIR="${REPO_ROOT}/data/state/restore-drill"

die() {
  echo "verify-db-restore: error: $*" >&2
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
command -v pg_restore >/dev/null 2>&1 || die "pg_restore not on PATH"
command -v createdb   >/dev/null 2>&1 || die "createdb not on PATH"
command -v dropdb     >/dev/null 2>&1 || die "dropdb not on PATH"

DB_URL="$(
  CONFIG_JSON="${CONFIG_JSON}" node -e \
    'console.log(JSON.parse(require("fs").readFileSync(process.env.CONFIG_JSON, "utf8")).database_url)'
)"
[[ -n "${DB_URL}" ]] || die "database_url missing in ${CONFIG_JSON}"

# Strip path component → admin URL pointing at postgres maintenance DB.
ADMIN_URL="${DB_URL%/*}/postgres"

DATE_UTC="${DATE_OVERRIDE:-$(date -u +%Y-%m-%d)}"
[[ "${DATE_UTC}" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}$ ]] || die "invalid date: ${DATE_UTC}"

# Locate latest dump under BACKUP_ROOT/db/*/territory_ia_dev.dump.
LATEST_DUMP="$(
  find "${BACKUP_ROOT}/db" -maxdepth 2 -name 'territory_ia_dev.dump' 2>/dev/null \
    | sort \
    | tail -n 1
)"

mkdir -p "${DRILL_DIR}"
OUT_JSON="${DRILL_DIR}/${DATE_UTC}.json"

if [[ -z "${LATEST_DUMP}" ]] || [[ ! -f "${LATEST_DUMP}" ]]; then
  cat > "${OUT_JSON}" <<EOF
{
  "date": "${DATE_UTC}",
  "ok": false,
  "reason": "no_dump_found",
  "dump_path": null,
  "latency_ms": 0,
  "validate_exit": null
}
EOF
  echo "verify-db-restore: FAIL — no dump under ${BACKUP_ROOT}/db/" >&2
  exit 1
fi

EPHEMERAL_DB="territory_ia_restore_test_$(date +%s)"
EPHEMERAL_URL="${DB_URL%/*}/${EPHEMERAL_DB}"

cleanup() {
  # Always drop the ephemeral DB even on Ctrl-C / restore failure.
  dropdb --if-exists --dbname="${ADMIN_URL}" "${EPHEMERAL_DB}" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

START_MS="$(node -e 'console.log(Date.now())')"

createdb --dbname="${ADMIN_URL}" "${EPHEMERAL_DB}" \
  || die "createdb ${EPHEMERAL_DB} failed"

RESTORE_EXIT=0
pg_restore \
  --dbname="${EPHEMERAL_URL}" \
  --no-owner \
  --no-privileges \
  "${LATEST_DUMP}" \
  || RESTORE_EXIT=$?

VALIDATE_EXIT=0
if [[ "${RESTORE_EXIT}" -eq 0 ]]; then
  ( cd "${REPO_ROOT}" && DATABASE_URL="${EPHEMERAL_URL}" npm run validate:catalog-spine ) \
    || VALIDATE_EXIT=$?
else
  VALIDATE_EXIT=-1
fi

END_MS="$(node -e 'console.log(Date.now())')"
LATENCY_MS=$(( END_MS - START_MS ))

if [[ "${RESTORE_EXIT}" -eq 0 ]] && [[ "${VALIDATE_EXIT}" -eq 0 ]]; then
  OK=true
  REASON="ok"
else
  OK=false
  if [[ "${RESTORE_EXIT}" -ne 0 ]]; then
    REASON="pg_restore_exit_${RESTORE_EXIT}"
  else
    REASON="validate_exit_${VALIDATE_EXIT}"
  fi
fi

cat > "${OUT_JSON}" <<EOF
{
  "date": "${DATE_UTC}",
  "ok": ${OK},
  "reason": "${REASON}",
  "dump_path": "${LATEST_DUMP}",
  "ephemeral_db": "${EPHEMERAL_DB}",
  "latency_ms": ${LATENCY_MS},
  "restore_exit": ${RESTORE_EXIT},
  "validate_exit": ${VALIDATE_EXIT}
}
EOF

if [[ "${OK}" = "true" ]]; then
  echo "verify-db-restore: OK ${DATE_UTC} latency=${LATENCY_MS}ms dump=${LATEST_DUMP}"
else
  echo "verify-db-restore: FAIL ${DATE_UTC} reason=${REASON} dump=${LATEST_DUMP}" >&2
  exit 1
fi
