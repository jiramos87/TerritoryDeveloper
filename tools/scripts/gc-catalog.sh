#!/usr/bin/env bash
# gc-catalog.sh — Stage 18.1 / TECH-8604
#
# Wrapper that runs gc-catalog.ts from tools/postgres-ia/ so the `pg` dep
# from postgres-ia-tools resolves. The TypeScript entry uses paths relative
# to its own source location, so cwd does not affect script behaviour.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

cd "${REPO_ROOT}"
exec npx tsx "${REPO_ROOT}/tools/postgres-ia/gc-catalog.ts" "$@"
