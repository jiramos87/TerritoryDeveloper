#!/usr/bin/env bash
# bootstrap-blob-root.sh — TECH-1435.
#
# Idempotent setup for the canonical local `var/blobs/` blob root + matching
# `.gitignore` entries. Re-running is a no-op once the tree is configured.

set -euo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
BLOB_ROOT="${REPO_ROOT}/var/blobs"
GITIGNORE="${REPO_ROOT}/.gitignore"
GITIGNORE_DIR_LINE="var/blobs/"
GITIGNORE_KEEP_LINE="!var/blobs/.gitkeep"

mkdir -p "${BLOB_ROOT}"

# Drop a .gitkeep so the dir ships at HEAD even though contents are gitignored.
if [[ ! -f "${BLOB_ROOT}/.gitkeep" ]]; then
  cat > "${BLOB_ROOT}/.gitkeep" <<'EOF'
# Keep var/blobs/ in tree even though contents are gitignored. TECH-1435.
EOF
fi

ensure_line() {
  local line="$1"
  if ! grep -Fxq -- "${line}" "${GITIGNORE}"; then
    printf '\n%s\n' "${line}" >> "${GITIGNORE}"
  fi
}

# Append both rules: ignore directory, but keep the .gitkeep marker.
ensure_line "${GITIGNORE_DIR_LINE}"
ensure_line "${GITIGNORE_KEEP_LINE}"

echo "[bootstrap-blob-root] ${BLOB_ROOT} ready"
