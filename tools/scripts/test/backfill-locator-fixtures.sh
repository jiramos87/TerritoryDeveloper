#!/usr/bin/env bash
# backfill-locator-fixtures.sh — fixture harness for backfill-parent-plan-locator.mjs.
#
# Iterates test-fixtures/backfill-locator/<case>; per case:
#   1. Copies fixture sandbox to a tmpdir (keeps source clean for repeated runs).
#   2. Exports IA_REPO_ROOT to the tmpdir sandbox.
#   3. Runs driver (stdout+stderr combined).
#   4. Diffs actual combined output vs expected.stdout.
#   5. Checks exit code vs expected.exit.
#   6. Prints PASS/FAIL per sub-case; exits non-zero on any FAIL.
#
# Source: Stage 3.2 Phase 2 (TECH-387).
#
# Usage:
#   ./tools/scripts/test/backfill-locator-fixtures.sh
#   npm run validate:backfill-fixtures

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
FIXTURES_DIR="${SCRIPT_DIR}/../test-fixtures/backfill-locator"
DRIVER="${SCRIPT_DIR}/../backfill-parent-plan-locator.mjs"

PASS=0
FAIL=0

# run_case <label> <fixture_dir> <expected_stdout_file> <expected_exit_file> [<driver_flags...>]
run_case() {
  local label="$1"
  local fixture_dir="$2"
  local expected_stdout="$3"
  local expected_exit_file="$4"
  shift 4

  local tmpdir
  tmpdir="$(mktemp -d)"

  # Copy fixture sandbox to tmpdir (preserves source state for repeated runs)
  cp -R "${fixture_dir}/ia" "${tmpdir}/"

  # Run driver; capture combined stdout+stderr; capture exit code safely
  local actual_output actual_exit
  actual_output="$(IA_REPO_ROOT="${tmpdir}" node "${DRIVER}" "$@" 2>&1)" || true
  # Capture real exit via subshell to avoid set -e interference
  actual_exit="$(IA_REPO_ROOT="${tmpdir}" node "${DRIVER}" "$@" 2>/dev/null; echo $?)" || true
  # Re-run cleanly for exit code only — use process substitution
  actual_exit="$(set +e; IA_REPO_ROOT="${tmpdir}" node "${DRIVER}" "$@" >/dev/null 2>&1; echo $?)"

  local expected_exit
  expected_exit="$(cat "${expected_exit_file}")"
  expected_exit="${expected_exit//[$'\n\r ']}"   # strip whitespace/newlines

  # Compare output — write actual to tmpfile for clean diff
  local actual_file
  actual_file="$(mktemp)"
  printf '%s\n' "${actual_output}" > "${actual_file}"

  local diff_out
  if diff_out="$(diff "${actual_file}" "${expected_stdout}" 2>&1)"; then
    local stdout_ok=true
  else
    local stdout_ok=false
  fi
  rm -f "${actual_file}"
  rm -rf "${tmpdir}"

  if [[ "${actual_exit}" == "${expected_exit}" ]] && ${stdout_ok}; then
    echo "PASS  ${label}"
    PASS=$((PASS + 1))
  else
    echo "FAIL  ${label}"
    if [[ "${actual_exit}" != "${expected_exit}" ]]; then
      echo "  exit: got=${actual_exit}  want=${expected_exit}"
    fi
    if ! ${stdout_ok}; then
      echo "  stdout diff (actual vs expected):"
      echo "${diff_out}" | sed 's/^/    /'
    fi
    FAIL=$((FAIL + 1))
  fi
}

# ── Cases ────────────────────────────────────────────────────────────────────

# Case 1: resolved — yaml missing both fields; plan row present → RESOLVE + exit 0
run_case "resolved" \
  "${FIXTURES_DIR}/resolved" \
  "${FIXTURES_DIR}/resolved/expected.stdout" \
  "${FIXTURES_DIR}/resolved/expected.exit"

# Case 2: already-populated — both fields present → idempotent skip + exit 0
run_case "already-populated" \
  "${FIXTURES_DIR}/already-populated" \
  "${FIXTURES_DIR}/already-populated/expected.stdout" \
  "${FIXTURES_DIR}/already-populated/expected.exit"

# Case 3a: plan-missing (no flag) → unresolvable error + exit 1
run_case "plan-missing (no flag)" \
  "${FIXTURES_DIR}/plan-missing" \
  "${FIXTURES_DIR}/plan-missing/expected-no-flag.stdout" \
  "${FIXTURES_DIR}/plan-missing/expected-no-flag.exit"

# Case 3b: plan-missing (--skip-unresolvable) → SKIP line + exit 0
run_case "plan-missing (--skip-unresolvable)" \
  "${FIXTURES_DIR}/plan-missing" \
  "${FIXTURES_DIR}/plan-missing/expected-skip.stdout" \
  "${FIXTURES_DIR}/plan-missing/expected-skip.exit" \
  "--skip-unresolvable"

# ── Summary ──────────────────────────────────────────────────────────────────

echo ""
echo "Results: ${PASS} passed, ${FAIL} failed"

if [[ "${FAIL}" -gt 0 ]]; then
  exit 1
fi
