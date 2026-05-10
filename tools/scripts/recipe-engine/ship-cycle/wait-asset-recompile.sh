#!/usr/bin/env bash
# ship-cycle Pass B Phase B.7b — synchronous gate that waits for Unity
# Editor to finish recompiling after `maybe-refresh-asset-db.sh` enqueued
# a refresh_asset_database job.
#
# Why: pre-hardening, Pass B fired-and-forgot the refresh + relied on
# async cron drain (B.13). That left a window where the stage commit
# (B.8) landed BEFORE the live Editor compile result was known — so
# false-green verifies (stale compilation_failed flag) shipped broken
# trees. This script closes that gap: when Assets/** touched, block on
# the bridge round-trip until compile is genuinely green.
#
# Args:
#   --slug              <plan-slug>
#   --stage-id          <X.Y>
#   --touched-assets    <true|false>   (from maybe-refresh-asset-db output)
#   --refresh-job-id    <uuid>         (from maybe-refresh-asset-db output)
#   --timeout-seconds   <int default=120>
#   --poll-interval     <int default=3>
#
# Output (stdout, exit 0):
#   touched_assets=false skipped=true
#   touched_assets=true compile_status=green refresh_ms=<N> compile_ms=<N>
# Exit 1: compile_failed=true (prints recent_error_messages to stderr).
# Exit 2: timeout.
# Exit 3: psql / bridge unavailable.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"
# shellcheck disable=SC1091
source "${REPO_ROOT}/tools/scripts/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

slug=""
stage_id=""
touched_assets=""
refresh_job_id=""
timeout_seconds=120
poll_interval=3
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)                              slug="$2";            shift 2 ;;
    --stage-id|--stage_id)               stage_id="$2";        shift 2 ;;
    --touched-assets|--touched_assets)   touched_assets="$2";  shift 2 ;;
    --refresh-job-id|--refresh_job_id)   refresh_job_id="$2";  shift 2 ;;
    --timeout-seconds|--timeout_seconds) timeout_seconds="$2"; shift 2 ;;
    --poll-interval|--poll_interval)     poll_interval="$2";   shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$stage_id" ]]; then
  echo "wait-asset-recompile: missing --slug or --stage-id" >&2
  exit 3
fi

# Short-circuit when Pass B B.7 reported no Asset touches.
if [[ "$touched_assets" != "true" ]]; then
  echo "touched_assets=false skipped=true"
  exit 0
fi

if [[ -z "${DATABASE_URL:-}" ]]; then
  echo "wait-asset-recompile: DATABASE_URL unset" >&2
  exit 3
fi

cd "$REPO_ROOT"

started_at=$(date +%s)

# Phase 1 — wait for the refresh_asset_database job to drain.
if [[ -n "$refresh_job_id" ]]; then
  while : ; do
    elapsed=$(( $(date +%s) - started_at ))
    if (( elapsed > timeout_seconds )); then
      echo "wait-asset-recompile: timeout waiting for refresh_asset_database job ${refresh_job_id}" >&2
      exit 2
    fi
    status=$(psql "$DATABASE_URL" -tAc "
      SELECT status FROM agent_bridge_job WHERE command_id = '${refresh_job_id}'
    " 2>/dev/null | tr -d '[:space:]' || true)
    case "$status" in
      completed) break ;;
      failed)
        err=$(psql "$DATABASE_URL" -tAc "
          SELECT error FROM agent_bridge_job WHERE command_id = '${refresh_job_id}'
        " 2>/dev/null || true)
        echo "wait-asset-recompile: refresh_asset_database failed — ${err}" >&2
        exit 1
        ;;
      pending|processing|"") sleep "$poll_interval" ;;
      *) echo "wait-asset-recompile: unexpected refresh status: ${status}" >&2; exit 3 ;;
    esac
  done
fi
refresh_ms=$(( ($(date +%s) - started_at) * 1000 ))

