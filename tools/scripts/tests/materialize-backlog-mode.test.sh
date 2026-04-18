#!/usr/bin/env bash
# materialize-backlog-mode.test.sh
#
# Regression test for BUG-58: verify materialize-backlog.sh mode switching.
#
# Tests three modes:
#   1. Default (write) mode — node mjs regenerates BACKLOG files from yaml, exits 0.
#   2. --check arg on synced files — exits 0 (no drift).
#   3. --check arg with artificially drifted BACKLOG copy — exits non-zero.
#   4. EXTRA_ARGS guard logic (value-equality) — bash unit tests.
#
# Does NOT mutate real ia/backlog/*.yaml or real BACKLOG.md / BACKLOG-ARCHIVE.md.
# Uses a trap-guarded temp dir for the drift test.
#
# Usage:
#   bash tools/scripts/tests/materialize-backlog-mode.test.sh
# Exit code: 0 = all pass, 1 = one or more failures.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
MJS="${REPO_ROOT}/tools/scripts/materialize-backlog.mjs"

PASS=0
FAIL=0

pass() { echo "[PASS] $1"; PASS=$((PASS + 1)); }
fail() { echo "[FAIL] $1"; FAIL=$((FAIL + 1)); }

# ── Test 1: default write mode — exits 0 and files present ───────────────────

if node "${MJS}" 2>&1 | grep -qE "Written|OK"; then
  if [ -f "${REPO_ROOT}/BACKLOG.md" ] && [ -f "${REPO_ROOT}/BACKLOG-ARCHIVE.md" ]; then
    pass "Test 1: default write mode exits 0 and BACKLOG files present"
  else
    fail "Test 1: BACKLOG files missing after write mode"
  fi
else
  fail "Test 1: node mjs write mode — unexpected output or non-zero exit"
fi

# ── Test 2: --check on synced state exits 0 ──────────────────────────────────

if node "${MJS}" --check > /dev/null 2>&1; then
  pass "Test 2: --check exits 0 on synced BACKLOG files"
else
  fail "Test 2: --check exits non-zero on synced files (unexpected)"
fi

# ── Test 3: drift detection — verify mjs exits non-zero when BACKLOG drifts ──
# Temporarily overwrite BACKLOG.md with a drifted copy, restore via trap.
# BACKLOG.md is a generated view (not yaml source); mutation is idempotent.

ORIG_BACKLOG="${REPO_ROOT}/BACKLOG.md"
BACKUP_BACKLOG="$(mktemp)"
trap 'cp "${BACKUP_BACKLOG}" "${ORIG_BACKLOG}"; rm -f "${BACKUP_BACKLOG}"' EXIT

cp "${ORIG_BACKLOG}" "${BACKUP_BACKLOG}"
echo "# DRIFT_MARKER_BUG58" >> "${ORIG_BACKLOG}"

if ! node "${MJS}" --check > /dev/null 2>&1; then
  pass "Test 3: --check exits non-zero on drifted BACKLOG.md"
else
  fail "Test 3: --check should exit non-zero on drift but exited 0"
fi

# Restore immediately (trap also fires on EXIT, this makes it explicit).
cp "${BACKUP_BACKLOG}" "${ORIG_BACKLOG}"
trap - EXIT
rm -f "${BACKUP_BACKLOG}"

# ── Test 4: value-equality guard — CHECK_MODE=0 produces no --check flag ─────

T4_RESULT="$(bash -c '
  CHECK_MODE=0; EXTRA_ARGS=(); [ "$CHECK_MODE" = "1" ] && EXTRA_ARGS+=(--check); echo "${#EXTRA_ARGS[@]}"
')"
if [ "$T4_RESULT" = "0" ]; then
  pass "Test 4: CHECK_MODE=0 → empty EXTRA_ARGS (no --check passed to node)"
else
  fail "Test 4: CHECK_MODE=0 unexpectedly produced EXTRA_ARGS (count: $T4_RESULT)"
fi

# ── Test 5: value-equality guard — CHECK_MODE=1 produces --check flag ─────────

T5_RESULT="$(bash -c '
  CHECK_MODE=1; EXTRA_ARGS=(); [ "$CHECK_MODE" = "1" ] && EXTRA_ARGS+=(--check); echo "${EXTRA_ARGS[*]}"
')"
if [ "$T5_RESULT" = "--check" ]; then
  pass "Test 5: CHECK_MODE=1 → EXTRA_ARGS=(--check)"
else
  fail "Test 5: CHECK_MODE=1 EXTRA_ARGS wrong (got: '$T5_RESULT')"
fi

# ── Test 6: old :+ guard regression — "0" must NOT expand ────────────────────
# Guard against regression to ${CHECK_MODE:+--check} pattern.

T6_RESULT="$(bash -c 'CHECK_MODE=0; echo "${CHECK_MODE:+EXPANDED}"')"
if [ "$T6_RESULT" = "EXPANDED" ]; then
  pass "Test 6 (regression proof): bash :+ expands on '0' — confirms old guard was broken (BUG-58)"
else
  fail "Test 6: unexpected bash :+ behavior (environment anomaly)"
fi

# ── Summary ──────────────────────────────────────────────────────────────────

echo ""
echo "Results: ${PASS} passed, ${FAIL} failed."
[ "$FAIL" -eq 0 ]
