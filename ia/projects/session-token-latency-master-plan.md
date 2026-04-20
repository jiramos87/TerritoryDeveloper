# Session Token + Latency Remediation — Master Plan (MVP)

> **Last updated:** 2026-04-20
>
> **Status:** In Progress — Step 2 / Stage 2.2
>
> **Scope:** Token-economy and latency remediation across MCP surface pruning (B1/B3/B7 bundle), ambient context collapse (Theme A), dispatch path flattening (Theme C), hook plane remainder (Theme D), repo hygiene remainder (Theme E), and rev-4 larger bets (Theme F). Theme-0-round-1 quick-wins (B7/D1/E1/E2/D3) ship as standalone `/project-new` issues — out of this orchestrator. Theme B MCP-surface remainder (B4/B5/B6/B8/B9) delegated to `/master-plan-extend` against `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` — separate invocation, out of scope here.
>
> **Exploration source:** `docs/session-token-latency-audit-exploration.md` (`## Design Expansion — Post-M8 Authoring Shape` = ground truth; first `## Design Expansion` governs standalone Theme-0-r1 issues only); `docs/session-token-latency-post-mvp-extensions.md` (§4 Stage 3.3 + §5 pre-authored specs = extension source for Step 5).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach B two-pass: this orchestrator covers Themes A/C/D/E/F + Stage 1 B1+B3+B7 bundle; Theme B MCP-surface remainder via `/master-plan-extend` against MCP plan.
> - Stage 1 bundle = B1 (server split) + B3 (per-agent allowlist) + B7-extended (baseline harness) — one Stage, breadth-first, per-theme commit boundaries.
> - Baseline (Stage 1.1) = blocking gate for Stage 1.2; aggregate p50/p95/p99 only at Stage 1.1; no per-theme attribution until post-Stage-1.3 sweep.
> - Post-Stage 1.3 = one telemetry sweep only (Q4 lock).
> - A1/A2/C1/C2 must run **after** lifecycle-refactor Stage 10 T10.2 + T10.4 land.
> - F1 superseded by lifecycle-refactor Stage 10 T10.3 (runtime cacheable bundle); diffability angle demoted to Open Q in exploration doc.
> - Step 5 (D5 context pack) extension: PreCompact hook shell-only (no `claude -p` subprocess); pack gitignored (session-ephemeral); size cap 300 lines; freshness gate 24 h fixed; model-backed synthesis (`/pack-context`) explicitly out of scope. Semantic placement = Stage 3.3, filed as Step 5 per skill append-only contract — human reviewer may relocate post-apply.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/session-token-latency-audit-exploration.md` — full design + architecture + examples. `## Design Expansion — Post-M8 Authoring Shape` is ground truth.
> - `docs/session-token-latency-post-mvp-extensions.md` — Stage 3.3 (D5) synthesized context pack extension source; §4 Stage block + §5 pre-authored §Plan Author content for Step 5.
> - `docs/ai-mechanics-audit-2026-04-19.md` — source audit; item ids (B1–B9, C1–C3, D1–D4, E1–E3, F1–F7) traceable here.
> - `ia/projects/lifecycle-refactor-master-plan.md` — Stage 10 T10.2 + T10.4 = pre-conditions for Step 2.
> - `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` — Theme B MCP-surface extension lands here; B1 server-split decision (Stage 1.2) must precede that extension's B4 dist-build task.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — none flagged (zero runtime C# / IA-authoring surface touched by this plan).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | Skeleton | Planned | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`; `Skeleton` + `Planned` authored by `master-plan-new` / `stage-decompose`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `stage-file` also flips Stage header `Draft/Planned → In Progress` (R2) and plan top Status `Draft → In Progress — Step {N} / Stage {N.M}` on first task ever filed (R1); `stage-decompose` → Step header `Skeleton → Draft (tasks _pending_)` (R7); `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level step rollup; `project-stage-close` / `project-spec-close` → plan top Status `→ Final` when all Steps read `Final` (R5); `master-plan-extend` → plan top Status `Final → In Progress — Step {N_new} / Stage {N_new}.1` when new Steps appended to a Final plan (R6).

---

### Step 1 — Token-economy baseline + MCP surface pruning

**Status:** Final

**Backlog state (Step 1):** 14 filed / 14 closed across Stages 1.1–1.3 (TECH-510..513, TECH-524..527, TECH-534..539 — all archived).

**Objectives:** Establish a durable telemetry baseline (aggregate p50/p95/p99 for session input tokens + cache metrics + hook latency) that gates all subsequent Steps. Ship the three independent surface-pruning items from Theme B that do not require the T10.2 stable-block: B1 MCP server split (IA-core vs Unity-bridge), B3 per-agent tool-allowlist narrowing, and B7-extended session-level harness. Close with a single post-Stage-1.3 telemetry sweep providing per-theme attribution.

**Exit criteria:**