# Phase 2 — poll compile status until quiet AND green (or fail / timeout).
compile_start=$(date +%s)
while : ; do
  elapsed=$(( $(date +%s) - started_at ))
  if (( elapsed > timeout_seconds )); then
    echo "wait-asset-recompile: timeout waiting for compile (total ${elapsed}s)" >&2
    exit 2
  fi

  # Enqueue a fresh get_compilation_status job each iteration so we get
  # a current read, not a cached pointer to an old response row.
  cmd_id=$(psql "$DATABASE_URL" -tAc "
    INSERT INTO agent_bridge_job (command_id, kind, status, request)
    VALUES (gen_random_uuid(), 'get_compilation_status', 'pending', '{\"params\":{}}'::jsonb)
    RETURNING command_id
  " 2>/dev/null | tr -d '[:space:]' || true)

  if [[ -z "$cmd_id" ]]; then
    echo "wait-asset-recompile: psql INSERT failed (compile poll)" >&2
    exit 3
  fi

  # Wait for that one job to settle.
  job_started=$(date +%s)
  job_settled=""
  while : ; do
    jelapsed=$(( $(date +%s) - job_started ))
    overall=$(( $(date +%s) - started_at ))
    if (( overall > timeout_seconds )); then
      echo "wait-asset-recompile: timeout waiting for compile-status job ${cmd_id}" >&2
      exit 2
    fi
    if (( jelapsed > 30 )); then
      echo "wait-asset-recompile: compile-status job ${cmd_id} did not drain within 30s — bridge stalled?" >&2
      exit 3
    fi
    status=$(psql "$DATABASE_URL" -tAc "
      SELECT status FROM agent_bridge_job WHERE command_id = '${cmd_id}'
    " 2>/dev/null | tr -d '[:space:]' || true)
    case "$status" in
      completed) job_settled="completed"; break ;;
      failed)    job_settled="failed";    break ;;
      *) sleep 2 ;;
    esac
  done

  if [[ "$job_settled" != "completed" ]]; then
    err=$(psql "$DATABASE_URL" -tAc "
      SELECT COALESCE(error, '') FROM agent_bridge_job WHERE command_id = '${cmd_id}'
    " 2>/dev/null || true)
    echo "wait-asset-recompile: get_compilation_status bridge failure — ${err}" >&2
    exit 3
  fi

  # Parse compilation_status from response jsonb.
  resp=$(psql "$DATABASE_URL" -tAc "
    SELECT response::text FROM agent_bridge_job WHERE command_id = '${cmd_id}'
  " 2>/dev/null || true)

  if [[ -z "$resp" ]]; then
    sleep "$poll_interval"
    continue
  fi

  read -r compiling compile_failed err_excerpt <<<"$(node -e '
    const r = JSON.parse(process.argv[1] || "{}");
    const cs = r.compilation_status || {};
    process.stdout.write(
      String(cs.compiling ?? "true") + " " +
      String(cs.compilation_failed ?? "true") + " " +
      JSON.stringify(cs.last_error_excerpt || "")
    );
  ' "$resp")"

  if [[ "$compiling" == "true" ]]; then
    sleep "$poll_interval"
    continue
  fi

  if [[ "$compile_failed" == "true" ]]; then
    errs=$(node -e '
      const r = JSON.parse(process.argv[1] || "{}");
      const cs = r.compilation_status || {};
      const list = cs.recent_error_messages || [];
      if (list.length === 0 && cs.last_error_excerpt) {
        process.stdout.write(cs.last_error_excerpt);
      } else {
        process.stdout.write(list.join("\n"));
      }
    ' "$resp")
    echo "wait-asset-recompile: Unity compile FAILED after refresh" >&2
    echo "${errs}" >&2
    exit 1
  fi

  # compiling=false AND compile_failed=false → green
  compile_ms=$(( ($(date +%s) - compile_start) * 1000 ))
  echo "touched_assets=true compile_status=green refresh_ms=${refresh_ms} compile_ms=${compile_ms}"
  exit 0
done
