#!/usr/bin/env bash
# backup-retention.sh — Stage 18.1 / TECH-8603
#
# Prune ${BACKUP_ROOT:-data/backups}/{db,blobs}/ by tier:
#   • Daily anchors (most recent ${DAILY_KEEP:-14} days)
#   • Weekly anchors (Sunday-dated dirs, ${WEEKLY_KEEP:-8} most recent)
#   • Monthly anchors (1st-of-month dated dirs, ${MONTHLY_KEEP:-6} most recent)
#
# Idempotent: re-runs preserve existing weekly/monthly anchors.
#
# Tier overrides via env: DAILY_KEEP / WEEKLY_KEEP / MONTHLY_KEEP.
#
# Usage:
#   tools/scripts/backup-retention.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck source=load-repo-env.inc.sh
source "${SCRIPT_DIR}/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

BACKUP_ROOT="${BACKUP_ROOT:-${REPO_ROOT}/data/backups}"
DAILY_KEEP="${DAILY_KEEP:-14}"
WEEKLY_KEEP="${WEEKLY_KEEP:-8}"
MONTHLY_KEEP="${MONTHLY_KEEP:-6}"

die() {
  echo "backup-retention: error: $*" >&2
  exit 1
}

# Returns 0 (Sunday) … 6 (Saturday) for a YYYY-MM-DD date string.
day_of_week() {
  local d="$1"
  if date -u -j -f "%Y-%m-%d" "${d}" "+%w" >/dev/null 2>&1; then
    date -u -j -f "%Y-%m-%d" "${d}" "+%w"   # macOS BSD date
  else
    date -u -d "${d}" "+%w"                  # GNU date
  fi
}

# Returns day-of-month integer.
day_of_month() {
  local d="$1"
  echo "${d##*-}" | sed 's/^0//'
}

prune_tier() {
  local tier_dir="$1"
  [[ -d "${tier_dir}" ]] || { echo "backup-retention: ${tier_dir} missing — skipping"; return 0; }

  # All dated subdirs (YYYY-MM-DD), newest first.
  local dirs=()
  while IFS= read -r d; do dirs+=("$d"); done < <(
    find "${tier_dir}" -maxdepth 1 -mindepth 1 -type d -name '????-??-??' -exec basename {} \; | sort -r
  )

  local daily_kept=0 weekly_kept=0 monthly_kept=0
  declare -a KEEP=()

  for d in "${dirs[@]}"; do
    local keep_reason=""

    if (( daily_kept < DAILY_KEEP )); then
      keep_reason="daily"
      daily_kept=$((daily_kept + 1))
    fi

    local dom dow
    dom=$(day_of_month "$d")
    dow=$(day_of_week "$d")

    if [[ -z "${keep_reason}" && "${dow}" == "0" && "${weekly_kept}" -lt "${WEEKLY_KEEP}" ]]; then
      keep_reason="weekly"
    fi
    if [[ "${dow}" == "0" && "${weekly_kept}" -lt "${WEEKLY_KEEP}" ]]; then
      weekly_kept=$((weekly_kept + 1))
    fi

    if [[ -z "${keep_reason}" && "${dom}" == "1" && "${monthly_kept}" -lt "${MONTHLY_KEEP}" ]]; then
      keep_reason="monthly"
    fi
    if [[ "${dom}" == "1" && "${monthly_kept}" -lt "${MONTHLY_KEEP}" ]]; then
      monthly_kept=$((monthly_kept + 1))
    fi

    if [[ -n "${keep_reason}" ]]; then
      KEEP+=("$d")
      echo "backup-retention: keep ${tier_dir}/${d} (${keep_reason})"
    else
      echo "backup-retention: prune ${tier_dir}/${d}"
      rm -rf "${tier_dir:?}/${d}"
    fi
  done
}

mkdir -p "${BACKUP_ROOT}/db" "${BACKUP_ROOT}/blobs"
prune_tier "${BACKUP_ROOT}/db"
prune_tier "${BACKUP_ROOT}/blobs"

echo "backup-retention: done (daily=${DAILY_KEEP} weekly=${WEEKLY_KEEP} monthly=${MONTHLY_KEEP})"
