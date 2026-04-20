#!/usr/bin/env bash
# claude-usage.sh — Claude Code token/time usage tracker + empirical quota estimator.
#
# Focus: tokens + time. Cost is STORED in the DB but HIDDEN from session output
# by default. Pass `-c` to the session subcommand to show cost columns.
#
# Storage: ~/.claude-personal/usage-data/claude-usage.db (SQLite).
# Zero runtime API calls. Pure shell + jq + sqlite3 on local transcripts.
#
# Subcommands:
#   session     — display session totals (default if no subcommand)
#   ingest      — scan transcripts, upsert rows into DB
#   checkpoint  — record observed web-dashboard %
#   anchor      — set week reset timestamp
#   week        — tokens this week + estimate from latest checkpoint
#   calibrate   — compute tokens-per-pct from checkpoint history
#   db          — db path / schema / stats / dump
#   help        — show help
#
# Pricing (USD per M tokens, 2026-01). Edit RATES_JSON when Anthropic changes rates.

set -euo pipefail

DB_DIR="${CLAUDE_USAGE_DB_DIR:-$HOME/.claude-personal/usage-data}"
DB_PATH="$DB_DIR/claude-usage.db"

command -v jq >/dev/null      || { echo "claude-usage: jq required (brew install jq)" >&2; exit 2; }
command -v sqlite3 >/dev/null || { echo "claude-usage: sqlite3 required" >&2; exit 2; }

RATES_JSON='
{
  "claude-opus-4-7":   {"in":15,  "out":75, "cr":1.50, "cw5":18.75, "cw1h":30},
  "claude-opus-4-6":   {"in":15,  "out":75, "cr":1.50, "cw5":18.75, "cw1h":30},
  "claude-sonnet-4-6": {"in":3,   "out":15, "cr":0.30, "cw5":3.75,  "cw1h":6},
  "claude-sonnet-4-5": {"in":3,   "out":15, "cr":0.30, "cw5":3.75,  "cw1h":6},
  "claude-haiku-4-5":  {"in":1,   "out":5,  "cr":0.10, "cw5":1.25,  "cw1h":2}
}'