- `tools/scripts/agent-telemetry/baseline-summary.json` committed with p50/p95/p99 for `total_input_tokens`, `cache_read_tokens`, `cache_write_tokens`, `mcp_cold_start_ms`, `hook_fork_count`, `hook_fork_total_ms` (≥10 sessions measured).
- `tools/mcp-ia-server/src/index-ia.ts` + `tools/mcp-ia-server/src/index-bridge.ts` extracted; `index.ts` retained as backward-compat default; `MCP_SPLIT_SERVERS=0` default in `.mcp.json`; integration test passes.
- 7 target agents (`verifier.md`, `spec-implementer.md`, `stage-decompose.md`, `project-new-planner.md`, `project-new-applier.md`, `design-explore.md`, `test-mode-loop.md`) carry narrowed `tools:` frontmatter; CI lint `npm run validate:agent-tools` passes.
- PostToolUse telemetry hook (`tools/scripts/agent-telemetry/session-hook.sh`) active; per-session JSONL appended to `.claude/telemetry/{session-id}.jsonl` (gitignored).
- `tools/scripts/agent-telemetry/baseline-summary-post-stage1.json` committed; diff vs baseline-summary.json shows per-theme attribution for B1/B3/B7.
- `npm run validate:all` green.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/session-token-latency-audit-exploration.md` §Design Expansion — Post-M8 §Architecture + §Subsystem Impact + §Implementation Points
- `docs/ai-mechanics-audit-2026-04-19.md` §Theme B items B1/B3/B7
- `.mcp.json` (exists) — server split target
- `tools/mcp-ia-server/src/index.ts` (exists) — split source
- `tools/mcp-ia-server/src/index-ia.ts` (new)
- `tools/mcp-ia-server/src/index-bridge.ts` (new)
- `.claude/agents/verifier.md`, `spec-implementer.md`, `stage-decompose.md`, `project-new-planner.md`, `project-new-applier.md`, `design-explore.md`, `test-mode-loop.md` (all exist) — B3 allowlist targets
- `.claude/settings.json` (exists) — PostToolUse hook addition
- `tools/scripts/agent-telemetry/` (new dir) — B7 harness
- `ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md` — coordinate B1 split decision before Pass 2 B4 dist build

---

#### Stage 1.1 — Baseline measurement (gating)

**Status:** Final

**Objectives:** Author telemetry collection tooling and run ≥10 representative sessions to produce the aggregate baseline floor that gates Stage 1.2 entry. No per-theme attribution at this stage — single aggregate only.

**Exit:**

- `tools/scripts/agent-telemetry/baseline-collect.sh` executes without error; appends valid JSONL to `.claude/telemetry/{session-id}.jsonl`.
- `npm run validate:telemetry-schema` passes against a sample JSONL file.
- `tools/scripts/agent-telemetry/baseline-summary.json` committed; p50/p95/p99 present for all 6 required metrics (≥10 sessions).
- `.gitignore` updated: `*.jsonl` raw files excluded; summary JSON tracked.

**Phases:**

- [x] Phase 1 — Telemetry tooling: author baseline-collect.sh + schema validator script.
- [x] Phase 2 — Collection run: execute ≥10 sessions; aggregate + commit baseline-summary.json.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | Baseline collect script | 1 | **TECH-510** | Done | Author `tools/scripts/agent-telemetry/baseline-collect.sh` (new dir): reads `DEBUG_MCP_COMPUTE` stderr + PostToolUse hook stdout; appends JSONL to `.claude/telemetry/{session-id}.jsonl` with fields `ts`, `session_id`, `total_input_tokens`, `cache_read_tokens`, `cache_write_tokens`, `mcp_cold_start_ms`, `hook_fork_count`, `hook_fork_total_ms`. Update `.gitignore`: `.claude/telemetry/*.jsonl` excluded, `*-summary.json` tracked. |
| T1.1.2 | Telemetry schema validator | 1 | **TECH-511** | Done | Author `npm run validate:telemetry-schema` in `package.json` `scripts`: reads any `.claude/telemetry/*.jsonl` sample; asserts all 8 required fields present + typed correctly; exits non-zero on schema mismatch. Wire into `validate:all` composition in root `package.json`. |
| T1.1.3 | Baseline collection run | 2 | **TECH-512** | Done | Execute ≥10 representative sessions (mix of `/implement`, `/ship`, `/stage-file` lifecycle seams) with `baseline-collect.sh` active. Aggregate raw JSONL to `tools/scripts/agent-telemetry/baseline-summary.json` (p50/p95/p99 per metric); commit. No per-theme attribution — single aggregate floor only. |
| T1.1.4 | Gate validation + provenance | 2 | **TECH-513** | Done | Validate `baseline-summary.json` schema via `npm run validate:telemetry-schema`; assert all 6 metric keys present. Append measurement provenance (session count, date range, model, seam mix) to `docs/session-token-latency-audit-exploration.md` §Provenance. Confirm `npm run validate:all` green. Stage 1.2 entry conditional on this task Done. |

### §Stage File Plan

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

#### §Stage Closeout Plan

<!-- stage-closeout-plan output — do not hand-edit; apply via stage-closeout-apply -->
<!-- PARTIAL close: TECH-510 + TECH-511 only. TECH-512 / TECH-513 remain Draft (blocked on real session data). Stage 1.1 header Status stays "In Progress". Step 1 status stays "In Progress". -->

```yaml
# --- shared ops (apply once per stage) -----------------------------------

- operation: insert_before_anchor
  target_path: docs/session-token-latency-audit-exploration.md
  target_anchor: "## Design Expansion"
  payload: |
    ## Tooling Lessons

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

#### Stage 1.2 — MCP server split (B1)

**Status:** Final

**Objectives:** Extract Unity-bridge + compute tools from the single `territory-ia` MCP server into a dedicated `territory-ia-bridge` server behind a feature flag. IA-authoring sessions load the lean core; verify/implement stages opt-in to the bridge. Flag default off in this Stage; flip default in Stage 1.3 post-sweep.

**Exit:**

- `tools/mcp-ia-server/src/index-ia.ts` (new): registers all non-bridge tools (≥22 IA-authoring tools).
- `tools/mcp-ia-server/src/index-bridge.ts` (new): registers Unity-bridge + compute tools (`unity_bridge_command`, `unity_bridge_get`, `unity_bridge_lease`, `unity_compile`, `unity_callers_of`, `unity_subscribers_of`, `findobjectoftype_scan`, `city_metrics_query`, `desirability_top_cells`, `geography_init_params_validate`, `grid_distance`, `growth_ring_classify`, `isometric_world_to_grid`, `pathfinding_cost_preview`).
- `tools/mcp-ia-server/src/index.ts` retained as default entry (backward compat, imports both).
- `.mcp.json` `territory-ia-bridge` server entry added; `MCP_SPLIT_SERVERS=0` default.
- Integration test `tools/mcp-ia-server/tests/server-split.test.ts` passes: `MCP_SPLIT_SERVERS=1` + design-explore dispatch → bridge tools absent from `tools/list`; spec-implementer dispatch + bridge prefix → bridge tools present.

**Phases:**

- [ ] Phase 1 — Server extraction: author index-ia.ts + index-bridge.ts; update .mcp.json.
- [ ] Phase 2 — Integration test + flag documentation.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | Extract IA-core + bridge servers | 1 | **TECH-524** | Done | In `tools/mcp-ia-server/src/`: author `index-ia.ts` registering all IA-authoring tools (backlog, router, glossary, spec, rules, invariants, journal, reserve, materialize surfaces); author `index-bridge.ts` registering Unity-bridge + compute tools (14 tools). Original `index.ts` retained as backward-compat default importing both. Add `MCP_SPLIT_SERVERS` env check to `index.ts`: when `=1`, `index-ia.ts` standalone path loads. |
| T1.2.2 | .mcp.json split config | 1 | **TECH-525** | Done | Add `territory-ia-bridge` entry to `.mcp.json` pointing to `index-bridge.ts`; add `"MCP_SPLIT_SERVERS": "0"` to existing `territory-ia` env block (alongside existing `DEBUG_MCP_COMPUTE`). Document `MCP_SPLIT_SERVERS=1` flag semantics in `docs/mcp-ia-server.md` (new §Server split architecture section). |
| T1.2.3 | Integration test fixture | 2 | **TECH-526** | Done | Author `tools/mcp-ia-server/tests/server-split.test.ts`: assert `MCP_SPLIT_SERVERS=1` + design-explore-style dispatch → `tools/list` response excludes `unity_bridge_command`; assert spec-implementer-style dispatch with bridge server prefix declared → bridge tools present. Add `npm run test:mcp-split` script to `package.json`. |
| T1.2.4 | Flag-flip timeline doc | 2 | **TECH-527** | Done | Document `MCP_SPLIT_SERVERS` flag-flip timeline in Stage 1.3 header (flip from `0` to `1` after post-stage sweep confirms correctness per NB-6 resolution). Update `docs/session-token-latency-audit-exploration.md` §Open questions to mark B1 primary decision closed. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  task_key: "T1.2.1"
  title: "Extract IA-core + bridge servers"
  priority: "high"
  issue_type: "TECH"
  notes: |
    In tools/mcp-ia-server/src/: author index-ia.ts registering all IA-authoring tools
    (backlog, router, glossary, spec, rules, invariants, journal, reserve, materialize
    surfaces); author index-bridge.ts registering Unity-bridge + compute tools (14 tools:
    unity_bridge_command, unity_bridge_get, unity_bridge_lease, unity_compile,
    unity_callers_of, unity_subscribers_of, findobjectoftype_scan, city_metrics_query,
    desirability_top_cells, geography_init_params_validate, grid_distance,
    growth_ring_classify, isometric_world_to_grid, pathfinding_cost_preview). Original
    index.ts retained as backward-compat default importing both. Add MCP_SPLIT_SERVERS env
    check to index.ts: when =1, index-ia.ts standalone path loads. Foundation for B1
    server split — T1.2.2/T1.2.3/T1.2.4 consume.
  depends_on: []
  related:
    - "T1.2.2"
    - "T1.2.3"
    - "T1.2.4"
  stub_body:
    summary: |
      Extract single territory-ia MCP server into IA-core + bridge dual-server shape behind
      MCP_SPLIT_SERVERS feature flag. IA-authoring sessions load lean core; verify/implement
      stages opt-in to bridge. Flag default off in this Stage; flip in Stage 1.3 post-sweep.
    goals: |
      - New file tools/mcp-ia-server/src/index-ia.ts registers ≥22 IA-authoring tools.
      - New file tools/mcp-ia-server/src/index-bridge.ts registers 14 Unity-bridge + compute tools.
      - index.ts retains backward-compat default; importing both server modules.
      - MCP_SPLIT_SERVERS env check selects standalone path when =1.
    systems_map: |
      New: tools/mcp-ia-server/src/index-ia.ts.
      New: tools/mcp-ia-server/src/index-bridge.ts.
      Touches: tools/mcp-ia-server/src/index.ts (env-check + dual-import).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 1 — Server extraction.
      1. Inventory current index.ts tool registrations (≥36 total).
      2. Bucket tools: IA-authoring (backlog/router/glossary/spec/rules/invariants/journal/reserve/materialize) vs Unity-bridge + compute (14).
      3. Author index-ia.ts: import shared registration helpers + register IA-authoring tools.
      4. Author index-bridge.ts: register the 14 bridge tools.
      5. Edit index.ts: add MCP_SPLIT_SERVERS env check; default path imports both; =1 path loads index-ia.ts only.
      6. Run npm run validate:all.
```

```yaml
- reserved_id: ""
  task_key: "T1.2.2"
  title: ".mcp.json split config"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Add territory-ia-bridge entry to .mcp.json pointing to index-bridge.ts; add
    "MCP_SPLIT_SERVERS": "0" to existing territory-ia env block (alongside existing
    DEBUG_MCP_COMPUTE). Document MCP_SPLIT_SERVERS=1 flag semantics in
    docs/mcp-ia-server.md (new §Server split architecture section). Activates B1 dual-server
    surface for opt-in consumption.
  depends_on: []
  related:
    - "T1.2.1"
    - "T1.2.3"
    - "T1.2.4"
  stub_body:
    summary: |
      Wire dual-server config in .mcp.json: register territory-ia-bridge alongside existing
      territory-ia entry; default flag off. Document flag semantics + flip timeline in
      docs/mcp-ia-server.md.
    goals: |
      - .mcp.json carries territory-ia-bridge server entry pointing to index-bridge.ts.
      - territory-ia env block carries MCP_SPLIT_SERVERS=0 default.
      - docs/mcp-ia-server.md gains §Server split architecture section documenting flag.
      - npm run validate:all green.
    systems_map: |
      Touches: .mcp.json (root).
      Touches: docs/mcp-ia-server.md (new §Server split architecture section).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 1 — Config + docs.
      1. Edit .mcp.json: add territory-ia-bridge server block (mirrors territory-ia shape, command targets index-bridge.ts).
      2. Edit territory-ia env block: add "MCP_SPLIT_SERVERS": "0" alongside DEBUG_MCP_COMPUTE.
      3. Author docs/mcp-ia-server.md §Server split architecture: rationale + flag semantics + flip timeline pointer (Stage 1.3 sweep).
      4. Run npm run validate:all.
```

```yaml
- reserved_id: ""
  task_key: "T1.2.3"
  title: "Integration test fixture"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Author tools/mcp-ia-server/tests/server-split.test.ts: assert MCP_SPLIT_SERVERS=1 +
    design-explore-style dispatch → tools/list response excludes unity_bridge_command;
    assert spec-implementer-style dispatch with bridge server prefix declared → bridge
    tools present. Add npm run test:mcp-split script to package.json. Locks B1 split
    semantics behind a CI gate.
  depends_on: []
  related:
    - "T1.2.1"
    - "T1.2.2"
    - "T1.2.4"
  stub_body:
    summary: |
      Ship integration test asserting B1 server-split semantics. Two dispatches: lean
      IA-core path excludes bridge tools; bridge-prefix path exposes them. Wired via
      npm run test:mcp-split.
    goals: |
      - New file tools/mcp-ia-server/tests/server-split.test.ts.
      - Test asserts MCP_SPLIT_SERVERS=1 + IA-core dispatch hides 14 bridge tools.
      - Test asserts bridge-prefix dispatch exposes 14 bridge tools.
      - package.json scripts gain test:mcp-split entry.
    systems_map: |
      New: tools/mcp-ia-server/tests/server-split.test.ts.
      Touches: package.json (scripts.test:mcp-split).
      Reads: tools/mcp-ia-server/src/index-ia.ts + index-bridge.ts (T1.2.1 output).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 2 — Integration test.
      1. Author server-split.test.ts: spawn server with MCP_SPLIT_SERVERS=1; query tools/list; assert bridge tools absent.
      2. Add bridge-prefix branch: query tools/list with bridge config; assert 14 bridge tools present.
      3. Add test:mcp-split script to package.json.
      4. Run npm run test:mcp-split locally; confirm green.
      5. Run npm run validate:all.
```

```yaml
- reserved_id: ""
  task_key: "T1.2.4"
  title: "Flag-flip timeline doc"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Document MCP_SPLIT_SERVERS flag-flip timeline in Stage 1.3 header (flip from 0 to 1
    after post-stage sweep confirms correctness per NB-6 resolution). Update
    docs/session-token-latency-audit-exploration.md §Open questions to mark B1 primary
    decision closed. Closes Stage 1.2 paper trail.
  depends_on: []
  related:
    - "T1.2.1"
    - "T1.2.2"
    - "T1.2.3"
  stub_body:
    summary: |
      Doc-only task: cross-reference flag-flip timeline in Stage 1.3 header + close NB-6
      open question on B1 in exploration doc. No code touched.
    goals: |
      - Stage 1.3 header in master plan carries MCP_SPLIT_SERVERS flip timeline note.
      - docs/session-token-latency-audit-exploration.md §Open questions B1 entry marked Closed.
      - npm run validate:all green.
    systems_map: |
      Touches: ia/projects/session-token-latency-master-plan.md (Stage 1.3 header).
      Touches: docs/session-token-latency-audit-exploration.md (§Open questions).
      No code / runtime surface touched.
    impl_plan_sketch: |
      Phase 2 — Doc closeout.
      1. Edit Stage 1.3 header: add inline note pointing to MCP_SPLIT_SERVERS flip step (T1.3.6).
      2. Edit exploration §Open questions: flip B1 row to Closed with resolution pointer (Stage 1.2 + Stage 1.3 sweep).
      3. Run npm run validate:all.
```

---

#### Stage 1.3 — Allowlist narrowing + telemetry harness + post-stage sweep (B3 + B7 + sweep)

**Status:** Final

**Objectives:** Narrow `tools:` frontmatter for the 7 non-pair-seam subagents (B3), wire the PostToolUse session-level telemetry harness (B7-extended), then run the single post-Stage-1 telemetry sweep producing per-theme attribution. Flip `MCP_SPLIT_SERVERS` default to `1` after sweep validates correctness.

> **`MCP_SPLIT_SERVERS` flag-flip timeline (TECH-527):** flag authored + wired in Stage 1.2 (TECH-524 + TECH-525), default `0` through all of Stage 1.2 (backward compat — both buckets on single `territory-ia` server). Stage 1.3 Task T1.3.6 flips default `0` → `1` in `.mcp.json` ONLY AFTER T1.3.5 post-stage sweep confirms per-theme attribution correctness per NB-6 resolution (see `docs/session-token-latency-audit-exploration.md` §Examples §B7-extended). Flip gate: sweep produces `baseline-summary-post-stage1.json` with B1 split attribution row showing expected IA-core-only session token reduction. Do NOT flip the default earlier — telemetry sweep depends on A/B (`0` vs `1`) comparison data during Stage 1.3.
>
> **Status update (2026-04-20):** TECH-538 stubbed in-chain; `baseline-summary-post-stage1.json` = placeholder. TECH-539 sweep report carries per-theme attribution framework but no real rows. Flag flip **DEFERRED** pending real sweep data via follow-up issue. Default `MCP_SPLIT_SERVERS=0` retained in `.mcp.json`.


**Exit:**

- `verifier.md`, `spec-implementer.md`, `stage-decompose.md`, `project-new-planner.md`, `project-new-applier.md`, `design-explore.md`, `test-mode-loop.md` each carry `tools:` frontmatter listing only required tools (no wildcard).
- `npm run validate:agent-tools` CI lint passes: compares declared `tools:` set vs observed tool calls in test fixtures; alerts on wildcard re-introduction.
- `.claude/settings.json` carries `PostToolUse` hook entry running `tools/scripts/agent-telemetry/session-hook.sh`.
- `tools/scripts/agent-telemetry/session-hook.sh` appends per-tool-call JSONL (fields: `ts`, `session_id`, `tool`, `duration_ms`, `agent`, `lifecycle_stage`) to `.claude/telemetry/{session-id}.jsonl`.
- `tools/scripts/agent-telemetry/baseline-summary-post-stage1.json` committed; diff vs `baseline-summary.json` shows per-theme attribution rows for B1/B3/B7.
- `MCP_SPLIT_SERVERS` default flipped to `1` in `.mcp.json` after sweep validates correctness.

**Phases:**

- [ ] Phase 1 — Agent allowlist narrowing (B3).
- [ ] Phase 2 — Telemetry harness (B7-extended).
- [ ] Phase 3 — Post-stage sweep + flag flip.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.3.1 | Per-agent tools: narrowing | 1 | **TECH-534** | Done | Add `tools:` frontmatter to 7 target agents (`.claude/agents/verifier.md`, `spec-implementer.md`, `stage-decompose.md`, `project-new-planner.md`, `project-new-applier.md`, `design-explore.md`, `test-mode-loop.md`). Allowlist: Bash + Read + Grep + Glob + domain-relevant MCP tools only (e.g. verifier: `unity_bridge_command`, `unity_bridge_get`, `invariants_summary`, `backlog_issue`, `spec_section`; design-explore: `glossary_discover`, `glossary_lookup`, `router_for_task`, `spec_sections`, `spec_outline`, `invariants_summary`, `list_rules`, `rule_content`). See exploration §Examples §B3 for verifier before/after shape. |
| T1.3.2 | Agent-tools CI lint | 1 | **TECH-535** | Done | Author `npm run validate:agent-tools` in `package.json`: reads all `.claude/agents/*.md` frontmatter; asserts `tools:` present for the 7 narrowed agents; asserts no wildcard entry; alerts on any tool not in the approved MCP server namespace. Add to `validate:all` chain. Carry NB-7 (allowlist drift prevention) as enforced CI gate. |
| T1.3.3 | PostToolUse session hook | 2 | **TECH-536** | Done | Add `PostToolUse` entry to `.claude/settings.json` hooks array: `{"matcher": "*", "hooks": [{"type": "command", "command": "tools/scripts/agent-telemetry/session-hook.sh"}]}`. Author `tools/scripts/agent-telemetry/session-hook.sh`: reads tool name + duration from hook env vars; appends JSONL to `.claude/telemetry/{session-id}.jsonl`; exits 0 always (non-blocking). Reuses JSONL schema from T1.1.1. |
| T1.3.4 | Session aggregation helper | 2 | **TECH-537** | Done | Author `tools/scripts/agent-telemetry/aggregate-session.sh {session-id}`: reads JSONL for session; outputs per-tool p50/p99 duration + token estimates. Used by post-stage sweep script (T1.3.5). Update `.gitignore` to confirm raw JSONL excluded; `tools/scripts/agent-telemetry/*-summary.json` tracked. |
| T1.3.5 | Post-stage sweep run | 3 | **TECH-538** | Done | STUB — `baseline-summary-post-stage1.json` committed as schema-conformant placeholder per user direction (real sweep requires ≥10 human-driven sessions; self-modification hazard precludes the running agent from producing its own telemetry). Follow-up issue required for real sweep. |
| T1.3.6 | Per-theme attribution + flag flip | 3 | **TECH-539** | Done | Sweep report `tools/scripts/agent-telemetry/sweep-report-post-stage1.md` authored with per-theme attribution framework (B1/B3/B7). `MCP_SPLIT_SERVERS` flag flip DEFERRED pending real sweep data (stub cannot validate B1 attribution per NB-6 resolution). Default `0` retained in `.mcp.json`. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-534"
  task_key: "T1.3.1"
  title: "Per-agent tools: narrowing"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Add tools: frontmatter to 7 target subagents (verifier.md, spec-implementer.md,
    stage-decompose.md, project-new-planner.md, project-new-applier.md, design-explore.md,
    test-mode-loop.md). Allowlist = Bash + Read + Grep + Glob + domain-relevant MCP tools
    only. No wildcard. Source shape: exploration §Examples §B3 (verifier before/after).
  depends_on: []
  related:
    - "TECH-535"
  stub_body:
    summary: |
      Narrow tools: frontmatter for 7 non-pair-seam subagents. Replace implicit
      wildcard with explicit per-agent allowlist. Carries Theme B3 from session-token
      latency audit — reduces cold-start tool schema payload per subagent spawn.
    goals: |
      - 7 target subagent bodies carry explicit tools: frontmatter (no wildcard).
      - Per-agent allowlist matches observed tool calls in recent lifecycle runs.
      - Pair-seam subagents untouched (out of scope Stage 1.3).
      - npm run validate:all green.
    systems_map: |
      Touches: .claude/agents/verifier.md, spec-implementer.md, stage-decompose.md,
      project-new-planner.md, project-new-applier.md, design-explore.md, test-mode-loop.md.
      Reference: docs/session-token-latency-audit-exploration.md §Examples §B3.
      No runtime C# / Unity scene code touched.
    impl_plan_sketch: |
      Phase 1 — Per-agent allowlist authoring.
      1. For each of 7 target agents: enumerate observed tool calls from recent lifecycle runs.
      2. Compose tools: frontmatter entry — Bash + Read + Grep + Glob + domain MCP subset.
      3. Insert frontmatter; re-run targeted smoke (one invocation per agent).
      4. Run npm run validate:all.

- reserved_id: "TECH-535"
  task_key: "T1.3.2"
  title: "Agent-tools CI lint"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Author npm run validate:agent-tools in package.json: parses .claude/agents/*.md
    frontmatter; asserts tools: present for 7 narrowed agents + no wildcard entry;
    alerts on tools outside approved MCP server namespace. Wire into validate:all.
    Carries NB-7 (allowlist drift prevention) as enforced CI gate.
  depends_on:
    - "TECH-534"
  related:
    - "TECH-534"
  stub_body:
    summary: |
      CI lint script blocking wildcard re-introduction + tool drift in narrowed
      subagents. Parses YAML frontmatter; asserts allowlist shape. Wires into
      validate:all chain so future PRs fail fast on regression.
    goals: |
      - npm run validate:agent-tools passes on current main after TECH-534 lands.
      - Lint fails deterministically when wildcard re-added or unapproved tool declared.
      - validate:all composition includes validate:agent-tools.
      - CI job green.
    systems_map: |
      Touches: package.json (scripts entry), tools/scripts/validate-agent-tools.sh (new
      or tools/mcp-ia-server/src/validators/agent-tools.ts). Reads .claude/agents/*.md.
      Hooks into validate:all chain.
    impl_plan_sketch: |
      Phase 1 — Lint authoring.
      1. Author tools/scripts/validate-agent-tools.sh (or TS equivalent).
      2. Parse .claude/agents/*.md frontmatter; assert tools: list for 7 narrowed agents.
      3. Assert no wildcard; assert all entries in approved MCP namespace.
      4. Add to package.json scripts; chain into validate:all.
      5. Run npm run validate:all.

- reserved_id: "TECH-536"
  task_key: "T1.3.3"
  title: "PostToolUse session hook"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Add PostToolUse entry to .claude/settings.json hooks array. Author
    tools/scripts/agent-telemetry/session-hook.sh: reads tool name + duration from
    hook env; appends JSONL to .claude/telemetry/{session-id}.jsonl; exits 0 always
    (non-blocking). Reuses JSONL schema from T1.1.1 (TECH-510).
  depends_on: []
  related:
    - "TECH-537"
  stub_body:
    summary: |
      Wire PostToolUse session telemetry harness. Per-tool-call JSONL append with
      ts, session_id, tool, duration_ms, agent, lifecycle_stage fields. Non-blocking
      exit-0 hook. Feeds aggregation helper (T1.3.4) + post-stage sweep (T1.3.5).
    goals: |
      - .claude/settings.json PostToolUse entry wired, matcher "*", non-blocking.
      - session-hook.sh appends JSONL per tool call; exits 0 always.
      - Raw JSONL excluded via .gitignore (already set by T1.1.1).
      - npm run validate:all green.
    systems_map: |
      Touches: .claude/settings.json (hooks array), tools/scripts/agent-telemetry/session-hook.sh (new),
      tools/scripts/agent-telemetry/ (existing dir from Stage 1.1).
      Schema: reuses T1.1.1 / TECH-510 JSONL shape.
    impl_plan_sketch: |
      Phase 2 — Harness wiring.
      1. Author tools/scripts/agent-telemetry/session-hook.sh: read env vars (CLAUDE_TOOL_NAME,
         CLAUDE_TOOL_DURATION_MS, CLAUDE_SESSION_ID, CLAUDE_AGENT_SLUG); append JSONL.
      2. chmod +x; smoke-test single invocation.
      3. Add PostToolUse entry to .claude/settings.json hooks array.
      4. Run one /verify lifecycle seam to confirm JSONL appends + exit 0.
      5. npm run validate:all.

- reserved_id: "TECH-537"
  task_key: "T1.3.4"
  title: "Session aggregation helper"
  priority: "medium"
  issue_type: "TECH"
  notes: |
    Author tools/scripts/agent-telemetry/aggregate-session.sh {session-id}: reads
    JSONL for session; outputs per-tool p50/p99 duration + token estimates. Consumed
    by post-stage sweep (T1.3.5). Confirm .gitignore excludes raw JSONL;
    *-summary.json tracked.
  depends_on:
    - "TECH-536"
  related:
    - "TECH-536"
    - "TECH-538"
  stub_body:
    summary: |
      Per-session aggregation CLI turning raw JSONL into p50/p99 per-tool duration
      + token estimates. Input to post-stage sweep diff report. Non-interactive;
      reads single session id arg; writes summary JSON to stdout or fixed path.
    goals: |
      - aggregate-session.sh {session-id} returns per-tool p50/p99 + token totals.
      - Output JSON schema compatible with baseline-summary.json diff flow.
      - .gitignore confirmed: raw *.jsonl excluded; *-summary.json tracked.
      - Script tested against sample JSONL from T1.3.3.
    systems_map: |
      Touches: tools/scripts/agent-telemetry/aggregate-session.sh (new).
      Reads: .claude/telemetry/{session-id}.jsonl.
      Reference schema: tools/scripts/agent-telemetry/baseline-summary.json (from TECH-512).
    impl_plan_sketch: |
      Phase 2 — Aggregation tooling.
      1. Author aggregate-session.sh: jq-driven JSONL reader; compute p50/p99 per tool.
      2. Emit JSON shape compatible with baseline-summary.json schema.
      3. Smoke-test on sample session JSONL produced by T1.3.3.
      4. Confirm .gitignore rules.
      5. npm run validate:all.

- reserved_id: "TECH-538"
  task_key: "T1.3.5"
  title: "Post-stage sweep run"
  priority: "high"
  issue_type: "TECH"
  notes: |
    After B1 + B3 + B7 commit boundaries land: run ≥10 representative sessions (pad
    to ≥3 working days per NB-10). Produce tools/scripts/agent-telemetry/baseline-summary-post-stage1.json
    (p50/p95/p99 per metric, same schema as baseline-summary.json). If natural session
    diversity insufficient for per-theme attribution, synthesize A/B sessions (B1 on/off,
    B3 on/off independently) per NB-9.
  depends_on:
    - "TECH-534"
    - "TECH-536"
    - "TECH-537"
  related:
    - "TECH-537"
    - "TECH-539"
  stub_body:
    summary: |
      Execute the single post-Stage-1 telemetry sweep. ≥10 representative lifecycle
      sessions across mixed seams; aggregate to baseline-summary-post-stage1.json.
      A/B synthesize on B1 + B3 independently if natural diversity falls short.
    goals: |
      - baseline-summary-post-stage1.json committed with p50/p95/p99 per metric.
      - Schema matches baseline-summary.json exactly (TECH-511 validator green).
      - Per-theme attribution separation feasible (B1 vs B3 vs B7 distinguishable).
      - npm run validate:telemetry-schema + validate:all green.
    systems_map: |
      Touches: tools/scripts/agent-telemetry/baseline-summary-post-stage1.json (new).
      Consumes: aggregate-session.sh (TECH-537), session-hook.sh (TECH-536).
      Reference: docs/session-token-latency-audit-exploration.md §Provenance, NB-9, NB-10.
    impl_plan_sketch: |
      Phase 3 — Sweep execution.
      1. Confirm TECH-534 + TECH-536 + TECH-537 landed + merged to main.
      2. Run ≥10 representative sessions across /implement, /ship, /stage-file seams.
      3. If diversity insufficient: synthesize A/B pairs (B1 off/on, B3 off/on).
      4. Aggregate via aggregate-session.sh; write baseline-summary-post-stage1.json.
      5. npm run validate:telemetry-schema + validate:all.

- reserved_id: "TECH-539"
  task_key: "T1.3.6"
  title: "Per-theme attribution + flag flip"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Diff baseline-summary-post-stage1.json vs baseline-summary.json; compute
    per-theme attribution rows (B1 server-split, B3 allowlist narrowing, B7 harness
    overhead) per exploration §Examples §B7-extended aggregate shape. Commit sweep
    report. Flip MCP_SPLIT_SERVERS default from 0 to 1 in .mcp.json. Confirm
    npm run validate:all green.
  depends_on:
    - "TECH-538"
  related:
    - "TECH-538"
  stub_body:
    summary: |
      Diff pre/post baseline summaries; produce per-theme attribution rows (B1/B3/B7).
      Commit sweep report artifact. Flip MCP_SPLIT_SERVERS default 0 → 1 in .mcp.json
      after attribution validates B1 split correctness per NB-6 resolution.
    goals: |
      - Sweep report committed with per-theme rows for B1, B3, B7.
      - MCP_SPLIT_SERVERS default flipped 0 → 1 in .mcp.json.
      - B1 split attribution row shows expected IA-core session token reduction.
      - npm run validate:all green.
    systems_map: |
      Touches: .mcp.json (MCP_SPLIT_SERVERS default flip),
      tools/scripts/agent-telemetry/sweep-report-post-stage1.md (new, or similar).
      Consumes: baseline-summary.json + baseline-summary-post-stage1.json.
      Reference: exploration §Examples §B7-extended aggregate shape; NB-6 resolution.
    impl_plan_sketch: |
      Phase 3 — Attribution + flag flip.
      1. Diff baseline-summary.json vs baseline-summary-post-stage1.json per metric.
      2. Attribute deltas to B1, B3, B7 using A/B data (or synthetic pairs per NB-9).
      3. Author sweep report with per-theme rows.
      4. If B1 attribution matches expected reduction: flip MCP_SPLIT_SERVERS default to 1 in .mcp.json.
      5. npm run validate:all.
```

---

### Step 2 — Authority chain collapse (Themes A + C)

**Status:** In Progress — Stage 2.2

**Backlog state (Step 2):** 4 filed (TECH-577..580) — Stage 2.1 shipped (Pass 1 commits on branch); Stage 2.2 not yet filed

**Objectives:** Eliminate authority-chain violations across the always-loaded ambient surface (CLAUDE.md / AGENTS.md / `docs/agent-lifecycle.md` duplication — A1), caveman preamble restatements across ~40 surfaces (A2), oversized MEMORY.md entries (A4), and slash-command triple-statement dispatch (C1/C2). Lands the "single source of truth per topic" design principle across the full doc triangle. Seed lint (C3) ships in this Step as the CI gate enforcing future compliance.

**Pre-conditions:** lifecycle-refactor Stage 10 T10.2 (`ia/skills/_preamble/stable-block.md` authored and in canonical ingestion path) + T10.4 (F5 tool-uniformity validator for pair-seam agents) must both be Done before Stage 2.1 starts.

**Exit criteria:**

- `docs/agent-lifecycle.md` = sole authority for lifecycle taxonomy; `ia/rules/agent-lifecycle.md` shrunk to ≤12-line pointer stub; CLAUDE.md §3 Key files ≤20 lines.
- AGENTS.md §3 lifecycle section: cross-reference only (≤8 lines), no restated taxonomy.
- `.claude/memory/{slug}.md` files written for every MEMORY.md entry that exceeded 10 lines; MEMORY.md index ≤180 lines.
- `ia/skills/_preamble/stable-block.md` referenced (not restated) in all 13 subagent bodies + ~30 skill preambles + slash-command seeds.
- `npm run validate:skill-seeds` passes: every `Seed prompt` code block in `ia/skills/*/SKILL.md` names an existing subagent + files that exist.
- `/implement`, `/verify-loop`, `/closeout`, `/ship`, `/ship-stage` command bodies ≤ 60 lines each; mission statements stripped; parameter forwarding only.
- `npm run validate:all` green.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/session-token-latency-audit-exploration.md` §Theme A + §Theme C rows
- `ia/skills/_preamble/stable-block.md` (new — authored by lifecycle-refactor T10.2; must exist before Stage 2.1)
- `ia/rules/agent-lifecycle.md` (exists) — shrink target
- `CLAUDE.md` §3 Key files (exists) — collapse target
- `AGENTS.md` §3 (exists) — cross-ref collapse target
- `docs/agent-lifecycle.md` (exists) — promote to sole authority
- `.claude/memory/` (new dir, currently empty) — A4 promotion target
- MEMORY.md root + `~/.claude-personal/projects/.../memory/MEMORY.md` — A4 source
- `.claude/agents/*.md` (13 subagent bodies, all exist) — A2 preamble de-dupe targets
- `ia/skills/*/SKILL.md` (~30 skills) — A2 + C3 targets
- `.claude/commands/*.md` — C1/C2 flatten targets
- Prior step outputs: `tools/scripts/agent-telemetry/baseline-summary-post-stage1.json` + `tools/scripts/agent-telemetry/baseline-summary.json`

