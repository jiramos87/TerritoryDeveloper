#!/usr/bin/env bash
# Local Postgres helper for Territory IA (Postgres.app / Homebrew — no Docker required).
# Reads connection targets from config/postgres-dev.json at REPO_ROOT.
# Usage:
#   bash tools/scripts/setup-territory-ia-postgres.sh help
#   bash tools/scripts/setup-territory-ia-postgres.sh configure-port
#   bash tools/scripts/setup-territory-ia-postgres.sh init-db
#   bash tools/scripts/setup-territory-ia-postgres.sh migrate
#   bash tools/scripts/setup-territory-ia-postgres.sh server-restart   # pg_ctl start/restart (Postgres.app data dir)
#   bash tools/scripts/setup-territory-ia-postgres.sh all   # configure-port + pg_ctl restart + init-db + migrate

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
territory_load_repo_dotenv_files "$REPO_ROOT"
CONFIG_JSON="${REPO_ROOT}/config/postgres-dev.json"
POSTGRES_APP_SUPPORT="${HOME}/Library/Application Support/Postgres"

die() {
  echo "setup-territory-ia-postgres: error: $*" >&2
  exit 1
}

require_config() {
  [[ -f "${CONFIG_JSON}" ]] || die "missing ${CONFIG_JSON}"
}