# Per-transcript aggregation: emits one JSON object per file (or empty if no usage turns).
AGGREGATE_JQ='
  [.[] | select(.message.usage != null) | {
    model: (.message.model // "unknown" | sub("-[0-9]{8}$"; "")),
    ts:    (.timestamp // ""),
    in:    (.message.usage.input_tokens // 0),
    out:   (.message.usage.output_tokens // 0),
    cr:    (.message.usage.cache_read_input_tokens // 0),
    cw5:   (.message.usage.cache_creation.ephemeral_5m_input_tokens // 0),
    cw1h:  (.message.usage.cache_creation.ephemeral_1h_input_tokens // 0)
  }] as $turns |
  if ($turns | length) == 0 then empty else
    def r(m): $rates[m] // null;
    def tc(t; x): if x == null then 0 else ((t.in*x.in)+(t.out*x.out)+(t.cr*x.cr)+(t.cw5*x.cw5)+(t.cw1h*x.cw1h))/1000000 end;
    ($turns | group_by(.model) | map({
      model: .[0].model,
      turns: length,
      in:   ([.[].in]  | add),
      out:  ([.[].out] | add),
      cr:   ([.[].cr]  | add),
      cw5:  ([.[].cw5] | add),
      cw1h: ([.[].cw1h]| add)
    }) | map(. + {cost: tc(.; r(.model))})) as $m |
    ($turns | map(.ts) | sort) as $ts |
    {
      session_id: $sid,
      config_dir: $cd,
      project_slug: $sl,
      first_ts: ($ts | first),
      last_ts:  ($ts | last),
      duration_s: (((($ts | last | sub("\\.[0-9]+Z$"; "Z") | fromdateiso8601) - ($ts | first | sub("\\.[0-9]+Z$"; "Z") | fromdateiso8601))) | floor),
      turns: ($turns | length),
      in_total:   ($m | [.[].in]   | add),
      out_total:  ($m | [.[].out]  | add),
      cr_total:   ($m | [.[].cr]   | add),
      cw5_total:  ($m | [.[].cw5]  | add),
      cw1h_total: ($m | [.[].cw1h] | add),
      cost_total: ($m | [.[].cost] | add),
      models: $m
    }
  end'

stat_mtime() { stat -f %m "$1" 2>/dev/null || stat -c %Y "$1" 2>/dev/null || echo 0; }

format_duration() {
  local s="${1:-0}"
  if (( s < 60 )); then printf '%ds' "$s"
  elif (( s < 3600 )); then printf '%dm%ds' $((s/60)) $((s%60))
  else printf '%dh%dm' $((s/3600)) $(( (s%3600)/60 ))
  fi
}

# --- DB init (idempotent) ---
db_init() {
  mkdir -p "$DB_DIR"
  sqlite3 "$DB_PATH" <<'SQL'
CREATE TABLE IF NOT EXISTS sessions (
  session_id      TEXT PRIMARY KEY,
  config_dir      TEXT NOT NULL,
  project_slug    TEXT NOT NULL,
  first_ts        TEXT NOT NULL,
  last_ts         TEXT NOT NULL,
  duration_s      INTEGER NOT NULL,
  turns           INTEGER NOT NULL,
  input_tokens    INTEGER NOT NULL,
  output_tokens   INTEGER NOT NULL,
  cache_read      INTEGER NOT NULL,
  cache_write_5m  INTEGER NOT NULL,
  cache_write_1h  INTEGER NOT NULL,
  cost_usd        REAL NOT NULL,
  models_json     TEXT NOT NULL,
  ingested_at     TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_sessions_first_ts ON sessions(first_ts);
CREATE INDEX IF NOT EXISTS idx_sessions_project  ON sessions(project_slug);

CREATE TABLE IF NOT EXISTS checkpoints (
  id     INTEGER PRIMARY KEY AUTOINCREMENT,
  ts     TEXT NOT NULL,
  pct    REAL NOT NULL,
  scope  TEXT NOT NULL DEFAULT 'overall',
  note   TEXT
);
CREATE INDEX IF NOT EXISTS idx_checkpoints_ts    ON checkpoints(ts);
CREATE INDEX IF NOT EXISTS idx_checkpoints_scope ON checkpoints(scope, ts);

CREATE TABLE IF NOT EXISTS week_anchors (
  id        INTEGER PRIMARY KEY AUTOINCREMENT,
  reset_ts  TEXT NOT NULL,
  note      TEXT
);
SQL
}

# --- Collect transcript paths ---
# If $1 is a repo path, return that repo's slug'd dirs; else return everything.
collect_files() {
  local repo_path="${1:-}"
  local dirs=( "$HOME/.claude/projects" "$HOME/.claude-personal/projects" )
  if [[ -n "$repo_path" ]]; then
    local slug="${repo_path//\//-}"
    for base in "${dirs[@]}"; do
      [[ -d "$base/$slug" ]] || continue
      ls -1 "$base/$slug"/*.jsonl 2>/dev/null || true
    done
  else
    for base in "${dirs[@]}"; do
      [[ -d "$base" ]] || continue
      find "$base" -maxdepth 2 -name '*.jsonl' 2>/dev/null || true
    done
  fi
}

aggregate_file() {
  local f="$1"
  local sid; sid=$(basename "$f" .jsonl)
  local cd;  if [[ "$f" == *"/.claude-personal/"* ]]; then cd="personal"; else cd="default"; fi
  local sl;  sl=$(basename "$(dirname "$f")")
  jq -s --arg sid "$sid" --arg cd "$cd" --arg sl "$sl" --argjson rates "$RATES_JSON" "$AGGREGATE_JQ" < "$f" 2>/dev/null || true
}

# --- session subcommand ---
cmd_session() {
  local mode="latest" session_id="" repo_path="" json_out=0 show_cost=0
  while getopts ":as:r:jch" opt; do
    case $opt in
      a) mode="all" ;;
      s) mode="session"; session_id="$OPTARG" ;;
      r) repo_path="$OPTARG" ;;
      j) json_out=1 ;;
      c) show_cost=1 ;;
      h) cat <<'EOF'
usage: claude-usage.sh session [-a] [-s ID] [-r REPO] [-j] [-c]

  -a         All sessions for the repo.
  -s ID      Specific session id (prefix ok).
  -r PATH    Repo path (default: git toplevel or pwd).
  -j         JSON output.
  -c         Include cost column (hidden by default — tokens+time focus).
EOF
        return 0 ;;
      *) echo "session: invalid option" >&2; return 2 ;;
    esac
  done

  : "${repo_path:=$(git rev-parse --show-toplevel 2>/dev/null || pwd)}"
  repo_path="${repo_path%/}"
  local slug="${repo_path//\//-}"

  local files=()
  while IFS= read -r f; do [[ -n "$f" ]] && files+=("$f"); done < <(collect_files "$repo_path")
  (( ${#files[@]} > 0 )) || { echo "claude-usage: no transcripts for slug $slug" >&2; return 1; }

  local targets=()
  case "$mode" in
    latest)
      local latest="" lt=0 t
      for f in "${files[@]}"; do t=$(stat_mtime "$f"); (( t > lt )) && { lt=$t; latest="$f"; }; done
      [[ -n "$latest" ]] || { echo "no latest session" >&2; return 1; }
      targets=( "$latest" ) ;;
    all)
      targets=( "${files[@]}" ) ;;
    session)
      local match=""
      for f in "${files[@]}"; do
        [[ "$(basename "$f" .jsonl)" == "$session_id"* ]] && match="$f" && break
      done
      [[ -n "$match" ]] || { echo "session not found: $session_id" >&2; return 1; }
      targets=( "$match" ) ;;
  esac

  local agg
  agg=$(cat "${targets[@]}" | jq -c --argjson rates "$RATES_JSON" '
    select(.message.usage != null) |
    {
      model: (.message.model // "unknown" | sub("-[0-9]{8}$"; "")),
      ts:    (.timestamp // ""),
      in:    (.message.usage.input_tokens // 0),
      out:   (.message.usage.output_tokens // 0),
      cr:    (.message.usage.cache_read_input_tokens // 0),
      cw5:   (.message.usage.cache_creation.ephemeral_5m_input_tokens // 0),
      cw1h:  (.message.usage.cache_creation.ephemeral_1h_input_tokens // 0)
    }' | jq -s --argjson rates "$RATES_JSON" '
    def sumf(f): [.[] | f] | add // 0;
    def cost(t; r):
      if r == null then null
      else ((t.in*r.in)+(t.out*r.out)+(t.cr*r.cr)+(t.cw5*r.cw5)+(t.cw1h*r.cw1h)) / 1000000
      end;
    if length == 0 then
      {first_ts:"", last_ts:"", duration_s:0, models:[], total:{turns:0,in:0,out:0,cr:0,cw5:0,cw1h:0,cost:0}}
    else
      (map(.ts) | sort) as $ts |
      group_by(.model) |
      map({
        model: .[0].model,
        turns: length,
        in:   sumf(.in),
        out:  sumf(.out),
        cr:   sumf(.cr),
        cw5:  sumf(.cw5),
        cw1h: sumf(.cw1h)
      }) |
      map(. + {cost: cost(.; $rates[.model])}) as $m |
      {
        first_ts: ($ts | first),
        last_ts:  ($ts | last),
        duration_s: (((($ts | last | sub("\\.[0-9]+Z$"; "Z") | fromdateiso8601) - ($ts | first | sub("\\.[0-9]+Z$"; "Z") | fromdateiso8601))) | floor),
        models: $m,
        total: {
          turns: ($m | sumf(.turns)),
          in:    ($m | sumf(.in)),
          out:   ($m | sumf(.out)),
          cr:    ($m | sumf(.cr)),
          cw5:   ($m | sumf(.cw5)),
          cw1h:  ($m | sumf(.cw1h)),
          cost:  ($m | [.[] | .cost // 0] | add)
        }
      }
    end
  ')

  if (( json_out )); then
    printf '%s\n' "$agg"
    return 0
  fi

  local n_files=${#targets[@]}
  printf 'claude-usage  mode=%s  files=%d\n' "$mode" "$n_files"
  if (( n_files <= 5 )); then
    for t in "${targets[@]}"; do printf '  %s\n' "$t"; done
  else
    printf '  %s\n' "${targets[0]}"
    printf '  ... (%d more)\n' $(( n_files - 2 ))
    printf '  %s\n' "${targets[$((n_files - 1))]}"
  fi

  local first last dur
  first=$(jq -r '.first_ts' <<<"$agg")
  last=$(jq -r  '.last_ts'  <<<"$agg")
  dur=$(jq -r   '.duration_s' <<<"$agg")
  [[ -n "$first" ]] && printf '  window: %s → %s  (%s)\n' "$first" "$last" "$(format_duration "$dur")"
  printf '\n'

  # Header + rows. Cost column only when -c is set.
  if (( show_cost )); then
    printf '%-22s %6s %10s %12s %12s %12s %12s %12s %10s\n' MODEL TURNS DUR INPUT CACHE-R CACHE-W5 CACHE-W1H OUTPUT COST
    printf '%-22s %6s %10s %12s %12s %12s %12s %12s %10s\n' "----------------------" "------" "--------" "------------" "------------" "------------" "------------" "------------" "----------"
  else
    printf '%-22s %6s %10s %12s %12s %12s %12s %12s\n' MODEL TURNS DUR INPUT CACHE-R CACHE-W5 CACHE-W1H OUTPUT
    printf '%-22s %6s %10s %12s %12s %12s %12s %12s\n' "----------------------" "------" "--------" "------------" "------------" "------------" "------------" "------------"
  fi

  jq -r '
    (.models[] | [.model, .turns, "", .in, .cr, .cw5, .cw1h, .out, (.cost // -1)] | @tsv),
    "---",
    (.total  | ["TOTAL", .turns, "__DUR__", .in, .cr, .cw5, .cw1h, .out, (.cost // -1)] | @tsv)
  ' <<<"$agg" | awk -F'\t' -v SC="$show_cost" -v DUR="$(format_duration "$dur")" '
    function human(n) {
      if (n == "" || n+0 == 0) return (n == "" ? "" : "0")
      if (n+0 >= 1000000) return sprintf("%.2fM", n/1000000)
      if (n+0 >= 1000)    return sprintf("%.1fk", n/1000)
      return sprintf("%d", n)
    }
    function cost(c) { if (c+0 < 0) return "—"; if (c+0 == 0) return "$0.00"; return sprintf("$%.2f", c) }
    $1 == "---" {
      if (SC == "1") printf "%-22s %6s %10s %12s %12s %12s %12s %12s %10s\n", "----------------------", "------", "--------", "------------", "------------", "------------", "------------", "------------", "----------"
      else           printf "%-22s %6s %10s %12s %12s %12s %12s %12s\n",      "----------------------", "------", "--------", "------------", "------------", "------------", "------------", "------------"
      next
    }
    {
      dur_cell = ($3 == "__DUR__" ? DUR : $3)
      if (SC == "1") printf "%-22s %6s %10s %12s %12s %12s %12s %12s %10s\n", $1, $2, dur_cell, human($4), human($5), human($6), human($7), human($8), cost($9)
      else           printf "%-22s %6s %10s %12s %12s %12s %12s %12s\n",      $1, $2, dur_cell, human($4), human($5), human($6), human($7), human($8)
    }'
}

# --- ingest subcommand ---
cmd_ingest() {
  db_init
  local repo_path=""
  while getopts ":r:h" opt; do
    case $opt in
      r) repo_path="$OPTARG" ;;
      h) echo "usage: claude-usage.sh ingest [-r REPO]"; return 0 ;;
      *) echo "ingest: invalid option" >&2; return 2 ;;
    esac
  done

  local files=()
  while IFS= read -r f; do [[ -n "$f" ]] && files+=("$f"); done < <(collect_files "$repo_path")
  local total=${#files[@]}
  (( total > 0 )) || { echo "no transcripts found"; return 1; }

  echo "ingesting $total transcripts..."
  local tmp_tsv; tmp_tsv=$(mktemp -t claude-usage.XXXXXX)
  trap 'rm -f "$tmp_tsv"' RETURN

  local count=0 skipped=0 i=0
  for f in "${files[@]}"; do
    i=$((i+1))
    local agg; agg=$(aggregate_file "$f")
    if [[ -z "$agg" ]]; then skipped=$((skipped+1)); continue; fi
    jq -r '
      [.session_id, .config_dir, .project_slug, .first_ts, .last_ts, .duration_s, .turns, .in_total, .out_total, .cr_total, .cw5_total, .cw1h_total, .cost_total, (.models|tojson)]
      | @tsv
    ' <<<"$agg" >> "$tmp_tsv"
    count=$((count+1))
    if (( i % 100 == 0 )); then printf '  %d/%d processed...\n' "$i" "$total" >&2; fi
  done

  sqlite3 "$DB_PATH" <<EOF
CREATE TEMP TABLE _staging (
  session_id TEXT, config_dir TEXT, project_slug TEXT, first_ts TEXT, last_ts TEXT,
  duration_s INTEGER, turns INTEGER, input_tokens INTEGER, output_tokens INTEGER,
  cache_read INTEGER, cache_write_5m INTEGER, cache_write_1h INTEGER, cost_usd REAL,
  models_json TEXT
);
.mode tabs
.import '$tmp_tsv' _staging
INSERT OR REPLACE INTO sessions (session_id, config_dir, project_slug, first_ts, last_ts, duration_s, turns, input_tokens, output_tokens, cache_read, cache_write_5m, cache_write_1h, cost_usd, models_json, ingested_at)
SELECT session_id, config_dir, project_slug, first_ts, last_ts, duration_s, turns, input_tokens, output_tokens, cache_read, cache_write_5m, cache_write_1h, cost_usd, models_json, datetime('now')
FROM _staging;
EOF

  echo "ingest: $count upserted, $skipped empty skipped → $DB_PATH"
}

# SQL-escape a string (single-quote → double single-quote).
sql_q() { printf '%s' "$1" | sed "s/'/''/g"; }

# --- checkpoint subcommand ---
cmd_checkpoint() {
  db_init
  local pct="${1:-}"; shift 2>/dev/null || true
  local scope="overall" note="" ts_iso=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --scope) scope="$2"; shift 2 ;;
      --note)  note="$2"; shift 2 ;;
      --at)    ts_iso="$2"; shift 2 ;;
      -h|--help)
        cat <<'EOF'
usage: claude-usage.sh checkpoint PCT [--scope overall|opus|sonnet|haiku] [--note TEXT] [--at ISO_TS]

  PCT         Observed web-dashboard percentage (0-100).
  --scope     Which bar you read (default: overall).
  --note      Free-text note (context for future self).
  --at        Override timestamp (default: now, UTC).
EOF
        return 0 ;;
      *) echo "checkpoint: unknown arg $1" >&2; return 2 ;;
    esac
  done
  [[ -n "$pct" ]] || { echo "usage: claude-usage.sh checkpoint PCT [--scope X] [--note T] [--at ISO]" >&2; return 2; }
  [[ -z "$ts_iso" ]] && ts_iso=$(date -u +%Y-%m-%dT%H:%M:%SZ)

  local note_sql
  if [[ -n "$note" ]]; then note_sql="'$(sql_q "$note")'"; else note_sql="NULL"; fi

  sqlite3 "$DB_PATH" "INSERT INTO checkpoints (ts, pct, scope, note) VALUES ('$(sql_q "$ts_iso")', $pct, '$(sql_q "$scope")', $note_sql);"
  echo "checkpoint saved: $ts_iso  scope=$scope  pct=$pct${note:+  note=\"$note\"}"
}

# --- anchor subcommand ---
cmd_anchor() {
  db_init
  local reset_ts="${1:-now}"; shift 2>/dev/null || true
  local note=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --note) note="$2"; shift 2 ;;
      -h|--help)
        cat <<'EOF'
usage: claude-usage.sh anchor [ISO_TS|now] [--note TEXT]

Sets the start of your weekly quota window. The anchor advances in 7-day
steps automatically; set this once (to the start of your current week).
EOF
        return 0 ;;
      *) echo "anchor: unknown arg $1" >&2; return 2 ;;
    esac
  done
  [[ "$reset_ts" == "now" ]] && reset_ts=$(date -u +%Y-%m-%dT%H:%M:%SZ)
  local note_sql
  if [[ -n "$note" ]]; then note_sql="'$(sql_q "$note")'"; else note_sql="NULL"; fi
  sqlite3 "$DB_PATH" "INSERT INTO week_anchors (reset_ts, note) VALUES ('$(sql_q "$reset_ts")', $note_sql);"
  echo "week anchor saved: $reset_ts"
}

# Convert ISO8601 → epoch seconds via sqlite (portable).
iso_to_epoch() { sqlite3 "$DB_PATH" "SELECT CAST(strftime('%s', '$(sql_q "$1")') AS INTEGER);"; }
epoch_to_iso() { sqlite3 "$DB_PATH" "SELECT strftime('%Y-%m-%dT%H:%M:%SZ', $1, 'unixepoch');"; }

# --- week subcommand ---
cmd_week() {
  db_init
  local scope="overall" json_out=0
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --scope) scope="$2"; shift 2 ;;
      --json)  json_out=1; shift ;;
      -h|--help)
        cat <<'EOF'
usage: claude-usage.sh week [--scope overall|opus|sonnet|haiku] [--json]

Reports tokens consumed in the current weekly window (derived from the latest
anchor + 7-day steps) and — if ≥1 checkpoint exists for scope — an estimated
percentage now based on tokens-per-pct calibration.
EOF
        return 0 ;;
      *) echo "week: unknown arg $1" >&2; return 2 ;;
    esac
  done

  local anchor
  anchor=$(sqlite3 "$DB_PATH" "SELECT reset_ts FROM week_anchors ORDER BY reset_ts DESC LIMIT 1;")
  [[ -n "$anchor" ]] || { echo "no week anchor set. run: claude-usage.sh anchor <ISO|now>" >&2; return 1; }

  local ws_epoch; ws_epoch=$(iso_to_epoch "$anchor")
  local now_epoch; now_epoch=$(date -u +%s)
  while (( ws_epoch + 7*86400 <= now_epoch )); do ws_epoch=$(( ws_epoch + 7*86400 )); done
  local we_epoch=$(( ws_epoch + 7*86400 ))
  local ws_iso; ws_iso=$(epoch_to_iso "$ws_epoch")
  local we_iso; we_iso=$(epoch_to_iso "$we_epoch")

  # Pull all sessions in the week window; aggregate per scope via jq.
  local rows
  rows=$(sqlite3 "$DB_PATH" -json "SELECT turns, duration_s, input_tokens, output_tokens, cache_read, cache_write_5m, cache_write_1h, cost_usd, models_json FROM sessions WHERE first_ts >= '$(sql_q "$ws_iso")' AND first_ts < '$(sql_q "$we_iso")';" 2>/dev/null || echo "[]")
  [[ -n "$rows" ]] || rows="[]"

  local summary
  summary=$(jq --arg scope "$scope" '
    def model_match($m; $s):
      if $s == "overall" then true
      elif $s == "opus"   then ($m | startswith("claude-opus"))
      elif $s == "sonnet" then ($m | startswith("claude-sonnet"))
      elif $s == "haiku"  then ($m | startswith("claude-haiku"))
      else ($m == $s) end;

    if $scope == "overall" then
      {
        scope: "overall",
        sessions: length,
        turns:          (map(.turns) | add // 0),
        duration_s:     (map(.duration_s) | add // 0),
        input:          (map(.input_tokens) | add // 0),
        output:         (map(.output_tokens) | add // 0),
        cache_read:     (map(.cache_read) | add // 0),
        cache_write_5m: (map(.cache_write_5m) | add // 0),
        cache_write_1h: (map(.cache_write_1h) | add // 0),
        cost_usd:       (map(.cost_usd) | add // 0)
      }
    else
      [.[] | {row: ., models: (.models_json | fromjson)}] as $joined |
      ($joined | map(.models[] | select(model_match(.model; $scope)))) as $mm |
      {
        scope: $scope,
        sessions: ($joined | map(select(.models[] | model_match(.model; $scope))) | length),
        turns:          ($mm | map(.turns) | add // 0),
        duration_s:     0,
        input:          ($mm | map(.in)   | add // 0),
        output:         ($mm | map(.out)  | add // 0),
        cache_read:     ($mm | map(.cr)   | add // 0),
        cache_write_5m: ($mm | map(.cw5)  | add // 0),
        cache_write_1h: ($mm | map(.cw1h) | add // 0),
        cost_usd:       ($mm | map(.cost) | add // 0)
      }
    end
  ' <<<"$rows")

  # Latest checkpoint for this scope
  local latest_cp
  latest_cp=$(sqlite3 "$DB_PATH" -json "SELECT ts, pct, note FROM checkpoints WHERE scope='$(sql_q "$scope")' ORDER BY ts DESC LIMIT 1;" 2>/dev/null || echo "[]")
  [[ -n "$latest_cp" ]] || latest_cp="[]"

  # Average tokens/pct across all checkpoint pairs in this scope
  local tpp
  tpp=$(sqlite3 "$DB_PATH" "
WITH cps AS (
  SELECT ts, pct,
         LAG(ts)  OVER (ORDER BY ts) AS prev_ts,
         LAG(pct) OVER (ORDER BY ts) AS prev_pct
  FROM checkpoints WHERE scope='$(sql_q "$scope")'
)
SELECT COALESCE(AVG(CAST((
  SELECT COALESCE(SUM(input_tokens+output_tokens+cache_read+cache_write_5m+cache_write_1h), 0)
  FROM sessions WHERE first_ts >= prev_ts AND first_ts < ts
) AS REAL) / (pct - prev_pct)), 0)
FROM cps WHERE prev_ts IS NOT NULL AND pct > prev_pct;
")
  [[ -z "$tpp" || "$tpp" == "0" || "$tpp" == "0.0" ]] && tpp=""

  if (( json_out )); then
    jq -n --argjson summary "$summary" --argjson cp "$latest_cp" \
          --arg start "$ws_iso" --arg end "$we_iso" --arg tpp "${tpp:-null}" '
      {
        week_start: $start,
        week_end: $end,
        summary: $summary,
        latest_checkpoint: ($cp[0] // null),
        tokens_per_pct: (if $tpp == "null" or $tpp == "" then null else ($tpp | tonumber) end)
      }'
    return 0
  fi

  printf 'claude-usage week  scope=%s\n' "$scope"
  printf '  window: %s → %s\n\n' "$ws_iso" "$we_iso"

  local total
  total=$(jq -r '.input + .output + .cache_read + .cache_write_5m + .cache_write_1h' <<<"$summary")
  local dur
  dur=$(jq -r '.duration_s' <<<"$summary")

  printf '  sessions       : %s\n' "$(jq -r .sessions <<<"$summary")"
  printf '  turns          : %s\n' "$(jq -r .turns <<<"$summary")"
  if [[ "$scope" == "overall" ]]; then
    printf '  duration       : %s\n' "$(format_duration "$dur")"
  fi
  printf '  input          : %s\n' "$(jq -r .input <<<"$summary")"
  printf '  output         : %s\n' "$(jq -r .output <<<"$summary")"
  printf '  cache-read     : %s\n' "$(jq -r .cache_read <<<"$summary")"
  printf '  cache-write-5m : %s\n' "$(jq -r .cache_write_5m <<<"$summary")"
  printf '  cache-write-1h : %s\n' "$(jq -r .cache_write_1h <<<"$summary")"
  printf '  total tokens   : %s\n' "$total"

  echo
  local cp_pct cp_ts
  cp_pct=$(jq -r '.[0].pct // empty' <<<"$latest_cp")
  cp_ts=$(jq -r  '.[0].ts // empty'  <<<"$latest_cp")
  if [[ -z "$cp_pct" ]]; then
    printf '  no checkpoint yet for scope=%s. run: claude-usage.sh checkpoint PCT --scope %s\n' "$scope" "$scope"
  else
    printf '  last checkpoint: %s  pct=%s\n' "$cp_ts" "$cp_pct"
    local since_tokens
    since_tokens=$(sqlite3 "$DB_PATH" "SELECT COALESCE(SUM(input_tokens+output_tokens+cache_read+cache_write_5m+cache_write_1h), 0) FROM sessions WHERE first_ts >= '$(sql_q "$cp_ts")' AND first_ts < '$(sql_q "$we_iso")';")
    printf '  tokens since   : %s\n' "$since_tokens"
    if [[ -n "$tpp" ]]; then
      local est_now
      est_now=$(awk -v p="$cp_pct" -v t="$since_tokens" -v tpp="$tpp" 'BEGIN{printf "%.1f", p + t/tpp}')
      printf '  tokens/pct     : %.0f (calibration avg)\n' "$tpp"
      printf '  est pct now    : %s%%\n' "$est_now"
    else
      printf '  (need ≥2 checkpoints with pct increase to calibrate tokens/pct)\n'
    fi
  fi
}

# --- calibrate subcommand ---
cmd_calibrate() {
  db_init
  local scope="overall"
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --scope) scope="$2"; shift 2 ;;
      -h|--help)
        cat <<'EOF'
usage: claude-usage.sh calibrate [--scope overall|opus|sonnet|haiku]

Lists all checkpoint pairs (sorted by ts) with tokens consumed between them
and tokens/pct ratio. Reports average tokens/pct and estimated weekly cap.
EOF
        return 0 ;;
      *) echo "calibrate: unknown arg $1" >&2; return 2 ;;
    esac
  done

  local rows
  rows=$(sqlite3 "$DB_PATH" -json "
WITH cps AS (
  SELECT ts, pct,
         LAG(ts)  OVER (ORDER BY ts) AS prev_ts,
         LAG(pct) OVER (ORDER BY ts) AS prev_pct
  FROM checkpoints WHERE scope='$(sql_q "$scope")'
)
SELECT prev_ts, ts, prev_pct, pct,
  (SELECT COALESCE(SUM(input_tokens+output_tokens+cache_read+cache_write_5m+cache_write_1h), 0)
   FROM sessions WHERE first_ts >= prev_ts AND first_ts < ts) AS tokens
FROM cps
WHERE prev_ts IS NOT NULL AND pct > prev_pct;
")
  [[ -n "$rows" ]] || rows="[]"

  if [[ "$(jq length <<<"$rows")" -eq 0 ]]; then
    printf 'calibrate  scope=%s\n  no pairs yet (need ≥2 checkpoints with pct increase).\n' "$scope"
    printf '  run: claude-usage.sh checkpoint PCT --scope %s\n' "$scope"
    return 0
  fi

  printf 'calibrate  scope=%s\n\n' "$scope"
  printf '%-22s → %-22s %8s %14s %14s\n' FROM_TS TO_TS D_PCT TOKENS TOK/PCT
  printf -- '----------------------------------------------------------------------------------------------\n'
  jq -r '.[] | [.prev_ts, .ts, (.pct - .prev_pct), .tokens, (.tokens / (.pct - .prev_pct))] | @tsv' <<<"$rows" |
    awk -F'\t' '{ printf "%-22s → %-22s %8.1f %14d %14d\n", $1, $2, $3, $4, $5 }'

  local avg cap
  avg=$(jq '[.[] | .tokens / (.pct - .prev_pct)] | add / length' <<<"$rows")
  cap=$(awk -v t="$avg" 'BEGIN{printf "%.0f", t*100}')
  printf '\n  avg tokens/pct : %.0f\n' "$avg"
  printf   '  est weekly cap : %s tokens  (avg × 100)\n' "$cap"
}

# --- db subcommand ---
cmd_db() {
  db_init
  local what="${1:-path}"
  case "$what" in
    path)   echo "$DB_PATH" ;;
    schema) sqlite3 "$DB_PATH" ".schema" ;;
    dump)   sqlite3 "$DB_PATH" ".dump" ;;
    stats)
      sqlite3 "$DB_PATH" "
SELECT 'sessions:    ' || COUNT(*) || '  (span: ' || COALESCE(MIN(first_ts), '—') || ' → ' || COALESCE(MAX(last_ts), '—') || ')' FROM sessions
UNION ALL SELECT 'checkpoints: ' || COUNT(*) FROM checkpoints
UNION ALL SELECT 'anchors:     ' || COUNT(*) FROM week_anchors;
" ;;
    *) echo "db: unknown subcommand: $what. try: path | schema | dump | stats" >&2; return 2 ;;
  esac
}

print_help() {
  cat <<'EOF'
claude-usage.sh — Claude Code token/time tracker + empirical quota estimator.

subcommands:
  session     Display session totals (default).
                -a         all sessions for repo
                -s ID      specific session (prefix ok)
                -r PATH    override repo path
                -j         JSON output
                -c         include cost column (hidden by default)
  ingest      Scan transcripts, upsert into SQLite DB.
                -r PATH    limit to one repo (default: all repos)
  checkpoint  Record observed web-dashboard %.
                PCT [--scope overall|opus|sonnet|haiku] [--note T] [--at ISO]
  anchor      Set week reset timestamp.
                [ISO_TS|now] [--note T]
  week        Current-week tokens + estimate from latest checkpoint.
                [--scope overall|opus|sonnet|haiku] [--json]
  calibrate   Checkpoint pairs + tokens/pct + estimated cap.
                [--scope overall|opus|sonnet|haiku]
  db          path | schema | dump | stats

storage: ~/.claude-personal/usage-data/claude-usage.db
cost is STORED but hidden by default (pass -c to session to display).
EOF
}

# --- dispatch ---
SUBCMD="${1:-session}"
case "$SUBCMD" in
  session|ingest|checkpoint|anchor|week|calibrate|db)
    (( $# > 0 )) && shift
    cmd_"$SUBCMD" "$@"
    ;;
  help|-h|--help)
    print_help
    ;;
  -*)  # legacy flag form defaults to session
    cmd_session "$@"
    ;;
  *)
    echo "claude-usage: unknown subcommand: $SUBCMD" >&2
    print_help >&2
    exit 2
    ;;
esac