---

#### Stage 2.1 — Lifecycle taxonomy authority chain (A1 + A4)

**Status:** Final

**Pre-condition:** lifecycle-refactor Stage 10 T10.2 Done (`ia/skills/_preamble/stable-block.md` exists).

**Objectives:** Declare `docs/agent-lifecycle.md` as the single authoritative source for lifecycle taxonomy; collapse the 3 duplicates (CLAUDE.md §3, `ia/rules/agent-lifecycle.md`, AGENTS.md §3) to pointer stubs. Promote oversized MEMORY.md entries to per-file pointers.

**Exit:**

- `ia/rules/agent-lifecycle.md`: ≤12 lines, references `docs/agent-lifecycle.md` only.
- `CLAUDE.md` §3 Key files: ≤20 lines; lifecycle taxonomy row removed; `docs/agent-lifecycle.md` referenced as authority.
- `AGENTS.md` §3: ≤8 lines cross-reference section; full taxonomy table removed.
- `docs/agent-lifecycle.md`: explicitly marked `# {Title} — Canonical authority`; no duplicate prose removed (already authoritative).
- `.claude/memory/` dir: ≥1 `{slug}.md` file per oversized MEMORY.md entry; MEMORY.md index ≤180 lines.
- `npm run validate:all` green.

**Phases:**