# Prints tab-separated: host, port, user, password, database, admin_url
load_connection() {
  require_config
  # Do not pass the JSON path as argv to `node <<EOF` — Node treats the next token as a script file, not an argument to stdin code.
  CONFIG_JSON="${CONFIG_JSON}" node <<'NODE'
const fs = require("fs");
const path = process.env.CONFIG_JSON;
if (!path) {
  console.error("CONFIG_JSON env missing");
  process.exit(1);
}
const j = JSON.parse(fs.readFileSync(path, "utf8"));
const raw = String(j.database_url || "").trim().replace(/^postgresql:/i, "postgres:");
if (!raw) {
  console.error("database_url empty");
  process.exit(1);
}
const u = new URL(raw);
const host = u.hostname || "localhost";
const port = u.port || "5432";
const user = decodeURIComponent(u.username || "");
const pass = decodeURIComponent(u.password || "");
const database = (u.pathname || "/").replace(/^\//, "") || "postgres";
if (!user) {
  console.error("database_url must include a username");
  process.exit(1);
}
const admin = new URL(raw);
admin.pathname = "/postgres";
const adminUrl = admin.toString().replace(/^postgres:/i, "postgresql:");
const line = [host, port, user, pass, database, adminUrl].join("\t");
process.stdout.write(line);
NODE
}

parse_connection() {
  local line
  line="$(load_connection)"
  IFS=$'\t' read -r PGHOST PGPORT PGUSER PGPASSWORD PGDATABASE ADMIN_URL <<< "${line}"
  export PGHOST PGPORT PGUSER PGPASSWORD PGDATABASE ADMIN_URL
}

tcp_open() {
  local host="$1" port="$2"
  (echo >/dev/tcp/"${host}/${port}") 2>/dev/null
}

wait_for_port() {
  local host="$1" port="$2" tries="${3:-30}"
  local i
  for ((i = 1; i <= tries; i++)); do
    if tcp_open "${host}" "${port}"; then
      return 0
    fi
    sleep 1
  done
  return 1
}

# Sets PGAPP_PGDATA and PGAPP_PGCTL, or returns 1 (Postgres.app not found / pg_ctl missing).
postgresapp_resolve_paths() {
  PGAPP_PGDATA=""
  PGAPP_PGCTL=""
  if [[ -n "${POSTGRES_APP_PGDATA:-}" ]]; then
    PGAPP_PGDATA="${POSTGRES_APP_PGDATA}"
  else
    PGAPP_PGDATA="$(find "${POSTGRES_APP_SUPPORT}" -maxdepth 1 -type d -name 'var-*' 2>/dev/null | LC_ALL=C sort -t- -k2 -n | tail -1)"
  fi
  [[ -n "${PGAPP_PGDATA}" && -d "${PGAPP_PGDATA}" ]] || return 1
  local ver
  ver="$(basename "${PGAPP_PGDATA}")"
  ver="${ver#var-}"
  PGAPP_PGCTL="/Applications/Postgres.app/Contents/Versions/${ver}/bin/pg_ctl"
  [[ -x "${PGAPP_PGCTL}" ]] || PGAPP_PGCTL="/Applications/Postgres.app/Contents/Versions/latest/bin/pg_ctl"
  [[ -x "${PGAPP_PGCTL}" ]] || return 1
  return 0
}

try_postgresapp_pg_ctl_restart() {
  postgresapp_resolve_paths || return 1
  if "${PGAPP_PGCTL}" -D "${PGAPP_PGDATA}" restart -m fast 2>/dev/null; then
    return 0
  fi
  "${PGAPP_PGCTL}" -D "${PGAPP_PGDATA}" start 2>/dev/null && return 0
  return 1
}

cmd_help() {
  cat <<EOF
Territory IA — local PostgreSQL setup (bash)

Commands:
  help            This text.
  configure-port  Set port = N in Postgres.app postgresql.conf files under:
                    ${POSTGRES_APP_SUPPORT}
                  Backs up each file to postgresql.conf.bak.<epoch>
                  Then run server-restart (or start the server from Postgres.app).
  server-start    pg_ctl start for the latest var-* data dir (override: POSTGRES_APP_PGDATA).
  server-restart  pg_ctl restart -m fast, or start if not running (Postgres.app).
  init-db         Connect with admin URL (database postgres), ensure role password
                  and CREATE DATABASE from postgres-dev.json if missing.
  migrate         npm run db:migrate from repo root (uses same DATABASE_URL rules).
  all             configure-port, pg_ctl restart when possible, wait for port, init-db + migrate.

Connection is always read from: ${CONFIG_JSON}
EOF
}

cmd_server_start() {
  postgresapp_resolve_paths || die "Postgres.app data directory not found. Set POSTGRES_APP_PGDATA or install Postgres.app."
  "${PGAPP_PGCTL}" -D "${PGAPP_PGDATA}" start
}

cmd_server_restart() {
  postgresapp_resolve_paths || die "Postgres.app data directory not found. Set POSTGRES_APP_PGDATA or install Postgres.app."
  "${PGAPP_PGCTL}" -D "${PGAPP_PGDATA}" restart -m fast 2>/dev/null || "${PGAPP_PGCTL}" -D "${PGAPP_PGDATA}" start
}

# Returns 0 if at least one postgresql.conf was updated, 1 if none found (not a hard error for "all").
do_configure_port() {
  parse_connection
  local target_port="${PGPORT}"
  [[ "${target_port}" =~ ^[0-9]+$ ]] || die "invalid port in config: ${target_port}"

  local found=0
  while IFS= read -r -d '' conf; do
    found=1
    local bak="${conf}.bak.$(date +%s)"
    cp "${conf}" "${bak}"
    if grep -qE '^[[:space:]]*#?[[:space:]]*port[[:space:]]*=' "${conf}"; then
      perl -i -pe "s/^[#[:space:]]*port[[:space:]]*=[[:space:]]*.*/port = ${target_port}/" "${conf}"
    else
      printf "\n# Added by setup-territory-ia-postgres.sh\nport = %s\n" "${target_port}" >>"${conf}"
    fi
    echo "Updated port to ${target_port}: ${conf} (backup ${bak})"
  done < <(find "${POSTGRES_APP_SUPPORT}" -name postgresql.conf -print0 2>/dev/null)

  if [[ "${found}" -eq 0 ]]; then
    echo "No postgresql.conf under ${POSTGRES_APP_SUPPORT}."
    echo "If you use Homebrew PostgreSQL, set port = ${target_port} in your data directory's postgresql.conf, then: brew services restart postgresql@<version>"
    return 1
  fi

  echo "Then run: bash \"${SCRIPT_DIR}/setup-territory-ia-postgres.sh\" server-restart"
  echo "   or: bash \"${SCRIPT_DIR}/setup-territory-ia-postgres.sh\" init-db   (after the server is listening)"
  return 0
}

cmd_configure_port() {
  do_configure_port || exit $?
}

cmd_init_db() {
  parse_connection
  export PGPASSWORD

  if ! wait_for_port "${PGHOST}" "${PGPORT}" 5; then
    die "nothing listening on ${PGHOST}:${PGPORT}. Start PostgreSQL or run: $0 configure-port (then restart Postgres.app)"
  fi

  if ! psql "${ADMIN_URL}" -v ON_ERROR_STOP=1 -c "SELECT 1" >/dev/null; then
    die "cannot connect with admin URL to postgres database — check user, password, and listen_addresses"
  fi

  # Escape single quotes in password for SQL literal
  local esc_pw="${PGPASSWORD//\'/\'\'}"
  psql "${ADMIN_URL}" -v ON_ERROR_STOP=1 -c "ALTER USER \"${PGUSER}\" WITH PASSWORD '${esc_pw}';"

  local exists
  exists="$(psql "${ADMIN_URL}" -v ON_ERROR_STOP=1 -Atc "SELECT 1 FROM pg_database WHERE datname = '${PGDATABASE}'")"
  if [[ "${exists}" != "1" ]]; then
    psql "${ADMIN_URL}" -v ON_ERROR_STOP=1 -c "CREATE DATABASE \"${PGDATABASE}\" OWNER \"${PGUSER}\";"
    echo "Created database ${PGDATABASE}"
  else
    echo "Database ${PGDATABASE} already exists"
  fi

  local db_url
  db_url="$(
    CONFIG_JSON="${CONFIG_JSON}" node -e 'console.log(JSON.parse(require("fs").readFileSync(process.env.CONFIG_JSON, "utf8")).database_url)'
  )"
  psql "${db_url}" -v ON_ERROR_STOP=1 -c "SELECT 1 AS ok;" >/dev/null
  echo "Verified connection to ${PGDATABASE} on ${PGHOST}:${PGPORT}"
}

