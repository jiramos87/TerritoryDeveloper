---
name: stage-file
purpose: >-
  DB-backed single-skill stage-file: mode detection + cardinality + sizing gates + per-task
  task_insert MCP writes + manifest append + spec stub + task-table flip + R1/R2 Status flips.
audience: agent
loaded_by: "skill:stage-file"
slices_via: none
description: >-
  DB-backed single-skill filing. Loads shared Stage MCP bundle once; gates cardinality (≥2 Tasks per
  Stage) + sizing (H1–H6); batch-verifies Depends-on ids via single `backlog_list`; resolves target
  BACKLOG.md section from master-plan H1 title; per-Task writes via `task_insert` MCP tool (DB-backed
  monotonic id from per-prefix sequence — no reserve-id.sh); appends manifest entry to
  `ia/state/backlog-sections.json`; bootstraps task spec body in DB via `task_spec_section_write`;
  runs `materialize-backlog.sh` (DB source default) — exit code is the filing gate; atomic task-table
  flip + R1/R2 Status flips. No yaml file written under `ia/backlog/`. Triggers: "/stage-file
  {orchestrator-path} Stage 1.2", "file stage tasks", "bulk create stage issues", "create backlog rows
  for Stage X.Y", "bootstrap issues for pending stage tasks", "compress stage tasks", "merge draft
  tasks". Argument order (explicit): SLUG first, STAGE_ID second.
phases:
  - Mode detection
  - Load shared Stage MCP bundle
  - Read Stage block + cardinality + sizing gates
  - Batch Depends-on verification
  - Resolve target manifest section
  - Per-task iterator (task_insert + spec stub + manifest append)
  - "Post-loop: materialize + validate + task-table + R1/R2 flips"
  - Return to dispatcher
triggers:
  - /stage-file {orchestrator-path} Stage 1.2
  - file stage tasks
  - bulk create stage issues
  - create backlog rows for Stage X.Y
  - bootstrap issues for pending stage tasks
  - compress stage tasks
  - merge draft tasks
argument_hint: {master-plan-path} Stage {X.Y} [--force-model {model}]
model: opus
reasoning_effort: high
tools_role: pair-head
tools_extra:
  - mcp__territory-ia__backlog_list
  - mcp__territory-ia__backlog_record_validate
  - mcp__territory-ia__backlog_search
  - mcp__territory-ia__spec_outline
  - mcp__territory-ia__list_specs
  - mcp__territory-ia__invariant_preflight
  - mcp__territory-ia__master_plan_render
  - mcp__territory-ia__stage_render
  - mcp__territory-ia__master_plan_preamble_write
  - mcp__territory-ia__master_plan_change_log_append
  - mcp__territory-ia__task_insert
  - mcp__territory-ia__task_spec_section_write
  - mcp__territory-ia__lifecycle_stage_context
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
  - BACKLOG row text + spec stub prose (Notes + acceptance caveman; row structure verbatim per agent-output-caveman-authoring)
hard_boundaries:
  - Do NOT write yaml under `ia/backlog/` — DB is source of truth.
  - Do NOT call `reserve-id.sh` — per-prefix DB sequences own id assignment via `task_insert` MCP.
  - Do NOT re-query `backlog_issue` per Task — Phase 3 batch-verified.
  - Do NOT reorder Tasks — apply in task-table order.
  - Do NOT update task-table mid-loop — atomic Edit after Phase 6.1+6.2 exit 0.
  - Do NOT edit `BACKLOG.md` directly — `materialize-backlog.sh` regenerates from DB + manifest.
  - "Do NOT run `validate:backlog-yaml` — no yaml written on DB path."
  - "Do NOT run `validate:all` — gate is `materialize-backlog.sh` exit 0 only."
  - "Do NOT call `task_spec_section_write` for `raw_markdown` — that MCP writes `body` column only; dispatch `task_raw_markdown_write` directly after `task_insert` returns `ISSUE_ID`."
caller_agent: stage-file
---