- [x] Phase 1 — Doc-triangle authority chain collapse (A1).
- [x] Phase 2 — MEMORY.md hygiene (A4).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | Collapse rule + CLAUDE.md §3 | 1 | **TECH-577** | Done | Shrink `ia/rules/agent-lifecycle.md` to ≤12 lines: retain header + one-sentence purpose + `Full canonical doc: docs/agent-lifecycle.md` pointer + `## Ordered flow` stub linking there. Collapse `CLAUDE.md` §3 Key files: remove lifecycle taxonomy prose (≤20 lines remain); add `docs/agent-lifecycle.md` row to key-files table as sole lifecycle authority. Run `npm run validate:all`. |
| T2.1.2 | Collapse AGENTS.md §3 | 1 | **TECH-578** | Done | Shrink `AGENTS.md` §3 lifecycle section: replace full taxonomy table with ≤8-line block: "Full lifecycle flow: `docs/agent-lifecycle.md`. Surface map table: `ia/rules/agent-lifecycle.md` §Surface map." Remove restated step/stage/phase/task definitions. Verify no other AGENTS.md section duplicates CLAUDE.md key-files inventory. `npm run validate:all`. |
| T2.1.3 | MEMORY.md oversized-entry promotion | 2 | **TECH-579** | Done | Identify all MEMORY.md entries (both root `MEMORY.md` and `~/.claude-personal/projects/.../memory/MEMORY.md`) exceeding 10 lines. For each: write `{slug}.md` to `.claude/memory/` (repo-scoped entries) or `~/.claude-personal/projects/.../memory/` (user entries) with full content. Replace MEMORY.md inline content with pointer line `- [{Title}]({slug}.md) — {one-line hook}`. |
| T2.1.4 | MEMORY.md index validation | 2 | **TECH-580** | Done | Confirm both MEMORY.md files ≤200 lines (harness truncation threshold). Validate all pointer links resolve to existing files. Check `docs/agent-lifecycle.md` still has correct `Status:` + last-updated front matter after A1 edits. `npm run validate:all` green. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "Collapse rule + CLAUDE.md §3"
  priority: "medium"
  notes: |
    Shrink `ia/rules/agent-lifecycle.md` to pointer stub (≤12 lines). Collapse `CLAUDE.md` §3 Key files: drop lifecycle taxonomy prose; cite `docs/agent-lifecycle.md` as sole authority. Gate: `npm run validate:all`.
  depends_on:
    - "TECH-503"
  related: []
  stub_body:
    summary: |
      A1 doc-triangle: rule stub + CLAUDE key-files table point at `docs/agent-lifecycle.md` only; remove duplicate lifecycle taxonomy from always-loaded surfaces.
    goals: |
      - `ia/rules/agent-lifecycle.md` ≤12 lines with pointer to `docs/agent-lifecycle.md` + ordered-flow stub.
      - `CLAUDE.md` §3 Key files ≤20 lines; lifecycle row references canonical doc only.
      - `npm run validate:all` green.
    systems_map: |
      Touches: `ia/rules/agent-lifecycle.md`, `CLAUDE.md` §3, `docs/agent-lifecycle.md` (read-only confirm authority marker).
      Ref: `docs/session-token-latency-audit-exploration.md` Theme A; master plan Step 2 Stage 2.1 exit bullets.
    impl_plan_sketch: |
      Phase 1 — Rule + CLAUDE collapse.
      1. Edit `ia/rules/agent-lifecycle.md` to header + one-line purpose + pointer + `## Ordered flow` stub linking canonical doc.
      2. Trim `CLAUDE.md` §3: remove restated taxonomy; ensure key-files table includes `docs/agent-lifecycle.md` as lifecycle authority.
      3. `npm run validate:all`.

- reserved_id: ""
  title: "Collapse AGENTS.md §3"
  priority: "medium"
  notes: |
    Replace AGENTS.md §3 full taxonomy with ≤8-line cross-ref block to `docs/agent-lifecycle.md` + `ia/rules/agent-lifecycle.md` §Surface map. Verify no duplicate key-files inventory vs CLAUDE. `npm run validate:all`.
  depends_on:
    - "TECH-503"
  related: []
  stub_body:
    summary: |
      A1: AGENTS.md lifecycle section becomes short cross-reference; no restated step/stage/phase/task definitions.
    goals: |
      - `AGENTS.md` §3 ≤8 lines; points to canonical doc + surface map stub.
      - No section duplicates CLAUDE key-files list inappropriately.
      - `npm run validate:all` green.
    systems_map: |
      Touches: `AGENTS.md` §3, `docs/agent-lifecycle.md`, `ia/rules/agent-lifecycle.md` (surface map anchor).
    impl_plan_sketch: |
      Phase 1 — AGENTS collapse.
      1. Replace §3 body with cross-ref block per Stage 2.1 Intent.
      2. Grep AGENTS for accidental CLAUDE inventory duplication; fix.
      3. `npm run validate:all`.

- reserved_id: ""
  title: "MEMORY.md oversized-entry promotion"
  priority: "medium"
  notes: |
    Find MEMORY.md entries >10 lines (repo root + optional user path). Promote each to `.claude/memory/{slug}.md` or user memory dir; replace inline body with one-line pointer. Doc-only; no runtime C#.
  depends_on:
    - "TECH-503"
  related: []
  stub_body:
    summary: |
      A4: split oversized MEMORY bullets into slug files; keep MEMORY.md as short index with links + hooks.
    goals: |
      - Every >10-line entry promoted to dedicated file; MEMORY.md lines replaced with pointer rows.
      - `.claude/memory/` populated for repo-scoped content per Intent.
      - Paths outside repo documented if user-local-only (optional scope note in spec).
    systems_map: |
      Touches: root `MEMORY.md`, `.claude/memory/`, `~/.claude-personal/.../memory/MEMORY.md` (if present on dev machine).
    impl_plan_sketch: |
      Phase 2 — Promotion sweep.
      1. Enumerate entries; measure line counts.
      2. For each oversized: write `{slug}.md`; replace with `- [Title](slug.md) — hook` line.
      3. Commit repo-scoped files; document user-dir handling in §7 if needed.

