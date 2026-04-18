#!/usr/bin/env bash
# backfill-parent-plan-locator.sh — thin wrapper for backfill-parent-plan-locator.mjs.
#
# Resolves parent_plan + task_key for open backlog yaml records that lack both fields.
# Source: Stage 3.2 Phase 2 (TECH-387).
#
# Flags (passed through to mjs driver):
#   --dry-run            Preview only; no disk writes.
#   --skip-unresolvable  Log + skip records with no plan hit; never exit 1 for unresolvable.
#   --archive            Accepted but no-op (archive scan lives in Step 6). Emits one warn.
#
# Examples:
#   ./tools/scripts/backfill-parent-plan-locator.sh --dry-run
#   ./tools/scripts/backfill-parent-plan-locator.sh --skip-unresolvable

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

exec node "${SCRIPT_DIR}/backfill-parent-plan-locator.mjs" "$@"