# Stage-file skill — DB-backed single-skill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Single-skill filing. Reads Stage block; gates cardinality + sizing; writes DB rows via `task_insert` MCP (no yaml); appends manifest entry; bootstraps spec stub; regenerates BACKLOG.md via DB path; flips Status lines.

**Canonical master-plan shape:** [`docs/MASTER-PLAN-STRUCTURE.md`](../../../docs/MASTER-PLAN-STRUCTURE.md) — authoritative. Stage heading H3 `### Stage N.M`; 5-col Task table `| Task | Name | Issue | Status | Intent |` (no Phase column); Task id `T{N}.{M}.{K}`.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `SLUG` | 1st arg | Bare master-plan slug (e.g. `blip`). DB-first via `master_plan_render({slug})`. |
| `STAGE_ID` | 2nd arg | e.g. `5` or `Stage 5` or `7.2`. |
| `ISSUE_PREFIX` | 3rd arg or default | `TECH` / `FEAT` / `BUG` / `ART` / `AUDIO` — default `TECH` (no dash). |

---

## Phase 0 — Mode detection

Scan target Stage task table **before any other action**. Count by status:

| Mode | Condition | Route |
|------|-----------|-------|
| **File mode** | ≥1 `_pending_` task, 0 `Draft` tasks | Continue Phase 1. |
| **Compress mode** | 0 `_pending_`, ≥1 `Draft` | STOP; instruct caller to route to [`stage-compress`](../stage-compress/SKILL.md). |
| **Mixed mode** | ≥1 `_pending_` + ≥1 `Draft` | File pending first (this skill), then offer Compress on resulting Drafts. |
| **No-op** | 0 `_pending_`, 0 `Draft` | Report stage state + exit. |

`In Review`, `In Progress`, `Done` tasks — skip in all modes. Never touch active/closed work.

---

## Phase 1 — Load shared Stage MCP bundle

Call **once** at Stage open:

```
mcp__territory-ia__lifecycle_stage_context({
  slug: "{SLUG}",
  stage_id: "{STAGE_ID}"
})
```

Returns `{stage_header, task_spec_bodies, glossary_anchors, invariants, pair_contract_slice}`. Store as **shared context block** — reused across all Tasks in Stage.

---

## Phase 2 — Read Stage block + cardinality + sizing gates

### 2.1 Read Stage block via DB

`SLUG` already provided as 1st arg. Master plan body lives in DB. Call:

```
mcp__territory-ia__stage_render({ slug: "{SLUG}", stage_id: "{STAGE_ID}" })
```

Returns `{stage_id, title, status, objective, exit_criteria, tasks[], block_md}`. `block_md` is the rendered H3 `### Stage {STAGE_ID}` block (canonical shape — H4 legacy not produced by renderer).

Parse `block_md` Task-table rows. Collect `_pending_` rows into `pending_tasks[]` in task-table order. Each row = `{task_key: "T{STAGE_ID}.{K}", name, intent, priority}`.

`stage_render` not-found → halt `{reason: "stage {STAGE_ID} not found in ia_stages for slug {SLUG}"}`.

### 2.2 Read plan title via DB

```
mcp__territory-ia__master_plan_render({ slug: "{SLUG}" })
```

Returns `{slug, title, preamble, stages[]}`. Store `PLAN_TITLE` = `title`. `SLUG` already derived in 2.1. Used as `task_insert.slug` arg + manifest resolution fallback.

### 2.2b Surface-path verify (warn-only)

Parse `block_md` `**Relevant surfaces (load when stage opens):**` bullets. Extract every repo-relative file path token (skip URLs, MCP tool names, glossary refs). Call:

```
mcp__territory-ia__plan_digest_verify_paths({ paths: [...extracted] })
```

Per returned `{path, exists}` row:

- `exists: true` → ok.
- `exists: false` AND not annotated `(new)` in source bullet → emit warning: `SURFACE_PATH_MISS Stage {STAGE_ID}: '{path}' cited in Relevant surfaces but not on disk + missing (new) marker.`