cmd_migrate() {
  cd "${REPO_ROOT}"
  npm run db:migrate
}

cmd_all() {
  set +e
  do_configure_port
  local st=$?
  set -e
  if [[ "${st}" -ne 0 ]]; then
    echo >&2 "configure-port did not update Postgres.app files (exit ${st}). Set port in postgresql.conf to match ${CONFIG_JSON}, then start the server."
  fi
  parse_connection
  if [[ "${st}" -eq 0 ]]; then
    if try_postgresapp_pg_ctl_restart; then
      echo "PostgreSQL started or restarted via pg_ctl (${PGAPP_PGDATA})."
    else
      echo "Could not run pg_ctl (Postgres.app missing or POSTGRES_APP_PGDATA unset). Start the server from Postgres.app, then re-run: bash \"${SCRIPT_DIR}/setup-territory-ia-postgres.sh\" init-db"
    fi
  fi
  echo
  if wait_for_port "${PGHOST}" "${PGPORT}" 25; then
    echo "PostgreSQL is listening on ${PGHOST}:${PGPORT}."
  else
    read -r -p "PostgreSQL not detected on ${PGHOST}:${PGPORT}. Start it, then press Enter... " _
  fi
  cmd_init_db
  cmd_migrate
  echo "Done. Unset DATABASE_URL uses config/postgres-dev.json."
}

main() {
  local sub="${1:-help}"
  case "${sub}" in
    help | -h | --help)
      cmd_help
      ;;
    configure-port)
      cmd_configure_port
      ;;
    init-db)
      cmd_init_db
      ;;
    migrate)
      cmd_migrate
      ;;
    server-start)
      cmd_server_start
      ;;
    server-restart)
      cmd_server_restart
      ;;
    all)
      cmd_all
      ;;
    *)
      die "unknown command: ${sub}; run: bash \"${SCRIPT_DIR}/setup-territory-ia-postgres.sh\" help"
      ;;
  esac
}

main "$@"