- reserved_id: ""
  title: "MEMORY.md index validation"
  priority: "medium"
  notes: |
    Enforce MEMORY index size caps (≤200 lines per file per harness); validate pointer targets exist; re-check `docs/agent-lifecycle.md` front matter after A1. `npm run validate:all` green.
  depends_on:
    - "TECH-503"
  related: []
  stub_body:
    summary: |
      Close A4: index hygiene + link integrity + doc metadata consistency after lifecycle edits.
    goals: |
      - Both MEMORY.md files ≤200 lines; all links resolve.
      - `docs/agent-lifecycle.md` Status + last-updated front matter still correct post-A1.
      - `npm run validate:all` green.
    systems_map: |
      Touches: `MEMORY.md` (root), user memory path if applicable, `docs/agent-lifecycle.md` metadata.
    impl_plan_sketch: |
      Phase 2 — Validation pass.
      1. Line-count + link check on MEMORY indices.
      2. Verify `docs/agent-lifecycle.md` header fields.
      3. `npm run validate:all`.
```

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

---

#### Stage 2.2 — Preamble de-dupe + seed lint (A2 + C3)

**Status:** Draft (tasks _pending_ — not yet filed)

**Pre-condition:** lifecycle-refactor Stage 10 T10.2 Done (caveman rule entered `ia/skills/_preamble/stable-block.md`; stable-block is in canonical ingestion path).

**Objectives:** Replace full-text caveman directive restatements across 13 subagent bodies, ~30 skill preambles, and slash-command seeds with a single canonical reference line. Ship `npm run validate:skill-seeds` as the CI gate preventing future seed-subagent drift.

**Exit:**

- All 13 subagent bodies (`.claude/agents/*.md`): caveman restatement replaced by `@ia/skills/_preamble/stable-block.md` or equivalent single-reference line (≤15 tokens each).
- All `ia/skills/*/SKILL.md` preamble sections: full-text caveman directive replaced by reference line where restated.
- All `.claude/commands/*.md` "forward verbatim" blocks: caveman directive stripped; forwarded parameters only.
- `npm run validate:skill-seeds` passes: every `Seed prompt` code block names an existing `.claude/agents/{name}.md` file + references files that exist on disk.
- `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — Preamble de-dupe across subagents + skills (A2).
- [ ] Phase 2 — Skill-seed lint authoring + validation (C3).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | Subagent preamble collapse | 1 | _pending_ | _pending_ | In all 13 `.claude/agents/*.md` bodies: locate full-text caveman directive block (typically the first 10–20 lines or `@`-loaded preamble section); replace with `@ia/skills/_preamble/stable-block.md` reference line (single line, ≤15 tokens). Verify behavior unchanged: stable-block already contains `agent-output-caveman` rule. Run `npm run validate:all`. |
| T2.2.2 | Skill + command preamble collapse | 1 | _pending_ | _pending_ | In all `ia/skills/*/SKILL.md` preamble sections where caveman directive is restated verbatim: replace with `Caveman default — see \`ia/skills/_preamble/stable-block.md\``. In `.claude/commands/*.md` "forward verbatim" blocks: strip caveman directive restatement; keep parameter-forwarding prose only. Spot-check 5 skills + 3 commands before/after. `npm run validate:all`. |
| T2.2.3 | validate:skill-seeds script | 2 | _pending_ | _pending_ | Author `tools/scripts/validate-skill-seeds.sh` (or Node equivalent): reads every `Seed prompt` fenced code block in `ia/skills/*/SKILL.md`; extracts subagent name + referenced file paths; asserts each subagent maps to existing `.claude/agents/{name}.md`; asserts each file path resolves on disk. Add `npm run validate:skill-seeds` to `package.json` + `validate:all` chain. |
| T2.2.4 | Seed-drift remediation | 2 | _pending_ | _pending_ | Run `npm run validate:skill-seeds`; fix any seed-subagent name drift or stale file references found (expected: subagent renames from lifecycle-refactor M3/M6 collapse — `spec-kickoff` → retired, `closeout` → `stage-closeout-planner`). Commit fixes. `npm run validate:all` green. |

---

#### Stage 2.3 — Slash-command dispatch flattening (C1 + C2)

**Status:** Draft (tasks _pending_ — not yet filed)

**Pre-conditions:** lifecycle-refactor Stage 10 T10.2 Done (stable-block in agent bodies) + T10.4 Done (F5 tool-uniformity validator for pair-seam agents).

**Objectives:** Strip "Subagent prompt (forward verbatim)" mission-restatement blocks from `/implement`, `/verify-loop`, `/closeout`, `/ship`, `/ship-stage` command bodies. Commands become parameter-forwarding dispatchers (≤60 lines each); subagent bodies remain authoritative. Specific focus on `/ship` which is 192 lines (C2).

**Exit:**

- `.claude/commands/implement.md`: ≤60 lines; Mission + Phase loop stripped; ISSUE_ID forwarding retained.
- `.claude/commands/verify-loop.md`, `closeout.md`, `ship-stage.md`: ≤60 lines each.
- `.claude/commands/ship.md`: ≤60 lines (down from 192); gate logic + parameter forwarding only.
- Human-readable "What this does" block (10–20 lines) preserved at top per C5 Q5 resolution.
- `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — Core lifecycle commands collapse (C1).
- [ ] Phase 2 — /ship command slim (C2) + integration gate.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.3.1 | Collapse implement + verify-loop + closeout | 1 | _pending_ | _pending_ | In `.claude/commands/implement.md`, `verify-loop.md`, `closeout.md`: remove "Subagent prompt (forward verbatim)" block restating subagent Mission + Phase loop. Retain: "What this does" block (10–20 lines human summary, per Q5 resolution), parameter list (ISSUE_ID / MASTER_PLAN_PATH / STAGE_ID), gate boundary lines. Target ≤60 lines each. `npm run validate:all`. |
| T2.3.2 | Collapse ship-stage + kickoff commands | 1 | _pending_ | _pending_ | In `.claude/commands/ship-stage.md` and any remaining command bodies carrying full mission restatement: apply same collapse (≤60 lines; "What this does" header + parameters + gate). Check `.claude/commands/_retired/` for stale references; clean drift (no action if clean). `npm run validate:all`. |
| T2.3.3 | /ship command slim | 2 | _pending_ | _pending_ | `.claude/commands/ship.md` (currently ~192 lines): collapse to ≤60 lines. Keep: "What this does" (≤15 lines), ISSUE_ID / MASTER_PLAN_PATH params, gate-boundary check (master plan located?), dispatch line. Strip: full Phase loop restatement, hard-boundaries repeat, example invocations already in subagent body. Model after condensed ship-stage shape from T2.3.2. |
| T2.3.4 | Integration smoke + token delta | 2 | _pending_ | _pending_ | Run full `/ship {ISSUE_ID}` dispatch on a dry-run issue; confirm subagent body authoritative (no degraded behavior from stripped command). Estimate per-`/ship` token saving vs pre-C1/C2 baseline: diff collapsed command byte count × invocation frequency from telemetry. Commit finding to `docs/session-token-latency-audit-exploration.md` §Provenance. `npm run validate:all` green. |

---

### Step 3 — Hook plane + repo surface hygiene (Themes D + E remainder)

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 3):** 0 filed

**Objectives:** Complete the hook-plane work not covered in Theme-0-r1: make `session-start-prewarm.sh` emit a deterministic cacheable preamble (D2) and add compact-survival state capture (D4). Trim the oversized output-style descriptors `verification-report.md` and `closeout-digest.md` (E3). Step is mostly independent of T10.2/T10.4 and can begin once Stage 1.3 sweep is complete.

**Exit criteria:**

- `tools/scripts/claude-hooks/session-start-prewarm.sh`: volatile data (branch, dirty count) emitted to stderr only; stdout emits deterministic cacheable preamble block (fixed content: active-freeze status + MCP server version + enabled ruleset name).
- `.claude/last-compact-summary.md` written on compact/Stop event; contains current task id + active stage + last 3 tool call names.
- `.claude/output-styles/verification-report.md`: ≤35 lines; field semantics extracted to `docs/agent-led-verification-policy.md` §Output format; Part 1 / Part 2 structure + example block retained.
- `.claude/output-styles/closeout-digest.md`: audited; trimmed if over 50 lines.
- `npm run validate:all` green.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `tools/scripts/claude-hooks/session-start-prewarm.sh` (exists) — D2 refactor target
- `.claude/settings.json` (exists) — D4 hook entry addition
- `.claude/output-styles/verification-report.md` (exists, 87 lines) — E3 trim target
- `.claude/output-styles/closeout-digest.md` (exists) — E3 audit target
- `docs/agent-led-verification-policy.md` (exists) — E3 field-semantics destination
- Prior step outputs: Steps 1–2 telemetry sweep + command collapse

---

#### Stage 3.1 — Session-start preamble + compact-survival (D2 + D4)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Refactor `session-start-prewarm.sh` so the cacheable prefix is stable across sessions (volatile data to stderr; deterministic block to stdout). Add compact-survival hook writing `.claude/last-compact-summary.md` so agents can re-orient after context compaction.

**Exit:**

- `tools/scripts/claude-hooks/session-start-prewarm.sh`: `stderr` carries branch + dirty count; `stdout` emits fixed block: `[territory-developer] MCP: territory-ia v{version} | Ruleset: invariants + lifecycle + caveman | Freeze: {active|lifted}`.
- `.claude/settings.json` Stop/PostCompact hook entry: runs `tools/scripts/claude-hooks/compact-summary.sh`.
- `tools/scripts/claude-hooks/compact-summary.sh` (new): writes `.claude/last-compact-summary.md` with fields `active_task_id`, `active_stage`, `last_3_tools`, `ts`.
- `.claude/last-compact-summary.md` gitignored (session-ephemeral state).
- `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — Session-start preamble refactor (D2).
- [ ] Phase 2 — Compact-survival hook (D4).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.1.1 | Session-start deterministic preamble | 1 | _pending_ | _pending_ | Refactor `tools/scripts/claude-hooks/session-start-prewarm.sh`: move `branch=$(git branch ...)` + dirty-count line to emit via `>&2` (stderr); add fixed stdout block: `echo "[territory-developer] MCP: territory-ia v$(…) | Ruleset: invariants+lifecycle+caveman | Freeze: $(cat ia/state/lifecycle-refactor-migration.json | jq -r '.status')"`. Volatile suffix no longer destabilises cached prefix. |
| T3.1.2 | Runtime-state.json skeleton (F4 prep) | 1 | _pending_ | _pending_ | Author `.claude/runtime-state.json` schema stub (prep for Stage 4.1 F4): fields `last_verify_exit_code`, `last_bridge_preflight_exit_code`, `queued_test_scenario_id`, `active_task_id`, `active_stage`. Emit top-level keys from SessionStart preamble block (read `.claude/runtime-state.json` if exists; populate preamble deterministic block with `active_task_id` + `active_stage` values). Update `.gitignore`: track `runtime-state.json` (shared state, not ephemeral). |
| T3.1.3 | Compact-survival hook | 2 | _pending_ | _pending_ | Author `tools/scripts/claude-hooks/compact-summary.sh`: on Stop/PostCompact event reads `.claude/runtime-state.json` + last 3 entries from `.claude/telemetry/{session-id}.jsonl`; writes `.claude/last-compact-summary.md` (`active_task_id`, `active_stage`, `last_3_tools`, `ts`). Add Stop hook entry to `.claude/settings.json` hooks array. Add `.claude/last-compact-summary.md` to `.gitignore`. |
| T3.1.4 | Compact re-orientation test | 2 | _pending_ | _pending_ | Manual test: run session → compact → resume; verify `.claude/last-compact-summary.md` present + readable; confirm SessionStart preamble emits `active_task_id` from it. Confirm `npm run validate:all` green. Document compact-survival UX in `docs/agent-led-verification-policy.md` §Session continuity (new 3-line sub-section). |