Warn-only — does NOT halt filing. Rationale: catches drift before spec stubs inherit ghost paths into §4.2 Systems map at 5.A.3 (real-world dogfood: `parallel-carcass-rollout` Stage 2.1 cited `tools/mcp-ia-server/test/arch-drift-scan*.ts` — singular `test/` dir doesn't exist; real path uses plural `tests/tools/`). Halting on miss is too aggressive — author may legitimately reference a path that lands in this Stage's first task. User decides whether to abort + re-author Stage block or proceed.

Skip step entirely when `block_md` carries no Relevant surfaces section (greenfield Stage).

### 2.3 Cardinality gate

Run [`cardinality-gate-check`](../cardinality-gate-check/SKILL.md) on `pending_tasks`:

- `verdict = pause` → surface violations (product/designer phrasing per [`agent-human-polling.md`](../../rules/agent-human-polling.md)); wait for user confirmation.
- `verdict = proceed` → continue.

### 2.4 Sizing gate

Evaluate [`ia/rules/stage-sizing-gate.md`](../../rules/stage-sizing-gate.md) H1–H6:

- **PASS** (all PASS or ≤1 WARN) → continue.
- **WARN-gate** (≥2 WARN, no FAIL) → emit warning; ask user to confirm or split. Do NOT proceed without confirmation.
- **FAIL** (any heuristic FAIL) → **HALT**. Emit:

  ```
  SIZING GATE FAIL — Stage {STAGE_ID}
  Failed: {H-ids with rationale}
  Action: re-route to /stage-decompose to split Stage {STAGE_ID} → {STAGE_ID.A} / {STAGE_ID.B}.
  No rows written. Halt.
  ```

  No DB writes. Stop.

- **Waiver present** (sizing-gate-waiver comment in Stage block) → skip eval; proceed.

---

## Phase 3 — Batch Depends-on verification

Handled by subagent pre-recipe. See `ia/skills/stage-file/agent-body.md` §Recipe step 4.

---

## Phase 4 — Resolve target manifest section

`task_insert` writes the DB row but BACKLOG.md regen needs the manifest (`ia/state/backlog-sections.json`) to know which section the new issues belong to.

### 4.1 Slug-based match heuristic

1. Load manifest: `cat ia/state/backlog-sections.json | jq '.sections[].header'` (or Read + parse).
2. Normalize each manifest header: strip `## `, trim, lowercase, strip punctuation, replace spaces with `-`.
3. Candidate slugs (in order): `SLUG` (from filename); kebab-case of `PLAN_TITLE`; kebab-case of `PLAN_TITLE` with `-program` / `-lane` suffix stripped.
4. Match normalized manifest header against each candidate (prefix / substring / exact). First unique match → `TARGET_SECTION_HEADER`.
5. Ambiguous (0 or 2+ matches) → fall through to 4.2.

### 4.2 User prompt fallback

Emit caveman-terse option list (section headers verbatim from manifest). Ask user to pick one. Wait for reply. Store picked header as `TARGET_SECTION_HEADER`.

```
section match ambiguous for plan "{PLAN_TITLE}" (slug: {SLUG}).
pick target BACKLOG section:

  1. ## Compute-lib program
  2. ## Agent ↔ Unity & MCP context lane
  ...
  N. ## How to Use This Backlog

reply with number.
```

### 4.3 Persist for Phase 5

Store `TARGET_SECTION_HEADER`. Used in Phase 5 step 3 (manifest append).

---

## Phase 5 — Per-task iterator

Two-pass over `pending_tasks[]` (task-table order):

- **5.A insert pass** — `task_insert` per Task with `depends_on=[]` + `related=[]` (deps deferred). Capture `task_key → ISSUE_ID` map.
- **5.B dep registration pass** — post-insert; resolve same-batch `task_key` → `ISSUE_ID` via map; register `external + same_batch` edges via `task_dep_register` MCP (atomic per-call Tarjan SCC cycle check; idempotent re-register).
- **5.C raw_markdown patch pass** — render row text using returned `ISSUE_ID`; persist to `ia_tasks.raw_markdown` column via `task_raw_markdown_write` MCP.

### 5.A.2 Call task_insert

```
mcp__territory-ia__task_insert({
  prefix, title, slug, stage_id, priority, notes,
  depends_on: [], related: [], raw_markdown: null, body, status, type
})
```

Returns `{task_id: "TECH-NNN", ...}`. Store `ISSUE_ID` = `task_id`. Append `{task_key, ISSUE_ID, title}` to `filed_tasks[]`.

Errors:

- `sequence_gap` / `unique_violation` → retry once; else escalate.

**Idempotency:** title+slug+stage_id triple already present → MCP returns existing `task_id`. Reuse.

### 5.B Dep registration (post all 5.A insertions)

Handled by subagent post-recipe. See `ia/skills/stage-file/agent-body.md` §Recipe step 5.

### 5.C `raw_markdown` patch (post 5.B)

Handled by subagent post-recipe. See `ia/skills/stage-file/agent-body.md` §Recipe step 6.

### 5.D Manifest append (post 5.C)

Read `ia/state/backlog-sections.json`. Locate section where `.header === TARGET_SECTION_HEADER`. Append to `.items[]`:

```json
{
  "type": "issue",
  "id": "{ISSUE_ID}",
  "checklist_line": "- [ ] **{ISSUE_ID} — {title}** — {notes-first-sentence}.",
  "trailing_blanks": 1
}
```

Write manifest back. Buffer per Task; flush after full 5.A→5.D loop to minimize disk churn.

---

## Phase 6 — Post-loop: materialize + validate + task-table + R1/R2 flips

Run after all Tasks processed.

### 6.1 Materialize BACKLOG.md (filing gate)

**Short-circuit on no-op:** if `filed_tasks.length === 0` (every Task hit the idempotent `task_insert` skip path in Phase 5.A.2 — zero new DB rows AND zero manifest appends in Phase 5.D) → SKIP materialize. Emit `materialize=skipped (no-op)` in Phase 7 report. Continue to Phase 6.3.

Otherwise:

```bash
bash tools/scripts/materialize-backlog.sh
```

DB source is default (Step 5). **Exit code is the filing gate** — non-zero → escalate `{reason: "materialize-backlog.sh failed: {stderr}"}`. Re-run idempotent; safe.

`validate:dead-project-specs` retired — script + scanner removed; legacy yaml-archive coupling no longer applies on DB path. `validate:backlog-yaml` skipped — no yaml written. `validate:all` not invoked here.

### 6.4 R2 — Stage Status flip via change_log

No `stage_status_flip` MCP exists yet (followup gap; only `stage_closeout_apply` sets `done`). Mid-lifecycle pending → in_progress flip persists via change-log entry on master plan:

```
mcp__territory-ia__master_plan_change_log_append({
  slug: "{SLUG}",
  kind: "stage_status_flip",
  body: "Stage {STAGE_ID}: Draft → In Progress ({N} tasks filed {YYYY-MM-DD})"
})
```

Followup: needs `stage_status_flip(slug, stage_id, status)` mutation that sets `ia_stages.status` enum directly. Until then, change-log entry is the audit record; renderer reads `ia_stages.status` (defaults `pending` until `stage_closeout_apply` sets `done`).

### 6.6 Regenerate progress dashboard (non-blocking)

```bash
npm run progress
```

Failure does NOT block Phase 7 — log exit code, continue.

---

## Phase 7 — Return to dispatcher

Emit report:

```
stage-file done. STAGE_ID={STAGE_ID} FILED={N} SKIPPED={K}
Filed: {ISSUE_ID_1} — {title_1}
       {ISSUE_ID_2} — {title_2}
       ...
Section: {TARGET_SECTION_HEADER}
Materialize: {ran|skipped (no-op)}
Validators: exit 0.
next=stage-file-chain-continue
```

**Downstream chain:** dispatcher `.claude/commands/stage-file.md` continues to `stage-authoring` → STOP. Final handoff:

- **N≥2:** `Next: claude-personal "/ship-stage {SLUG} Stage {STAGE_ID}"`
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"`

Hard rule: `/ship-stage` is multi-task only; N=1 uses `/ship`.

---

## Hard boundaries

- Do NOT write yaml under `ia/backlog/` — DB is source of truth.
- Do NOT call `reserve-id.sh` — per-prefix DB sequences own id assignment.
- Do NOT re-query `backlog_issue` per Task — Phase 3 batch-verified.
- Do NOT reorder Tasks — apply in task-table order.
- Do NOT update task-table mid-loop — atomic Edit after Phase 6.1 exit 0.
- Do NOT edit `BACKLOG.md` directly — `materialize-backlog.sh` regenerates from DB + manifest.
- Do NOT read or edit any `ia/projects/**` markdown — DB is source of truth; use `master_plan_render` / `stage_render` / `master_plan_preamble_write` / `master_plan_change_log_append` MCP tools.
- Do NOT call `domain-context-load` per Task — Phase 1 once per Stage.
- Do NOT pass forward-ref same-batch deps via `task_insert.depends_on` — validation throws `unknown dep targets`. Use Phase 5.B 2-pass split.
- Do NOT call `task_spec_section_write` for `raw_markdown` column — writes `body` only. Use Phase 5.C `task_raw_markdown_write` MCP.
- Do NOT call `validate:dead-project-specs` — script retired. Filing gate = `materialize-backlog.sh` exit 0.
- Do NOT commit — user decides.


---

## Changelog

### 2026-04-30 — `lifecycle_stage_context` glossary lookup `ENAMETOOLONG` on greenfield slug

**Status:** applied — fix landed pre-log (commit pending)

**Symptom:**
Recipe step `lifecycle_ctx` fails with `ENAMETOOLONG` on every parallel `/stage-file` invocation against slug `recipe-runner-phase-e`. Two failed `ia_recipe_runs` rows captured (run_ids `751587bae723a3f4` + `e3a82a7c3db5de83`, 2026-04-30 12:21). Blocks Phase 1 MCP bundle load — Stage cannot proceed.

**Root cause:**
`tools/mcp-ia-server/src/tools/lifecycle-stage-context.ts:160` called `parseGlossary(content)` passing the glossary file *body* string (read via `fs.readFileSync(glossaryPath, "utf8")` at line 159). `parseGlossary` signature is `parseGlossary(filePath: string)` — internally re-reads via `fs.readFileSync(filePath)`. Result: kernel got the entire glossary body as a path arg, exceeded `PATH_MAX` → `ENAMETOOLONG`. Type system did not catch — both args typed `string`. Manifested only when the glossary search hits a real path (greenfield slug `recipe-runner-phase-e` triggered the lookup; pre-existing slugs likely skipped that branch via cache or early return).

**Fix:**
```typescript
// before (lines 159-160)
const content = fs.readFileSync(glossaryPath, "utf8");
const rows: GlossaryEntry[] = parseGlossary(content);
// after (line 159)
const rows: GlossaryEntry[] = parseGlossary(glossaryPath);
```
Verified by re-running `npm run recipe:run -- stage-file --inputs /tmp/recipe-inputs-1.1.json` — `lifecycle_ctx` clears, recipe halts at expected `manifest_resolve` gate.

**Friction types:** regression, type-mismatch, silent-breakage.

**Rollout row:** wave-2-dogfood-pilot (slug `recipe-runner-phase-e`, first carcass+section pilot)

**Tracker aggregator:** [docs/parallel-carcass-rollout-skill-iteration.md#skill-iteration-log-aggregator](../../../docs/parallel-carcass-rollout-skill-iteration.md#skill-iteration-log-aggregator)

---
