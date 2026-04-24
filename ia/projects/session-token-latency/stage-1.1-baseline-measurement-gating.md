### Stage 1.1 — Baseline measurement (gating)


**Status:** Final

**Objectives:** Author telemetry collection tooling and run ≥10 representative sessions to produce the aggregate baseline floor that gates Stage 1.2 entry. No per-theme attribution at this stage — single aggregate only.

**Exit:**

- `tools/scripts/agent-telemetry/baseline-collect.sh` executes without error; appends valid JSONL to `.claude/telemetry/{session-id}.jsonl`.
- `npm run validate:telemetry-schema` passes against a sample JSONL file.
- `tools/scripts/agent-telemetry/baseline-summary.json` committed; p50/p95/p99 present for all 6 required metrics (≥10 sessions).
- `.gitignore` updated: `*.jsonl` raw files excluded; summary JSON tracked.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T1.1.1 | Baseline collect script | **TECH-510** | Done | Author `tools/scripts/agent-telemetry/baseline-collect.sh` (new dir): reads `DEBUG_MCP_COMPUTE` stderr + PostToolUse hook stdout; appends JSONL to `.claude/telemetry/{session-id}.jsonl` with fields `ts`, `session_id`, `total_input_tokens`, `cache_read_tokens`, `cache_write_tokens`, `mcp_cold_start_ms`, `hook_fork_count`, `hook_fork_total_ms`. Update `.gitignore`: `.claude/telemetry/*.jsonl` excluded, `*-summary.json` tracked. |
| T1.1.2 | Telemetry schema validator | **TECH-511** | Done | Author `npm run validate:telemetry-schema` in `package.json` `scripts`: reads any `.claude/telemetry/*.jsonl` sample; asserts all 8 required fields present + typed correctly; exits non-zero on schema mismatch. Wire into `validate:all` composition in root `package.json`. |
| T1.1.3 | Baseline collection run | **TECH-512** | Done | Execute ≥10 representative sessions (mix of `/implement`, `/ship`, `/stage-file` lifecycle seams) with `baseline-collect.sh` active. Aggregate raw JSONL to `tools/scripts/agent-telemetry/baseline-summary.json` (p50/p95/p99 per metric); commit. No per-theme attribution — single aggregate floor only. |
| T1.1.4 | Gate validation + provenance | **TECH-513** | Done | Validate `baseline-summary.json` schema via `npm run validate:telemetry-schema`; assert all 6 metric keys present. Append measurement provenance (session count, date range, model, seam mix) to `docs/session-token-latency-audit-exploration.md` §Provenance. Confirm `npm run validate:all` green. Stage 1.2 entry conditional on this task Done. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-510"
  task_key: "T1.1.1"
  title: "Baseline collect script"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Author tools/scripts/agent-telemetry/baseline-collect.sh (new dir). Reads DEBUG_MCP_COMPUTE
    stderr + PostToolUse hook stdout; appends JSONL to .claude/telemetry/{session-id}.jsonl with
    8 fields (ts, session_id, total_input_tokens, cache_read_tokens, cache_write_tokens,
    mcp_cold_start_ms, hook_fork_count, hook_fork_total_ms). Update .gitignore: raw .jsonl
    excluded, *-summary.json tracked. Foundation for Stage 1.1 gate — T1.1.2/T1.1.3/T1.1.4
    consume its output.
  depends_on: []
  related:
    - "T1.1.2"
    - "T1.1.3"
    - "T1.1.4"
  stub_body:
    summary: |
      Author baseline-collect.sh shell harness emitting session-scoped telemetry JSONL under
      .claude/telemetry/. Establishes capture format for all six baseline metrics gating
      Stage 1.2 entry. No per-theme attribution — aggregate floor only.
    goals: |
      - New dir tools/scripts/agent-telemetry/ with baseline-collect.sh (executable).
      - JSONL schema: 8 fields, one line per measurement event.
      - .gitignore tracks summary JSON, excludes raw .jsonl.
      - Script runs clean against a dummy session (no hook env → zero-row append, exit 0).
    systems_map: |
      New: tools/scripts/agent-telemetry/baseline-collect.sh.
      Touches: .gitignore (root).
      Reads: DEBUG_MCP_COMPUTE stderr, PostToolUse hook stdout (env-passed).
      Writes: .claude/telemetry/{session-id}.jsonl (new dir, gitignored).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 1 — Script + gitignore.
      1. mkdir tools/scripts/agent-telemetry.
      2. Author baseline-collect.sh: parse hook env, compute args per 8-field schema, append JSONL.
      3. chmod +x; smoke test with synthetic session-id.
      4. Edit .gitignore: add `.claude/telemetry/*.jsonl`; confirm *-summary.json not shadowed.
      5. npm run validate:all.