---

#### Stage 3.2 — Output-style surface trim (E3)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Trim `.claude/output-styles/verification-report.md` from 87 lines to ≤35 lines by extracting field semantics to `docs/agent-led-verification-policy.md`. Audit and trim `closeout-digest.md`. Verify all verifier + closeout subagent dispatches still parse output styles correctly.

**Exit:**

- `.claude/output-styles/verification-report.md`: ≤35 lines; keeps Part 1 (JSON header) + Part 2 (caveman summary) structure + minimal example block; field-semantic prose moved to `docs/agent-led-verification-policy.md` §Verification output fields.
- `.claude/output-styles/closeout-digest.md`: ≤50 lines; audited; trimmed if over budget.
- Verifier + stage-closeout-applier subagents dispatch with correct output style shape (no regression).
- `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — Output-style trim + validation.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T3.2.1 | Trim verification-report.md | 1 | _pending_ | _pending_ | Read `.claude/output-styles/verification-report.md` (87 lines); extract field-semantic prose (per-field descriptions, JSON schema commentary) to new `docs/agent-led-verification-policy.md` §Verification output fields sub-section. Retain in file: brief purpose line, Part 1 JSON header shape (≤10 lines), Part 2 caveman summary shape (≤5 lines), one canonical example (≤15 lines). Target ≤35 lines. Update `verifier.md` + `verify-loop.md` agent bodies if they inline-reference line numbers. |
| T3.2.2 | Trim closeout-digest.md + validate | 1 | _pending_ | _pending_ | Read `.claude/output-styles/closeout-digest.md`; if > 50 lines apply same trim pattern (extract semantics to `docs/agent-led-verification-policy.md` §Closeout digest output fields). Update `stage-closeout-applier.md` if it references specific lines. Run full `/verify` + `/closeout` dispatch dry-run to confirm output shapes parse correctly. `npm run validate:all` green. |

---

### Step 4 — Rev-4 larger bets (Theme F)

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step 4):** 0 filed

**Objectives:** Ship the structural improvements that require rev-4 Tier 1/Tier 2 cache design to be stable: session-level MCP memoization (F2), unified runtime-state.json (F4, building on Stage 3.1 skeleton), prescriptive cache-breakpoint MCP tool (F5), and skills-navigator MCP tool (F6). File tracking issues for harness-gated items F3 + F7 (no code change; monitoring only).

**Pre-conditions:** Stage 3.1 `runtime-state.json` skeleton must be Done before Stage 4.1 (F4 full migration uses the skeleton). Lifecycle-refactor Stage 10 T10.7 (20-block guardrail) recommended before Stage 4.2 (F5 prescriptive tool complements T10.7 prohibitive rule).

**Exit criteria:**

- `.claude/tool-usage.jsonl` (session-ephemeral, gitignored): PostToolUse hook appends `{tool_name, args_hash, result_hash, ts}` per call.
- Subagent dispatch reads `.claude/tool-usage.jsonl` for current session; skips re-call when args_hash matches within Stage window.
- `.claude/runtime-state.json`: flat-file markers (`last-verify-exit-code`, `last-bridge-preflight-exit-code`, `.queued-test-scenario-id`) fully migrated; hooks write via `jq` append.
- New MCP tool `cache_breakpoint_recommend(stage_id)` registered in `tools/mcp-ia-server/src/index-ia.ts`; returns 4 anchors (Tier 1 prefix end, Tier 2 bundle end, spec end, last executor-mutable block).
- New MCP tool `skill_for_task(keywords, lifecycle_stage)` registered; returns matching `ia/skills/*/SKILL.md` path + URL + first-phase body.
- Tracking issues filed for F3 (harness caveman hook) + F7 (`defer_loading: true`); linked from exploration doc.
- `npm run validate:all` green.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/session-token-latency-audit-exploration.md` §Theme F items F2/F3/F4/F5/F6/F7
- `docs/prompt-caching-mechanics.md` §3 (Tier 1 + Tier 2 anchor definitions) — F5 dependency
- `.claude/settings.json` — PostToolUse hook updates
- `tools/mcp-ia-server/src/index-ia.ts` (will exist post-Stage 1.2) — F5/F6 registration target
- `tools/mcp-ia-server/src/index.ts` (exists) — F5/F6 interim registration target if split not yet flipped
- `ia/skills/README.md` (exists) — F6 index source
- `ia/rules/agent-lifecycle.md` — lifecycle_stage enum for F6
- Prior step outputs: `.claude/runtime-state.json` skeleton (Stage 3.1 T3.1.2), `.claude/tool-usage.jsonl` schema (extends T1.1.1 telemetry)

---

#### Stage 4.1 — Session-level MCP memoization + unified runtime state (F2 + F4)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire PostToolUse hook writing `{tool_name, args_hash, result_hash, ts}` to `.claude/tool-usage.jsonl` (F2). Enable subagent dispatch to read that file and skip re-calls within Stage window. Fully migrate scattered flat-file state markers to the unified `.claude/runtime-state.json` schema (F4, building on Stage 3.1 skeleton).

**Exit:**

- `.claude/tool-usage.jsonl` written per tool call via PostToolUse hook; fields: `tool_name`, `args_hash` (sha256 of serialized args), `result_hash` (sha256 of result), `ts`, `session_id`.
- `.claude/tool-usage.jsonl` gitignored (session-ephemeral).
- `spec-implementer.md` + `design-explore.md` preambles: read `.claude/tool-usage.jsonl` for session; skip `glossary_discover` / `router_for_task` re-call when args_hash matches within Stage window.
- `.claude/runtime-state.json`: `last_verify_exit_code`, `last_bridge_preflight_exit_code`, `queued_test_scenario_id` fields populated by hooks; old flat-file markers (`.claude/last-verify-exit-code`, etc.) deleted.
- `verify-loop` + `bridge-environment-preflight` skills write exit codes to `runtime-state.json` via `jq` (reuses D3 `jq` dep from Theme-0-r1 D3 issue).
- `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — Tool-usage memoization (F2).
- [ ] Phase 2 — Unified runtime state migration (F4).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.1.1 | Tool-usage PostToolUse hook | 1 | _pending_ | _pending_ | Extend `.claude/settings.json` PostToolUse hook (or add second hook entry): run `tools/scripts/agent-telemetry/tool-usage-hook.sh`. Author that script: reads tool name + args + result from hook env; computes `args_hash = sha256(tool_name + sorted_args_json)`, `result_hash = sha256(result_json)`; appends JSON line to `.claude/tool-usage.jsonl`. Add `.claude/tool-usage.jsonl` to `.gitignore`. |
| T4.1.2 | Subagent memoization read path | 1 | _pending_ | _pending_ | In `spec-implementer.md` + `design-explore.md` preamble: add "Session-window memoization check" block: before `glossary_discover` / `router_for_task` calls, compute args_hash; check `.claude/tool-usage.jsonl` for matching `{tool_name, args_hash}` within same `session_id`; if found, use cached `result_hash` lookup from a companion `.claude/tool-usage-cache.json` (key: args_hash → result). Skip live MCP call. Author `tools/scripts/agent-telemetry/cache-lookup.sh {tool_name} {args_hash}` returning result or exit 1 on miss. |
| T4.1.3 | Unified runtime-state migration | 2 | _pending_ | _pending_ | Extend `.claude/runtime-state.json` schema (from Stage 3.1 T3.1.2 skeleton): add `last_verify_exit_code`, `last_bridge_preflight_exit_code`, `queued_test_scenario_id` fields. Update `ia/skills/verify-loop/SKILL.md` Step 7 to write `last_verify_exit_code` via `jq '. + {"last_verify_exit_code": $code}' .claude/runtime-state.json` instead of flat file. Same for `bridge-environment-preflight` writing `last_bridge_preflight_exit_code`. Update `agent-test-mode-verify` skill reading `.queued-test-scenario-id` to read from `runtime-state.json`. |
| T4.1.4 | Flat-file marker cleanup | 2 | _pending_ | _pending_ | After migration verified: delete old flat-file markers (`.claude/last-verify-exit-code`, `.claude/last-bridge-preflight-exit-code`, `.claude/queued-test-scenario-id` if they exist). Update SessionStart preamble (Stage 3.1 T3.1.1 stdout block) to emit `last_verify_exit_code` from `runtime-state.json`. `npm run validate:all` green. |

---

#### Stage 4.2 — Cache-breakpoint prescriptive tooling (F5)

**Status:** Draft (tasks _pending_ — not yet filed)

**Pre-condition:** `docs/prompt-caching-mechanics.md` §3 must define Tier 1 + Tier 2 anchors (authored by lifecycle-refactor T10.2 or earlier). Lifecycle-refactor T10.7 (20-block guardrail) recommended (F5 complements T10.7 prohibitive rule with prescriptive recipe).

**Objectives:** Add new MCP tool `cache_breakpoint_recommend(stage_id)` returning the 4 recommended breakpoint anchors for a given Stage. Author `npm run validate:cache-breakpoints` CI lint enforcing breakpoint annotations in skill preambles. Document 4-anchor layout prescriptively in `prompt-caching-mechanics.md` §F5.

**Exit:**

- MCP tool `cache_breakpoint_recommend` registered in `tools/mcp-ia-server/src/index-ia.ts`; returns `{tier1_end, tier2_bundle_end, spec_end, last_executor_mutable}` for a given `stage_id`.
- `npm run validate:cache-breakpoints` script: reads `ia/skills/*/SKILL.md` preambles; asserts breakpoint annotation present; exits non-zero if missing. Added to `validate:all`.
- `docs/prompt-caching-mechanics.md` §F5: 4-anchor layout documented as prescriptive recipe (not just prohibitive reference).
- `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — MCP tool authoring + registration.
- [ ] Phase 2 — CI lint + docs.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.2.1 | cache_breakpoint_recommend MCP tool | 1 | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/cache-breakpoint-recommend.ts`: `registerTool("cache_breakpoint_recommend", ...)` with input `stage_id: string`; reads stage block from `ia/projects/*/master-plan.md` matching stage_id; returns 4 anchor objects `{name, anchor_type, location_hint}` per `prompt-caching-mechanics.md` §3 Tier 1/Tier 2 definitions + Tier 3 (spec end) + Tier 4 (last executor-mutable block). Register in `index-ia.ts`. |
| T4.2.2 | Skill preamble breakpoint annotation | 1 | _pending_ | _pending_ | Update `ia/skills/*/SKILL.md` preamble sections (lifecycle skills: `stage-file-plan`, `project-spec-implement`, `opus-code-review`, `stage-closeout-plan`, `plan-author`, `opus-audit`) to include a `cache_breakpoints:` frontmatter line listing the 4 anchor names. Use `cache_breakpoint_recommend` output to derive correct values per skill's lifecycle_stage. |
| T4.2.3 | validate:cache-breakpoints CI script | 2 | _pending_ | _pending_ | Author `tools/scripts/validate-cache-breakpoints.sh`: reads `ia/skills/*/SKILL.md` frontmatter; for skills with `phases:` key (progress-emit lifecycle skills), asserts `cache_breakpoints:` key present with ≥4 named anchors. Add `npm run validate:cache-breakpoints` to `package.json` + `validate:all` chain. |
| T4.2.4 | 4-anchor layout documentation | 2 | _pending_ | _pending_ | Append `## F5 — Prescriptive 4-anchor recipe` to `docs/prompt-caching-mechanics.md`: document Tier 1 (stable prefix end), Tier 2 (bundle end), Tier 3 (spec end), Tier 4 (last executor-mutable block); note 4-anchor Anthropic cap; note how this complements T10.7 prohibitive rule (forbids >1 stable-prefix block) with prescriptive layout guidance. `npm run validate:all`. |

---

#### Stage 4.3 — Skills navigator MCP tool + harness-gated tracking (F6 + F3 + F7)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Ship `skill_for_task(keywords, lifecycle_stage)` MCP tool replacing the pattern-match-yourself rule (`ia/skills/README.md` is 150 lines). File tracking issues for F3 (harness-level caveman enforcement, `PreCompletion` hook) and F7 (`defer_loading: true` rollout) — zero code change for tracking items; monitoring only pending Anthropic harness confirmation.

**Exit:**

- MCP tool `skill_for_task` registered in `tools/mcp-ia-server/src/index-ia.ts`; returns `{skill_name, skill_path, url, first_phase_body}` for keywords + lifecycle_stage query.
- Integration test: `skill_for_task("implement spec", "implement")` returns path matching `ia/skills/project-spec-implement/SKILL.md`.
- Tracking BACKLOG issues filed for F3 + F7 with `harness-gated` label and dependency note on Anthropic harness capability.
- `docs/session-token-latency-audit-exploration.md` §Open questions: F3 + F7 tracking issues linked.
- `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — Skills navigator MCP tool (F6).
- [ ] Phase 2 — Harness-gated tracking issues (F3 + F7).

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T4.3.1 | skill_for_task MCP tool | 1 | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/skill-for-task.ts`: `registerTool("skill_for_task", ...)` with inputs `keywords: string[]`, `lifecycle_stage?: string`; reads `ia/skills/README.md` index + each `ia/skills/*/SKILL.md` frontmatter (`title`, `phases`, `trigger` fields); computes keyword overlap score; returns top-1 match with `{skill_name, skill_path, url, first_phase_body}` (first phase body = first `### Phase 1` section text, ≤500 tokens). Register in `index-ia.ts`. |
| T4.3.2 | skill_for_task integration test | 1 | _pending_ | _pending_ | Author `tools/mcp-ia-server/tests/skill-for-task.test.ts`: assert `skill_for_task(["implement", "spec"], "implement")` returns path containing `project-spec-implement`; assert `skill_for_task(["stage", "file"], "stage-file")` returns path containing `stage-file-plan`. Add `npm run test:skill-for-task`. `npm run validate:all`. |
| T4.3.3 | F3 tracking issue | 2 | _pending_ | _pending_ | File `/project-new TECH-{id}: Track F3 harness-level caveman enforcement (PreCompletion hook)`: notes that `output-style: caveman` frontmatter in skill files requires `PreCompletion` hook support from Claude Code harness; links to `docs/session-token-latency-audit-exploration.md` §F3; blocked until Anthropic harness team confirms `PreCompletion` semantics. Zero code change. Link filed issue from exploration doc §Open questions Q3. |
| T4.3.4 | F7 tracking issue | 2 | _pending_ | _pending_ | File `/project-new TECH-{id}: Track F7 defer_loading: true MCP rollout`: monitors Claude Code release notes for `defer_loading: true` per-tool support; links to exploration §F7 + audit source; antidote to B1 two-server split when harness supports per-tool deferred loading. Zero code change until harness confirms. Link filed issue from exploration §Open questions. |

