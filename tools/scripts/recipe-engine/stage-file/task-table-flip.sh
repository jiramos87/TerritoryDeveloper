#!/usr/bin/env bash
# stage-file Phase 6.3 — atomic task-table flip.
#
# Replaces `_pending_` in the Issue column (3rd pipe-col) of each task table
# row in ia_stages.body with the actual filed task_id, in task_id ASC order.
# Idempotent: rows already showing a task_id are left unchanged.
#
# Args:
#   --slug     <plan-slug>
#   --stage-id <X.Y>
#
# Output (stdout, exit 0): "task_table_flipped=N" (rows updated).
# Exit 1: missing args / DB error.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../../.." && pwd)"
# shellcheck disable=SC1091
source "${REPO_ROOT}/tools/scripts/load-repo-env.inc.sh"
territory_load_repo_dotenv_files "$REPO_ROOT"

slug=""
stage_id=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)     slug="$2";     shift 2 ;;
    --stage-id) stage_id="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$stage_id" || -z "${DATABASE_URL:-}" ]]; then
  echo "task-table-flip: missing --slug / --stage-id / DATABASE_URL" >&2
  exit 1
fi

stage_norm="${stage_id#Stage }"
stage_norm="${stage_norm// /}"

# Fetch ordered task_id list for this slug+stage. bash3-safe (no mapfile).
task_ids=()
while IFS= read -r line; do [[ -n "$line" ]] && task_ids+=("$line"); done < <(psql "$DATABASE_URL" -tAc \
  "SELECT task_id FROM ia_tasks WHERE slug = '${slug//\'/\'\'}' AND stage_id = '${stage_norm//\'/\'\'}' ORDER BY task_id ASC" \
  2>/dev/null)

if [[ "${#task_ids[@]}" -eq 0 ]]; then
  echo "task_table_flipped=0"
  exit 0
fi

# Fetch current stage body.
body=$(psql "$DATABASE_URL" -tAc \
  "SELECT COALESCE(body,'') FROM ia_stages WHERE slug = '${slug//\'/\'\'}' AND stage_id = '${stage_norm//\'/\'\'}' LIMIT 1" \
  2>/dev/null || true)

if [[ -z "$body" ]]; then
  echo "task_table_flipped=0"
  exit 0
fi

# Replace _pending_ in Issue column (3rd pipe-delimited field) for task-table
# rows (| T{stage}.{k} | ... | _pending_ | _pending_ | ... |) in order.
# Uses Python for reliable multi-line text manipulation — bash3-safe outer shell.
updated=$(python3 - "$body" "${task_ids[@]}" <<'PYEOF'
import sys, re

body = sys.argv[1]
task_ids = sys.argv[2:]

# Match task table rows: | T?<digits>.<digits>.<digits> | ... |
# Both seed formats: "T3.2.1" (master-plan-new) and "3.2.1" (master-plan-extend).
task_row_re = re.compile(r'^\| T?\d+\.\d+\.\d+\s*\|')

lines = body.split('\n')
tid_iter = iter(task_ids)
flipped = 0
result = []

for line in lines:
    if task_row_re.match(line):
        cols = line.split('|')
        # cols[0]='' cols[1]=Task cols[2]=Name cols[3]=Issue cols[4]=Status cols[5]=Intent cols[6]=''
        # Issue col may seed as "_pending_" (master-plan-new) or "TBD" (master-plan-extend).
        if len(cols) >= 5 and cols[3].strip() in ('_pending_', 'TBD'):
            try:
                tid = next(tid_iter)
                cols[3] = f' {tid} '
                line = '|'.join(cols)
                flipped += 1
            except StopIteration:
                pass
    result.append(line)

print('\n'.join(result), end='')
sys.stderr.write(f'flipped={flipped}\n')
PYEOF
)

flipped_count=$(python3 -c "
import sys, re
body = open('/dev/stdin').read()
print(len(re.findall(r'TECH-|FEAT-|BUG-|ART-|AUDIO-', body)))
" <<< "$updated" 2>/dev/null || echo "0")

# Persist updated body via SQL.
escaped="${updated//\'/\'\'}"
psql "$DATABASE_URL" -c \
  "UPDATE ia_stages SET body = \$body\$${updated}\$body\$, updated_at = now() WHERE slug = '${slug//\'/\'\'}' AND stage_id = '${stage_norm//\'/\'\'}'" \
  2>/dev/null

echo "task_table_flipped=${flipped_count}"
exit 0