```

```yaml
- reserved_id: "TECH-511"
  task_key: "T1.1.2"
  title: "Telemetry schema validator"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Author `npm run validate:telemetry-schema` in package.json scripts. Reads any
    .claude/telemetry/*.jsonl sample; asserts all 8 required fields present + typed correctly;
    exits non-zero on schema mismatch. Wire into validate:all composition in root package.json.
    Gates T1.1.4 provenance step.
  depends_on: []
  related:
    - "T1.1.1"
    - "T1.1.3"
    - "T1.1.4"
  stub_body:
    summary: |
      Ship JSONL schema validator script reachable via `npm run validate:telemetry-schema`.
      Asserts 8-field shape authored by T1.1.1. Becomes a validate:all dependency so CI fails
      on malformed baseline captures.
    goals: |
      - validate:telemetry-schema script added to package.json.
      - Script iterates .claude/telemetry/*.jsonl, asserts each line parses + carries 8 typed fields.
      - Wired into validate:all chain.
      - Non-zero exit on missing field / type mismatch; zero exit on empty dir (graceful).
    systems_map: |
      New: tools/scripts/validate-telemetry-schema.{sh|mjs}.
      Touches: package.json (scripts section).
      Reads: .claude/telemetry/*.jsonl.
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 1 — Validator + wiring.
      1. Author validator script (bash with jq, or Node mjs) — 8-field assertion.
      2. Add "validate:telemetry-schema" to package.json scripts.
      3. Append to validate:all composition.
      4. Smoke: run on empty + synthetic JSONL.
      5. npm run validate:all.
```

```yaml
- reserved_id: "TECH-512"
  task_key: "T1.1.3"
  title: "Baseline collection run"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Execute ≥10 representative sessions (mix of /implement, /ship, /stage-file lifecycle seams)
    with baseline-collect.sh active. Aggregate raw JSONL to
    tools/scripts/agent-telemetry/baseline-summary.json (p50/p95/p99 per metric); commit.
    Single aggregate floor only — no per-theme attribution at this stage.
  depends_on: []
  related:
    - "T1.1.1"
    - "T1.1.2"
    - "T1.1.4"
  stub_body:
    summary: |
      Run ≥10 real lifecycle sessions under active collect.sh; aggregate raw JSONL to committed
      baseline-summary.json with p50/p95/p99 per metric. Produces the Stage 1.2 gating floor.
    goals: |
      - ≥10 session captures under .claude/telemetry/.
      - Aggregation script → tools/scripts/agent-telemetry/baseline-summary.json committed.
      - All 6 required metrics present with p50/p95/p99 keys.
      - Representative seam mix documented alongside.
    systems_map: |
      New: tools/scripts/agent-telemetry/aggregate-baseline.{sh|mjs} (if not folded into T1.1.1).
      Writes: tools/scripts/agent-telemetry/baseline-summary.json (committed).
      Consumes: .claude/telemetry/*.jsonl (raw, gitignored).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 2 — Collection + aggregation.
      1. Author aggregate-baseline.sh computing percentiles over raw JSONL.
      2. Run ≥10 sessions across /implement, /ship, /stage-file (log seam per session).
      3. Run aggregator; produce baseline-summary.json.
      4. Commit baseline-summary.json.
      5. npm run validate:all (schema gate via T1.1.2).
```

```yaml
- reserved_id: "TECH-513"
  task_key: "T1.1.4"
  title: "Gate validation + provenance"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Validate baseline-summary.json schema via `npm run validate:telemetry-schema`; assert all 6
    metric keys present. Append measurement provenance (session count, date range, model, seam
    mix) to docs/session-token-latency-audit-exploration.md §Provenance. Confirm validate:all
    green. Stage 1.2 entry conditional on this task Done.
  depends_on: []
  related:
    - "T1.1.1"
    - "T1.1.2"
    - "T1.1.3"
  stub_body:
    summary: |
      Final gating task for Stage 1.1. Schema-validates baseline-summary.json via T1.1.2 script,
      records provenance in the exploration doc, confirms validate:all green. Done flip unblocks
      Stage 1.2 MCP server split.
    goals: |
      - validate:telemetry-schema passes against baseline-summary.json.
      - All 6 metric keys asserted present (total_input_tokens, cache_read_tokens,
        cache_write_tokens, mcp_cold_start_ms, hook_fork_count, hook_fork_total_ms).
      - docs/session-token-latency-audit-exploration.md §Provenance appended with session count,
        date range, model, seam mix.
      - npm run validate:all green.
    systems_map: |
      Reads: tools/scripts/agent-telemetry/baseline-summary.json.
      Touches: docs/session-token-latency-audit-exploration.md (§Provenance only).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 2 — Gate + provenance.
      1. Run npm run validate:telemetry-schema; confirm zero exit.
      2. Sanity-check all 6 metric keys present in baseline-summary.json.
      3. Append §Provenance block to exploration doc (session count, date range, model, seam mix).
      4. npm run validate:all.
      5. Hand off: Stage 1.2 entry unblocked.
```

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

<!-- stage-closeout-plan output — do not hand-edit; apply via stage-closeout-apply -->
<!-- PARTIAL close: TECH-510 + TECH-511 only. TECH-512 / TECH-513 remain Draft (blocked on real session data). Stage 1.1 header Status stays "In Progress". Step 1 status stays "In Progress". -->

```yaml
# --- shared ops (apply once per stage) -----------------------------------

- operation: insert_before_anchor
  target_path: docs/session-token-latency-audit-exploration.md
  target_anchor: "## Design Expansion"
  payload: |
    ### Tooling Lessons

    Lessons harvested from Stage 1.1 baseline-telemetry tooling (TECH-510, TECH-511):

    - **macOS BSD `date` lacks `%3N`** — epoch-ms capture in shell scripts must use a portability fallback chain (`python3 -c 'time.time()*1000'` → `perl Time::HiRes` → `s * 1000`). Raw `date -u +%s%3N` emits literal `N` on BSD and compounding `000` suffix on GNU — both wrong. Any future shell-based ms-timestamp capture under `tools/scripts/` inherits this fallback.
    - **Prefer Node `readline` streaming over `readFileSync` for JSONL validators** — streams keep heap flat on large captures (>100k rows) and give natural file:line diagnostics on JSON.parse failures. Default choice for any future `tools/scripts/validate-*.mjs` consuming line-delimited data.

- operation: run_command
  target_path: tools/scripts/materialize-backlog.sh
  target_anchor: "bash tools/scripts/materialize-backlog.sh"
  payload: "bash tools/scripts/materialize-backlog.sh"

- operation: run_command
  target_path: package.json
  target_anchor: "npm run validate:dead-project-specs"
  payload: "npm run validate:dead-project-specs"

- operation: run_command
  target_path: package.json
  target_anchor: "npm run validate:backlog-yaml"
  payload: "npm run validate:backlog-yaml"

- operation: digest_emit
  target_path: ia/projects/session-token-latency-master-plan.md
  target_anchor: "Stage 1.1"
  payload:
    stage_id: "Stage 1.1"
    master_plan: "ia/projects/session-token-latency-master-plan.md"
    partial_close: true
    closed_tasks: ["TECH-510", "TECH-511"]
    deferred_tasks: ["TECH-512", "TECH-513"]
    stage_status_after: "In Progress"
    step_status_after: "In Progress"

# --- per-Task ops: TECH-510 ----------------------------------------------

- operation: archive_yaml
  target_path: ia/backlog/TECH-510.yaml
  target_anchor: ia/backlog/TECH-510.yaml
  payload:
    from: ia/backlog/TECH-510.yaml
    to: ia/backlog-archive/TECH-510.yaml

- operation: replace_in_row
  target_path: ia/projects/session-token-latency-master-plan.md
  target_anchor: "| T1.1.1 | Baseline collect script | 1 | **TECH-510** | Draft |"
  payload:
    from: "| T1.1.1 | Baseline collect script | 1 | **TECH-510** | Draft |"
    to:   "| T1.1.1 | Baseline collect script | 1 | **TECH-510** | Done |"

- operation: id_purge_scan
  target_path: ia/
  target_anchor: "TECH-510"
  payload:
    id: "TECH-510"
    scope:
      - BACKLOG.md
      - BACKLOG-ARCHIVE.md
      - ia/projects/
      - docs/
    exclude:
      - ia/backlog-archive/TECH-510.yaml
      - ia/projects/TECH-510.md
      - ia/projects/session-token-latency-master-plan.md

# --- per-Task ops: TECH-511 ----------------------------------------------

- operation: archive_yaml
  target_path: ia/backlog/TECH-511.yaml
  target_anchor: ia/backlog/TECH-511.yaml
  payload:
    from: ia/backlog/TECH-511.yaml
    to: ia/backlog-archive/TECH-511.yaml

- operation: replace_in_row
  target_path: ia/projects/session-token-latency-master-plan.md
  target_anchor: "| T1.1.2 | Telemetry schema validator | 1 | **TECH-511** | Draft |"
  payload:
    from: "| T1.1.2 | Telemetry schema validator | 1 | **TECH-511** | Draft |"
    to:   "| T1.1.2 | Telemetry schema validator | 1 | **TECH-511** | Done |"

- operation: id_purge_scan
  target_path: ia/
  target_anchor: "TECH-511"
  payload:
    id: "TECH-511"
    scope:
      - BACKLOG.md
      - BACKLOG-ARCHIVE.md
      - ia/projects/
      - docs/
    exclude:
      - ia/backlog-archive/TECH-511.yaml
      - ia/projects/TECH-511.md
      - ia/projects/session-token-latency-master-plan.md

# --- guardrails ----------------------------------------------------------
# Do NOT flip Stage 1.1 header "Status: In Progress" — partial close; TECH-512/TECH-513 still open.
# Do NOT flip Step 1 header "Status: In Progress — Stage 1.1" — same rationale.
# Do NOT update Step 1 "Backlog state" line — all four ids still filed (two Done, two Draft).
# Do NOT delete ia/projects/TECH-510.md or TECH-511.md — per-Task specs are retained post-closeout
#   under the Stage-scoped closeout model (closeout lives in master plan §Stage Closeout Plan, not per-Task).
```

---