---

### Step 5 — Synthesized context pack (D5)

**Status:** In Progress — Stage 5.1

**Backlog state (Step 5):** 4 filed (TECH-520..TECH-523 — Stage 5.1)

**Objectives:** Extend compact-survival from Stage 3.1's last-3-tools signal into a full synthesized context pack written on PreCompact event and re-injected on SessionStart. Agents resuming after `/compact` recover active focus + surfaces + recent decisions + open questions from `.claude/context-pack.md` without re-reading source files. Hook stays shell-only (no `claude -p` subprocess) to keep compact path fast (<200 ms) and deterministic. Semantic placement = Stage 3.3 of Step 3; filed here as Step 5 per skill append-only contract — human reviewer may relocate block post-apply.

**Pre-conditions:** Step 3 Stage 3.1 T3.1.2 (`.claude/runtime-state.json` skeleton) + T3.1.3 (`compact-summary.sh` Stop/PostCompact hook) must be Done before Step 5 Stage 5.1 starts. Stage 4.1 T4.1.1 (`.claude/tool-usage.jsonl`) = soft dependency — optional `Recent memoized calls` section omitted silently if absent.

**Exit criteria:**

- `tools/scripts/claude-hooks/context-pack.sh` (new) executable; emits `.claude/context-pack.md` per §2 schema of extensions doc (Active focus + Relevant surfaces + Recent decisions + Open questions + Last tool outputs + Loaded context sources).
- `.claude/settings.json` hooks array gains PreCompact entry invoking `context-pack.sh`.
- `.claude/context-pack.md` session-ephemeral, gitignored.
- Size cap 300 lines enforced via awk at block boundaries (Recent decisions drop first, then Open questions; Relevant surfaces never truncated).
- `tools/scripts/claude-hooks/session-start-prewarm.sh` extended to cat pack content after deterministic block + `---` separator, gated by file existence + 24 h freshness.
- Stale pack (>24 h) → stderr warning, no stdout emission. Missing pack → silent.
- Manual re-orientation integration test passes: session → 2 Read + 2 Edit → `/compact` → resume → agent cites active task + stage + ≥2 relevant surfaces with zero Read calls on source files before first answer.
- `docs/agent-led-verification-policy.md` §Session continuity extended with pack re-injection contract (schema, freshness gate, truncation policy).
- `npm run validate:all` green.

**Art:** None.

**Relevant surfaces (load when step opens):**
- `docs/session-token-latency-post-mvp-extensions.md` §1–§4 (extension rationale + schema + Stage header) + §5 (pre-authored §Audit Notes / §Examples / §Test Blueprint / §Acceptance per task).
- `docs/session-token-latency-audit-exploration.md` §D4 — compact-survival origin row.
- `tools/scripts/claude-hooks/session-start-prewarm.sh` (exists) — re-injection extension target.
- `tools/scripts/claude-hooks/compact-summary.sh` (new, Stage 3.1 T3.1.3) — sibling Stop/PostCompact hook; `context-pack.sh` is PreCompact counterpart.
- `tools/scripts/claude-hooks/context-pack.sh` (new) — primary deliverable.
- `.claude/runtime-state.json` (new, Stage 3.1 T3.1.2) — primary digest input.
- `.claude/settings.json` (exists) — PreCompact hook entry addition.
- `.claude/telemetry/{session-id}.jsonl` (exists post-Stage 1) — secondary digest input.
- `.claude/tool-usage.jsonl` (new, Stage 4.1 T4.1.1, soft dep) — optional tertiary input.
- `ia/projects/session-token-latency-master-plan.md` (this file) — Stage block regex parser target.
- `docs/agent-led-verification-policy.md` (exists) — §Session continuity re-injection contract destination.
- `.gitignore` (exists) — ephemeral marker addition.
- Prior step outputs: Step 4 (`runtime-state.json` full schema + `tool-usage.jsonl` from Stage 4.1) + Step 3 (compact-summary.sh sibling hook + §Session continuity doc sub-section from T3.1.4).

---

#### Stage 5.1 — PreCompact digest + SessionStart re-injection

**Status:** In Progress — Stage 5.1 (TECH-520, TECH-521, TECH-522, TECH-523)

**Pre-conditions:** Stage 3.1 T3.1.2 + T3.1.3 Done. Stage 4.1 T4.1.1 optional (soft).

**Objectives:** Author `context-pack.sh` on PreCompact event writing `.claude/context-pack.md` per schema; wire into `.claude/settings.json`; extend `session-start-prewarm.sh` to re-inject pack content after deterministic block (volatile suffix zone — preserves Stage 3.1 D2 cacheable prefix); enforce 300-line cap + 24 h freshness gate. Land manual re-orientation integration test evidence + docs update.

**Exit:**

- `tools/scripts/claude-hooks/context-pack.sh` exists, executable, shebang `#!/usr/bin/env bash`, `set -uo pipefail` (no `-e` — graceful partial failure). Reads `.claude/runtime-state.json` + active plan Stage block + telemetry jsonl; emits schema per extensions doc §2.
- `.claude/settings.json` hooks array contains PreCompact entry running `context-pack.sh`.
- Size cap 300 lines enforced via awk block-boundary truncation; Relevant surfaces never truncated; truncation marker `_[...truncated N oldest decisions]_` emitted when cap triggers.
- `session-start-prewarm.sh` cats pack content after deterministic block + `---` separator, gated on `-f .claude/context-pack.md` AND pack `ts` header <24 h old.
- Deterministic prefix byte-stable across runs (verified by diff of two runs with different pack content).
- `.claude/context-pack.md` added to `.gitignore`.
- Manual re-orientation test passes per `docs/agent-led-verification-policy.md` §Session continuity protocol; evidence linked (screenshot + tool-call log) in task Verification block.
- `docs/agent-led-verification-policy.md` §Session continuity extended with ≥3-line "Context pack re-injection" paragraph covering schema, freshness gate, truncation policy.
- `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — Digest script authoring (schema + runtime-state + telemetry + size cap).
- [ ] Phase 2 — Re-injection + integration test + docs.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T5.1.1 | PreCompact digest script — schema + runtime-state | 1 | **TECH-520** | Draft | Author `tools/scripts/claude-hooks/context-pack.sh`: on PreCompact event reads `.claude/runtime-state.json` → extracts `active_task_id`, `active_stage`, `queued_test_scenario_id`, `last_verify_exit_code`, `last_bridge_preflight_exit_code`; parses active master plan Stage block via same narrow regex `/ship-stage` Phase 0 uses (Stage name + Exit criteria first 5 bullets + Relevant surfaces first 20 lines); emits `.claude/context-pack.md` per extensions doc §2 schema (Active focus + Relevant surfaces + Loaded context sources sections). Add PreCompact hook entry to `.claude/settings.json` hooks array. Add `.claude/context-pack.md` to `.gitignore`. All `jq` calls guarded with `\|\| echo "unknown"`; exit 0 on partial failure; `# Context pack — SCHEMA MISMATCH` marker on malformed runtime-state.json. No `claude -p` subprocess. |
| T5.1.2 | Digest script — telemetry + tool-usage + size cap | 1 | **TECH-521** | Draft | Extend `context-pack.sh`: append `Last tool outputs (pointers only)` section from `.claude/telemetry/{session-id}.jsonl` (last 10 rows via `tail -10 \| jq -c '{name, exit, ts}'`); if `.claude/tool-usage.jsonl` exists (Stage 4.1 T4.1.1), append `Recent memoized calls` section with top 10 `{tool_name, args_hash_short, result_hash_short, ts}`. Enforce 300-line cap via awk truncation at Recent decisions / Open questions block boundaries (blank-line delimited, not mid-line): drop oldest Recent decisions block first, then oldest Open questions. Emit `_[...truncated N oldest decisions]_` marker when truncation fires. Relevant surfaces never truncated. Soft-guard missing files with `[ -f ... ]` checks. |
| T5.1.3 | SessionStart re-injection + deterministic preamble compat | 2 | **TECH-522** | Draft | Extend `tools/scripts/claude-hooks/session-start-prewarm.sh` (Stage 3.1 T3.1.1): after deterministic preamble block + `---` separator, if `-f .claude/context-pack.md` AND pack `ts` header <24 h old, then `cat .claude/context-pack.md`. Stale pack (>24 h) → stderr warning `stale context pack ({age_hours} h old); regenerate via /pack-context`, no stdout emission. Missing pack → silent, no stdout or stderr. Platform-agnostic ts parsing (macOS BSD `date -jf` + GNU `date -d` fallback). Placement in volatile suffix preserves Stage 3.1 D2 deterministic prefix cacheability — verify via diff of two runs. Document re-injection contract in `docs/agent-led-verification-policy.md` §Session continuity (extend sub-section first added by Stage 3.1 T3.1.4). |
| T5.1.4 | Re-orientation integration test + validate:all | 2 | **TECH-523** | Draft | Manual integration test per protocol in extensions doc §5 T3.3.4 §Examples: start session on filed task → 2 Read + 2 Edit on 4 distinct source files → `/compact` → inspect `.claude/context-pack.md` (Active focus populated; Relevant surfaces lists all 4 files; ≥1 Recent decision; Last tool outputs lists last 4 actions); resume session (new terminal) → verify SessionStart preamble includes pack content; ask agent "what are you working on?" → confirm model cites active task + stage + ≥2 relevant surfaces with **zero** Read calls on source files before first answer. Screenshot + tool-call log evidence linked in task Verification block. `docs/agent-led-verification-policy.md` §Session continuity updated with full re-injection contract (≥3-line paragraph). `npm run validate:all` green. |

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-520"
  task_key: "T5.1.1"
  title: "PreCompact digest script — schema + runtime-state"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Author tools/scripts/claude-hooks/context-pack.sh. On PreCompact event reads
    .claude/runtime-state.json (active_task_id, active_stage, queued_test_scenario_id,
    last_verify_exit_code, last_bridge_preflight_exit_code); parses active master plan
    Stage block via narrow regex; emits .claude/context-pack.md per extensions doc §2
    schema (Active focus + Relevant surfaces + Loaded context sources). Wire PreCompact
    hook entry into .claude/settings.json. Gitignore pack. No claude -p subprocess.
    Graceful partial failure: jq calls guarded with `|| echo "unknown"`, exit 0,
    SCHEMA MISMATCH marker on malformed runtime-state.json.
  depends_on: []
  related:
    - "T5.1.2"
    - "T5.1.3"
    - "T5.1.4"
  stub_body:
    summary: |
      PreCompact hook script emits .claude/context-pack.md digesting runtime-state + active
      Stage block. Core deliverable of Stage 5.1 Phase 1. Shell-only, <200 ms, deterministic.
      Sibling to Stage 3.1 compact-summary.sh (Stop/PostCompact counterpart).
    goals: |
      - tools/scripts/claude-hooks/context-pack.sh executable, shebang bash, set -uo pipefail.
      - Reads .claude/runtime-state.json + active Stage block via same narrow regex /ship-stage Phase 0 uses.
      - Emits §2-schema sections: Active focus, Relevant surfaces, Loaded context sources.
      - PreCompact hook entry wired in .claude/settings.json.
      - Pack gitignored (session-ephemeral).
      - Graceful partial failure: exit 0 on malformed inputs; SCHEMA MISMATCH marker present.
    systems_map: |
      New: tools/scripts/claude-hooks/context-pack.sh.
      Touches: .claude/settings.json (hooks array), .gitignore.
      Reads: .claude/runtime-state.json (Stage 3.1 T3.1.2), active master plan Stage block,
      ia/projects/session-token-latency-master-plan.md.
      Writes: .claude/context-pack.md (gitignored).
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 1 — Digest script + hook wire.
      1. Draft context-pack.sh skeleton (shebang, set -uo pipefail, schema header emit).
      2. Parse runtime-state.json via jq; extract 5 keys; fallback to "unknown" per key.
      3. Regex-extract active Stage block from master plan (Stage header + Exit criteria first 5 + Relevant surfaces first 20 lines).
      4. Emit §2 schema sections to .claude/context-pack.md.
      5. Add PreCompact hook entry to .claude/settings.json.
      6. Add .claude/context-pack.md to .gitignore.
      7. Smoke-test: synthetic runtime-state.json + malformed variant both exit 0.
      8. npm run validate:all.
