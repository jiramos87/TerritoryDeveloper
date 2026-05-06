#!/usr/bin/env bash
# ship-final Phase 4 — plan-scoped `validate:fast` on plan's task commits.
#
# Replaces prior whole-repo `validate:all` (which red-blocked closes on drift in
# UNRELATED plan files). Now derives touched paths by unioning `git show
# --name-only` across all `ia_task_commits.commit_sha` rows for this plan,
# then dispatches `validate:fast --diff-paths <csv>` (TECH-12640 path-map
# scoped runner). Empty plan-commit set falls back to full `validate:fast`
# behavior (HEAD diff) for safety.
#
# Args:
#   --slug    <plan-slug>
#   --version <N>
#
# Output (stdout, exit 0): result={"ok":true,"parent_tag":"...","scripts":[...]}
# Exit 1 (stderr): validate failed; full output streamed to stderr.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"

slug=""
version=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)    slug="$2";    shift 2 ;;
    --version) version="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$version" ]]; then
  echo "cumulative-validate: missing --slug or --version" >&2
  exit 1
fi

cd "$REPO_ROOT"

# parent_tag — empty for v1 (no prior version).
parent_tag=""
if [[ "$version" -gt 1 ]]; then
  prev=$((version - 1))
  parent_tag="${slug}-v${prev}"
  if ! git rev-parse "$parent_tag" >/dev/null 2>&1; then
    echo "cumulative-validate: parent_tag=${parent_tag} not found in repo — cannot bound cumulative diff" >&2
    exit 1
  fi
fi

# Plan-scope derivation — query ia_task_commits for shas of THIS plan's tasks,
# union touched paths via `git show --name-only`. Skip-clause: if no commits or
# no Postgres reachable → fall back to `validate:fast` HEAD-diff default.
PG_HOST="${PGHOST:-localhost}"
PG_PORT="${PGPORT:-5434}"
PG_USER="${PGUSER:-postgres}"
PG_PASSWORD="${PGPASSWORD:-postgres}"
PG_DB="${PGDATABASE:-territory_ia_dev}"

shas=""
if shas_raw=$(PGPASSWORD="$PG_PASSWORD" psql -h "$PG_HOST" -p "$PG_PORT" -U "$PG_USER" -d "$PG_DB" -At -c \
  "SELECT DISTINCT c.commit_sha FROM ia_task_commits c JOIN ia_tasks t ON t.task_id = c.task_id WHERE t.slug = '${slug}' ORDER BY c.commit_sha;" 2>/dev/null); then
  shas="$shas_raw"
fi

diff_paths_csv=""
if [[ -n "$shas" ]]; then
  paths_set=()
  while IFS= read -r sha; do
    [[ -z "$sha" ]] && continue
    while IFS= read -r p; do
      [[ -z "$p" ]] && continue
      paths_set+=("$p")
    done < <(git show --name-only --format= "$sha" 2>/dev/null || true)
  done <<< "$shas"
  if [[ ${#paths_set[@]} -gt 0 ]]; then
    # Dedupe + comma-join.
    diff_paths_csv=$(printf '%s\n' "${paths_set[@]}" | sort -u | paste -sd, -)
  fi
fi

echo "cumulative-validate: parent_tag=${parent_tag} plan_commits=$(echo "$shas" | grep -c .) touched_paths=$(echo "$diff_paths_csv" | tr ',' '\n' | grep -c .)" >&2

if [[ -n "$diff_paths_csv" ]]; then
  if npm run validate:fast -- --diff-paths "$diff_paths_csv" >&2; then
    echo "result={\"ok\":true,\"parent_tag\":\"${parent_tag}\",\"scripts\":[\"validate:fast\"],\"scope\":\"plan-commits\"}"
    exit 0
  fi
  echo "cumulative-validate: validate:fast FAILED (plan-scoped) — see stderr" >&2
  echo "result={\"ok\":false,\"parent_tag\":\"${parent_tag}\",\"scripts\":[\"validate:fast\"],\"scope\":\"plan-commits\"}"
  exit 1
fi

# Fallback — no plan commits resolved (DB unreachable or commits not yet
# recorded). Run validate:fast with default HEAD-diff scope.
if npm run validate:fast >&2; then
  echo "result={\"ok\":true,\"parent_tag\":\"${parent_tag}\",\"scripts\":[\"validate:fast\"],\"scope\":\"head-diff-fallback\"}"
  exit 0
fi

echo "cumulative-validate: validate:fast FAILED (HEAD fallback) — see stderr" >&2
echo "result={\"ok\":false,\"parent_tag\":\"${parent_tag}\",\"scripts\":[\"validate:fast\"],\"scope\":\"head-diff-fallback\"}"
exit 1
