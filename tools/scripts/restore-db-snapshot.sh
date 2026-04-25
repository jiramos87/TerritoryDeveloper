#!/usr/bin/env bash
# restore-db-snapshot.sh
#
# Restore a custom-format pg_dump produced by freeze-db-snapshot.sh into
# the current project DB. Destructive — drops + recreates schema content,
# so refuses to run without --confirm.
#
# Usage:
#   tools/scripts/restore-db-snapshot.sh path/to/snapshot.dump --confirm
#
# Reads connection from config/postgres-dev.json.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

CONFIG_JSON="${REPO_ROOT}/config/postgres-dev.json"

die() {
  echo "restore-db-snapshot: error: $*" >&2
  exit 1
}

usage() {
  cat <<EOF
Usage: restore-db-snapshot.sh <dump-file> --confirm

Destructive — overwrites schema + data in the database referenced by
config/postgres-dev.json. The --confirm flag is required.
EOF
  exit 2
}

[[ $# -ge 1 ]] || usage

DUMP_FILE=""
CONFIRM=0
for arg in "$@"; do
  case "$arg" in
    --confirm) CONFIRM=1 ;;
    -h|--help) usage ;;
    -*) die "unknown flag: $arg" ;;
    *)  [[ -z "$DUMP_FILE" ]] && DUMP_FILE="$arg" || die "unexpected arg: $arg" ;;
  esac
done

[[ -n "$DUMP_FILE" ]] || usage
[[ -f "$DUMP_FILE" ]] || die "dump not found: $DUMP_FILE"
[[ "$CONFIRM" -eq 1 ]] || die "refusing without --confirm"

[[ -f "${CONFIG_JSON}" ]] || die "missing ${CONFIG_JSON}"
command -v pg_restore >/dev/null 2>&1 || die "pg_restore not on PATH"

DB_URL="$(
  CONFIG_JSON="${CONFIG_JSON}" node -e \
    'console.log(JSON.parse(require("fs").readFileSync(process.env.CONFIG_JSON, "utf8")).database_url)'
)"
[[ -n "${DB_URL}" ]] || die "database_url missing in ${CONFIG_JSON}"

echo "restore-db-snapshot: restoring ${DUMP_FILE} → ${DB_URL}"
echo "  --clean --if-exists --no-owner --no-privileges"

pg_restore \
  --clean --if-exists \
  --no-owner --no-privileges \
  --dbname="${DB_URL}" \
  "${DUMP_FILE}"

echo "restore-db-snapshot: ok"
