#!/usr/bin/env bash
# ship-cycle Pass B Phase B.2 — read verify-loop verdict file written by Pass A.
#
# Pass A's verify-loop subagent writes a JSON verdict to a tmp file before
# returning to the main session. Pass B recipe re-reads that file as the
# pass/fail gate. Stops on `fail` or missing/malformed verdict.
#
# Args:
#   --slug         <plan-slug>
#   --stage-id     <X.Y>
#   --verdict-file <optional override; defaults to /tmp/ship-cycle-verify-{slug}-{stage_id}.json>
#
# Verdict file shape: {"verdict":"pass|fail","reason":"...","duration_ms":N}
#
# Output (stdout, exit 0): JSON line `{"verdict":"pass","reason":"..."}`.
# Exit 1 (stderr): file missing / malformed / verdict=fail.
set -euo pipefail

slug=""
stage_id=""
verdict_file=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --slug)                       slug="$2";         shift 2 ;;
    --stage-id|--stage_id)        stage_id="$2";     shift 2 ;;
    --verdict-file|--verdict_file) verdict_file="$2"; shift 2 ;;
    *) shift ;;
  esac
done

if [[ -z "$slug" || -z "$stage_id" ]]; then
  echo "verify-loop-check: missing --slug or --stage-id" >&2
  exit 1
fi

if [[ -z "$verdict_file" ]]; then
  verdict_file="/tmp/ship-cycle-verify-${slug}-${stage_id}.json"
fi

if [[ ! -f "$verdict_file" ]]; then
  echo "verify-loop-check: verdict file not found: $verdict_file (Pass A verify-loop did not run or did not persist verdict)" >&2
  exit 1
fi

verdict=$(node -e '
  const fs = require("node:fs");
  try {
    const j = JSON.parse(fs.readFileSync(process.argv[1], "utf8"));
    if (typeof j.verdict !== "string") { console.error("malformed verdict file: missing verdict field"); process.exit(2); }
    process.stdout.write(j.verdict);
  } catch (e) {
    console.error("verify-loop-check: " + e.message);
    process.exit(2);
  }
' "$verdict_file" 2>&1) || {
  echo "verify-loop-check: failed to parse verdict file: $verdict_file" >&2
  exit 1
}

if [[ "$verdict" != "pass" ]]; then
  echo "verify-loop-check: verdict=${verdict} — STOP. Fix issues + re-run /ship-cycle." >&2
  exit 1
fi

# Echo full verdict file as recipe step output.
cat "$verdict_file"
exit 0
