#!/usr/bin/env bash
# freeze-db-snapshot.sh
#
# Take a custom-format pg_dump of the project DB and persist it under
# var/db-snapshots/, with a sha256 manifest entry. Used as the safety
# net before destructive migrations (see DEC-A32, DEC-A50).
#
# Usage:
#   tools/scripts/freeze-db-snapshot.sh [tag]
#
# Defaults: tag = "manual". Output file = var/db-snapshots/{tag}-{YYYY-MM-DD}.dump.
# Same-day re-runs overwrite. The companion MANIFEST.txt accumulates one
# `{sha256}  {filename}` line per snapshot (newest appended).
#
# Reads connection from config/postgres-dev.json (consistent with
# setup-territory-ia-postgres.sh load_connection helper).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

CONFIG_JSON="${REPO_ROOT}/config/postgres-dev.json"
SNAPSHOT_DIR="${REPO_ROOT}/var/db-snapshots"

die() {
  echo "freeze-db-snapshot: error: $*" >&2
  exit 1
}

[[ -f "${CONFIG_JSON}" ]] || die "missing ${CONFIG_JSON}"
command -v pg_dump >/dev/null 2>&1 || die "pg_dump not on PATH"

DB_URL="$(
  CONFIG_JSON="${CONFIG_JSON}" node -e \
    'console.log(JSON.parse(require("fs").readFileSync(process.env.CONFIG_JSON, "utf8")).database_url)'
)"
[[ -n "${DB_URL}" ]] || die "database_url missing in ${CONFIG_JSON}"

TAG="${1:-manual}"
[[ "${TAG}" =~ ^[A-Za-z0-9_.-]+$ ]] || die "tag must be [A-Za-z0-9_.-]+, got: ${TAG}"

DATE_UTC="$(date -u +%Y-%m-%d)"
OUT_FILE="${SNAPSHOT_DIR}/${TAG}-${DATE_UTC}.dump"
MANIFEST="${SNAPSHOT_DIR}/MANIFEST.txt"

mkdir -p "${SNAPSHOT_DIR}"

echo "freeze-db-snapshot: dumping ${DB_URL} → ${OUT_FILE}"
pg_dump --format=custom --no-owner --no-privileges --file="${OUT_FILE}" "${DB_URL}"

if command -v shasum >/dev/null 2>&1; then
  SHA="$(shasum -a 256 "${OUT_FILE}" | awk '{print $1}')"
elif command -v sha256sum >/dev/null 2>&1; then
  SHA="$(sha256sum "${OUT_FILE}" | awk '{print $1}')"
else
  die "neither shasum nor sha256sum on PATH"
fi

REL_OUT="${OUT_FILE#${REPO_ROOT}/}"
TIMESTAMP="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

# MANIFEST.txt is committed; *.dump are gitignored. One line per snapshot.
touch "${MANIFEST}"
# Strip prior entry for the same file path (idempotent same-day re-run).
TMP="$(mktemp)"
grep -v -F "  ${REL_OUT}$" "${MANIFEST}" > "${TMP}" || true
mv "${TMP}" "${MANIFEST}"
echo "${TIMESTAMP}  ${SHA}  ${REL_OUT}" >> "${MANIFEST}"

echo "freeze-db-snapshot: ok"
echo "  file:   ${OUT_FILE}"
echo "  sha256: ${SHA}"
echo "  manifest line appended to: ${MANIFEST}"
