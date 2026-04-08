#!/usr/bin/env bash
# Canonical local post-implementation verification (repository root).
# 1) npm run validate:all  (dead project specs, compute-lib build, test:ia, fixtures, IA index --check)
# 2) post-implementation-verify.sh --skip-node-checks  (Unity batch compile, DB migrate/preflight, macOS Editor + bridge smoke)
# Pass-through: npm run verify:local -- "x,y"  →  seed cell for db:bridge-playmode-smoke
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
cd "${REPO_ROOT}"

npm run validate:all
exec bash "${SCRIPT_DIR}/post-implementation-verify.sh" --skip-node-checks "$@"