```

```yaml
- reserved_id: "TECH-521"
  task_key: "T5.1.2"
  title: "Digest script — telemetry + tool-usage + size cap"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Extend context-pack.sh with telemetry tail (last 10 rows of
    .claude/telemetry/{session-id}.jsonl → Last tool outputs section) and optional
    tool-usage block (Stage 4.1 T4.1.1 soft dep → Recent memoized calls, top 10).
    Enforce 300-line size cap via awk truncation at Recent decisions / Open questions
    block boundaries (blank-line delimited); drop oldest Recent decisions first, then
    oldest Open questions; Relevant surfaces never truncated. Emit truncation marker
    `_[...truncated N oldest decisions]_`. Soft-guard missing files via `[ -f ... ]`.
  depends_on: []
  related:
    - "T5.1.1"
    - "T5.1.3"
    - "T5.1.4"
  stub_body:
    summary: |
      Second half of Phase 1 digest authoring. Adds telemetry + memoization sections and
      enforces 300-line cap with block-boundary truncation. Keeps Relevant surfaces
      untouched (hard invariant — cited surfaces must always survive).
    goals: |
      - Last tool outputs section populated from .claude/telemetry/{session-id}.jsonl tail -10.
      - Recent memoized calls section populated from .claude/tool-usage.jsonl if present; omitted silently if absent.
      - 300-line cap enforced via awk at blank-line block boundaries (no mid-line cuts).
      - Drop order: oldest Recent decisions → oldest Open questions.
      - Relevant surfaces block never truncated.
      - Truncation marker emitted when cap fires.
    systems_map: |
      Touches: tools/scripts/claude-hooks/context-pack.sh (extends T5.1.1 deliverable).
      Reads: .claude/telemetry/{session-id}.jsonl (Stage 1 output), .claude/tool-usage.jsonl (Stage 4.1 soft dep).
      Writes: .claude/context-pack.md.
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 1 — Telemetry + cap.
      1. Append Last tool outputs section via tail -10 + jq -c pipe.
      2. Guard tool-usage.jsonl with [ -f ... ]; if present, jq top-10 {tool_name, args_hash_short, result_hash_short, ts}.
      3. Author awk block-boundary truncation pass: count lines; if >300, drop oldest Recent decisions block, recount, repeat with Open questions.
      4. Emit `_[...truncated N oldest decisions]_` marker on drop.
      5. Smoke-test: oversized synthetic pack → truncation fires; Relevant surfaces intact.
      6. npm run validate:all.
```

```yaml
- reserved_id: "TECH-522"
  task_key: "T5.1.3"
  title: "SessionStart re-injection + deterministic preamble compat"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Extend tools/scripts/claude-hooks/session-start-prewarm.sh (Stage 3.1 T3.1.1 output):
    after deterministic preamble block + `---` separator, if
    -f .claude/context-pack.md AND pack ts header <24 h old, cat pack content.
    Stale pack (>24 h) → stderr warning, no stdout. Missing pack → silent.
    Platform-agnostic ts parsing (macOS BSD `date -jf` + GNU `date -d` fallback).
    Placement in volatile suffix preserves Stage 3.1 D2 deterministic prefix
    cacheability — verify via diff of two runs. Extend
    docs/agent-led-verification-policy.md §Session continuity sub-section first added
    by Stage 3.1 T3.1.4.
  depends_on: []
  related:
    - "T5.1.1"
    - "T5.1.2"
    - "T5.1.4"
  stub_body:
    summary: |
      Phase 2 re-injection half. Extends session-start-prewarm.sh to cat pack content in
      volatile suffix zone, preserving Stage 3.1 D2 cacheable deterministic prefix.
      Implements 24 h freshness gate + graceful absence.
    goals: |
      - session-start-prewarm.sh cats .claude/context-pack.md after deterministic block + `---` separator.
      - Existence gate (-f) + 24 h freshness gate on pack ts header.
      - Stale pack → stderr warning, no stdout.
      - Missing pack → silent (no stderr, no stdout).
      - BSD + GNU date parsing both supported.
      - Deterministic prefix byte-stable across runs (diff-verified).
      - §Session continuity doc sub-section extended with re-injection contract.
    systems_map: |
      Touches: tools/scripts/claude-hooks/session-start-prewarm.sh (Stage 3.1 T3.1.1 output).
      Touches: docs/agent-led-verification-policy.md §Session continuity.
      Reads: .claude/context-pack.md.
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 2 — Re-injection.
      1. Append conditional block to session-start-prewarm.sh: `-f` + ts-age check.
      2. Portable age-compute: try `date -jf` (BSD) first, fallback `date -d` (GNU).
      3. 24 h gate → cat; stale → stderr warning line, no stdout.
      4. Verify deterministic prefix byte-stable: run twice with different pack content; diff preamble up to `---` separator (must be identical).
      5. Extend docs/agent-led-verification-policy.md §Session continuity with re-injection contract.
      6. npm run validate:all.
```

```yaml
- reserved_id: "TECH-523"
  task_key: "T5.1.4"
  title: "Re-orientation integration test + validate:all"
  priority: "high"
  issue_type: "TECH"
  notes: |
    Manual integration test per extensions doc §5 T3.3.4 §Examples protocol.
    Start session on filed task → 2 Read + 2 Edit on 4 distinct source files →
    /compact → inspect .claude/context-pack.md (Active focus populated; 4 Relevant
    surfaces listed; ≥1 Recent decision; Last tool outputs has 4 actions) → resume
    session (new terminal) → verify SessionStart preamble includes pack content →
    ask "what are you working on?" → confirm model cites active task + stage +
    ≥2 relevant surfaces with zero Read calls before first answer. Screenshot +
    tool-call log linked in Verification block. Extend
    docs/agent-led-verification-policy.md §Session continuity with full re-injection
    contract (≥3-line paragraph). npm run validate:all green.
  depends_on: []
  related:
    - "T5.1.1"
    - "T5.1.2"
    - "T5.1.3"
  stub_body:
    summary: |
      Final gating task for Stage 5.1. Validates end-to-end re-orientation UX via manual
      test protocol. Evidence-linked Verification. Docs extended. validate:all green
      unblocks Stage 5.1 closeout.
    goals: |
      - Manual integration test executed per extensions §5 protocol; evidence captured.
      - Pack file content verified: Active focus, Relevant surfaces (all 4 files),
        ≥1 Recent decision, Last tool outputs (4 actions).
      - Resumed session's SessionStart preamble includes pack content.
      - Agent cites active task + stage + ≥2 relevant surfaces with zero pre-answer Reads.
      - docs/agent-led-verification-policy.md §Session continuity has ≥3-line re-injection paragraph.
      - npm run validate:all green.
    systems_map: |
      Touches: docs/agent-led-verification-policy.md §Session continuity.
      Reads: .claude/context-pack.md, session preamble stdout.
      No Unity / C# / runtime surface touched.
    impl_plan_sketch: |
      Phase 2 — Integration test + docs.
      1. Run test protocol: 2 Read + 2 Edit → /compact → inspect pack → resume → query agent.
      2. Capture screenshot + tool-call log; attach to Verification block.
      3. Extend §Session continuity sub-section with ≥3-line re-injection contract paragraph.
      4. npm run validate:all green.
      5. Stage 5.1 closeout unblocked.
```

### §Plan Fix — PASS (no drift)

<!-- plan-review verdict — 2026-04-20 — Stage 5.1 (TECH-520/521/522/523) — 12/12 checks pass; downstream continue. -->

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` runs.
- Run `claude-personal "/stage-file ia/projects/session-token-latency-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Stage 1.1 gates Stage 1.2 — confirm `tools/scripts/agent-telemetry/baseline-summary.json` committed before starting Stage 1.2.
- Stage 2.x gates: confirm lifecycle-refactor Stage 10 T10.2 + T10.4 Done before filing Stage 2.1 tasks. Check `ia/projects/lifecycle-refactor-master-plan.md` Stage 10 status.
- Stage 4.2 recommendation: confirm lifecycle-refactor T10.7 Done before filing Stage 4.2 tasks (prescriptive F5 complements T10.7 prohibitive guardrail).
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc.
- Pass 2 (`/master-plan-extend` against `mcp-lifecycle-tools-opus-4-7-audit-master-plan.md`) should be invoked after Stage 1.2 B1 server-split decision is durable; B4 dist build in the MCP extension depends on knowing the split target.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers `Status: Final`; the file stays.
- Silently promote Theme B MCP-surface items (B4/B5/B6/B8/B9) into this orchestrator — they belong in the `/master-plan-extend` pass against the MCP plan.
- Merge partial stage state — every stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Re-order Stages 2.x before confirming T10.2 + T10.4 landed — same agent bodies edited; churning twice is waste.
- Introduce any C# runtime / Unity bridge / product-correctness changes — all items in this plan are tooling-only.
