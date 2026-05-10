#!/usr/bin/env bash
# ship-cycle Pass B Phase B.6 — refresh BACKLOG.md + BACKLOG-ARCHIVE.md.
#
# Thin recipe-engine shim around `tools/scripts/materialize-backlog.sh`.
# Lifted out of ship-cycle SKILL.md prose (Phase 7 had it buried as a side
# effect of stage_closeout_apply that the SQL fn never actually performed).
#
# No args required. flock'd inside materialize-backlog.sh — concurrent
# recipe runs serialize on `.materialize-backlog.lock`.
#
# Output (stdout): forwarded from materialize-backlog.sh.
# Exit 1: forwarded.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"

cd "$REPO_ROOT"

bash tools/scripts/materialize-backlog.sh
