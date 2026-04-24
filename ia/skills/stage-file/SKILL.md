---
purpose: "DB-backed single-skill stage-file: mode detection + cardinality + sizing gates + per-task task_insert MCP writes + manifest append + spec stub + task-table flip + R1/R2 Status flips. Replaces retired stage-file-plan + stage-file-apply pair."
audience: agent
loaded_by: skill:stage-file
slices_via: none
name: stage-file
description: >
  DB-backed single-skill filing — replaces legacy stage-file-plan (Opus pair-head) +
  stage-file-apply (Sonnet pair-tail) pair (retired 2026-04-24 Step 6 of
  `docs/ia-dev-db-refactor-implementation.md`). Loads shared Stage MCP bundle once;
  gates cardinality (≥2 Tasks per Stage) + sizing (H1–H6); batch-verifies Depends-on
  ids via single `backlog_list`; resolves target BACKLOG.md section from master-plan
  H1 title; per-Task writes via `task_insert` MCP tool (DB-backed monotonic id from
  per-prefix sequence — no reserve-id.sh); appends manifest entry to
  `ia/state/backlog-sections.json`; bootstraps `ia/projects/{ISSUE_ID}.md` spec stub
  from template; runs `materialize-backlog.sh` (DB source default) +
  `validate:dead-project-specs`; atomic task-table flip + R1/R2 Status flips.
  No yaml file written under `ia/backlog/`.
  Triggers: "/stage-file {orchestrator-path} Stage 1.2", "file stage tasks",
  "bulk create stage issues", "create backlog rows for Stage X.Y",
  "bootstrap issues for pending stage tasks", "compress stage tasks", "merge draft tasks".
  Argument order (explicit): ORCHESTRATOR_SPEC first, STAGE_ID second.
model: opus
phases:
  - "Mode detection"
  - "Load shared Stage MCP bundle"
  - "Read Stage block + cardinality + sizing gates"
  - "Batch Depends-on verification"
  - "Resolve target manifest section"
  - "Per-task iterator (task_insert + spec stub + manifest append)"
  - "Post-loop: materialize + validate + task-table + R1/R2 flips"
  - "Return to dispatcher"
---

# Stage-file skill — DB-backed single-skill

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Single-skill filing. Reads Stage block; gates cardinality + sizing; writes DB rows via `task_insert` MCP (no yaml); appends manifest entry; bootstraps spec stub; regenerates BACKLOG.md via DB path; flips Status lines.

**Canonical master-plan shape:** [`ia/projects/MASTER-PLAN-STRUCTURE.md`](../../projects/MASTER-PLAN-STRUCTURE.md) — authoritative. Stage heading H3 `### Stage N.M`; 5-col Task table `| Task | Name | Issue | Status | Intent |` (no Phase column); Task id `T{N}.{M}.{K}`.

**Step 6 of `docs/ia-dev-db-refactor-implementation.md`** — this skill replaces retired [`stage-file-plan/SKILL.md`](../_retired/stage-file-plan/SKILL.md) + [`stage-file-apply/SKILL.md`](../_retired/stage-file-apply/SKILL.md). Seam #2 pair collapsed to single invocation per design doc B1.

Original pre-refactor monolith archived: [`ia/skills/_retired/stage-file-monolith/SKILL.md`](../_retired/stage-file-monolith/SKILL.md).

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `ORCHESTRATOR_SPEC` | 1st arg | Repo-relative path to `ia/projects/{master-plan}.md`. Glob fallback only when exactly one `*-master-plan.md` exists. |
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

**Upstream Stage tail guard:** before No-op treats as "nothing to do", agent MAY run `npm run validate:master-plan-status -- --plan {ORCHESTRATOR_SPEC}`. If **[R6]** on an earlier Stage → hand off `/ship-stage` or `/closeout` for that Stage before filing downstream.

---

## Phase 1 — Load shared Stage MCP bundle

Call **once** at Stage open:

```
mcp__territory-ia__lifecycle_stage_context({
  master_plan_path: "{ORCHESTRATOR_SPEC}",
  stage_id: "{STAGE_ID}"
})
```

Returns `{stage_header, task_spec_bodies, glossary_anchors, invariants, pair_contract_slice}`. Store as **shared context block** — reused across all Tasks in Stage.

If composite unavailable → fall back to [`domain-context-load`](../domain-context-load/SKILL.md) subskill:

- `keywords` = English tokens from Stage Objectives + Exit criteria (translate if non-English).
- `brownfield_flag = false` for Stages touching existing subsystems.
- `tooling_only_flag = true` for doc/IA-only Stages (no runtime C#).
- `context_label` = `"stage-file Stage {STAGE_ID}"`.

**Do NOT re-run per Task.** `cache_block` = Tier 2 per-Stage ephemeral bundle.

---

## Phase 2 — Read Stage block + cardinality + sizing gates

### 2.1 Read Stage block

Read `ORCHESTRATOR_SPEC`. Locate `### Stage {STAGE_ID}` (H3 canonical). Legacy H4 `#### Stage` accepted with warning `[stage-file] WARN legacy H4 Stage heading — re-author via /master-plan-extend to canonical H3`.

Extract: Objectives, Exit criteria, Task-table rows. Collect `_pending_` rows into `pending_tasks[]` in task-table order. Each row = `{task_key: "T{STAGE_ID}.{K}", name, intent, priority}`.

### 2.2 Extract orchestrator slug + H1 title

Read `ORCHESTRATOR_SPEC` H1 line. Pattern: `# {Title} — Master Plan ({SCOPE_LABEL})`. Extract `{Title}`. Strip trailing `— Master Plan` + scope tag. Store as `PLAN_TITLE`.

Derive `SLUG` from filename: `ia/projects/{slug}-master-plan.md` → `{slug}`. Used as `task_insert.slug` arg + manifest resolution fallback.

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

1. Collect **union** of all Depends-on ids across Stage-level deps + per-Task deps (dedupe).
2. Non-empty → call `mcp__territory-ia__backlog_list({ids: [union_ids]})` **once**. Verify each id appears (open or archived).
3. Unresolvable id → **HALT** + emit `{reason: "dep {ID} not found in backlog or archive", pending_tasks}`. No DB writes.
4. Store `verified_deps: Map<task_key, string[]>` — maps each Task to its verified subset.

**Hard rule:** exactly one `backlog_list` call per Stage. Zero `backlog_issue` calls.

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

For each Task in `pending_tasks[]` (task-table order):

### 5.1 Compose task_insert args

From Stage context + Task row + shared context block:

```yaml
prefix: "{ISSUE_PREFIX}"            # TECH / FEAT / BUG / ART / AUDIO (no dash)
title: "{task.intent truncated to ≤80 chars if needed}"
slug: "{SLUG}"                      # from Phase 2.2
stage_id: "{STAGE_ID}"
priority: "{task.priority or 'medium'}"
notes: "{1–3 sentence scope note from Stage context + task intent, caveman}"
depends_on: [{verified_deps[task.task_key]}]
related: [{sibling task issue ids in same Stage, non-self — empty until siblings reserved}]
raw_markdown: |
  {checklist_line}
  {2–4 sub-bullets: acceptance hint, depends hint, spec link placeholder}
body: ""                             # spec body stays in ia/projects/*.md until Step 9
status: "pending"                    # initial
type: "{issue_type_label}"          # free-form; omit if not useful
```

#### 5.1a `raw_markdown` composition

Must be byte-identical to what `materialize-backlog-from-db.mjs` emits for a live BACKLOG row. Shape:

```markdown
- [ ] **{ISSUE_ID} — {title}** — {notes-first-sentence}. _depends on {DEP_IDs or "—"}_
  - Acceptance — {1-line derived from intent}
  - Spec — [`ia/projects/{ISSUE_ID}.md`](ia/projects/{ISSUE_ID}.md)
```

`ISSUE_ID` is assigned by `task_insert` at DB tx time. We do **NOT** know it yet. Two-pass shape:

- **Pass A (insert):** omit `raw_markdown` (null). `task_insert` returns `{task_id}`.
- **Pass B (backfill):** compute `raw_markdown` using returned `task_id`; call `mcp__territory-ia__task_spec_section_write({ task_id, section: "raw_markdown", body: {rendered_md} })` — OR directly `UPDATE ia_tasks SET raw_markdown = $1 WHERE task_id = $2` via a dedicated MCP helper if exposed.

**Current status (Step 6):** `task_insert` accepts `raw_markdown` at insert time. Strategy: compute `ISSUE_ID` **before** insert via `mcp__territory-ia__reserve_backlog_ids({prefix, count: 1})` pre-peek — **NO**, that's the yaml path. DB-path approach: insert with `raw_markdown: null` first, then patch via a second MCP call. **Simplest for Step 6 smoke:** insert with `raw_markdown: null`; generator `materialize-backlog-from-db.mjs` emits a WARNING for null rows but BACKLOG.md still regens (row skipped).

**Decision for this skill:** Pass A + Pass B. After `task_insert` returns `task_id`, render `raw_markdown` string using `task_id`, then call MCP `task_spec_section_write` with `section: "raw_markdown"` if that section is supported, else fall back to direct DB update via a purpose-built helper. If neither is wired at Step 6 time → leave `raw_markdown` null; log deferred followup in Step 6 Findings.

### 5.2 Call task_insert

```
mcp__territory-ia__task_insert({
  prefix, title, slug, stage_id, priority, notes,
  depends_on, related, raw_markdown, body, status, type
})
```

Returns `{task_id: "TECH-NNN", ...}`. Store `ISSUE_ID` = `task_id`.

Errors:

- `dep_not_found` → MCP validated deps against DB; should not happen after Phase 3 (paranoia check). Escalate + halt.
- `sequence_gap` / `unique_violation` → retry once; else escalate.

**Idempotency:** if title+slug+stage_id triple already present → MCP returns existing `task_id` (per Step 4 contract). Reuse; skip to 5.3.

### 5.3 Append manifest entry

Read `ia/state/backlog-sections.json`. Locate section where `.header === TARGET_SECTION_HEADER`. Append to `.items[]`:

```json
{
  "type": "issue",
  "id": "{ISSUE_ID}",
  "checklist_line": "- [ ] **{ISSUE_ID} — {title}** — {notes-first-sentence}.",
  "trailing_blanks": 1
}
```

Write manifest back. Atomic per Task (buffer if needed; flush after entire loop to minimize disk churn).

### 5.4 Write spec stub

Bootstrap `ia/projects/{ISSUE_ID}.md` from [`ia/templates/project-spec-template.md`](../../templates/project-spec-template.md):

- Frontmatter: `parent_plan: "{ORCHESTRATOR_SPEC}"`, `task_key: "T{STAGE_ID}.{K}"`.
- `# {ISSUE_ID} — {title}`
- `> **Issue:** [{ISSUE_ID}](../../BACKLOG.md)`
- `> **Status:** Draft`
- `> **Created:** {YYYY-MM-DD}`
- `> **Last updated:** {YYYY-MM-DD}`
- `## 1. Summary` → `{notes}` (1–3 sentences).
- `## 2.1 Goals` → 2–4 bullets derived from Task intent.
- `## 4.2 Systems map` → file/class list from shared context `router_domains` + `spec_sections`.
- `## 7. Implementation Plan` → single Phase stub from Task intent.
- `## §Plan Digest` → `_pending — populated by /plan-digest_`
- §Audit / §Code Review / §Code Fix Plan → `_pending_` sentinels.

Write to `ia/projects/{ISSUE_ID}.md`. Overwrite-safe (idempotent).

### 5.5 Record for post-loop

Append `{task_key, ISSUE_ID, title}` to `filed_tasks[]`. Phase 6 uses for task-table flip.

---

## Phase 6 — Post-loop: materialize + validate + task-table + R1/R2 flips

Run after all Tasks processed.

### 6.1 Materialize BACKLOG.md

```bash
bash tools/scripts/materialize-backlog.sh
```

DB source is default (Step 5). Non-zero exit → escalate `{reason: "materialize-backlog.sh failed: {stderr}"}`. Re-run idempotent; safe.

### 6.2 Validate

```bash
npm run validate:dead-project-specs
```

Non-zero exit → escalate. `validate:backlog-yaml` **skipped** — no yaml written on DB path.

### 6.3 Update orchestrator task-table

Atomic Edit pass on `ORCHESTRATOR_SPEC`. For each `filed_tasks[]` entry:

- Replace `_pending_` in `Issue` column with `**{ISSUE_ID}**`.
- Replace `_pending_` in `Status` column with `Draft`.

All rows in one Edit (do NOT update row-by-row mid-loop).

### 6.4 R2 — Stage header Status flip

Find `^### Stage {STAGE_ID}\b` in orchestrator. Within 20 lines below, locate `**Status:**` line. Overwrite to:

```
**Status:** In Progress — {YYYY-MM-DD} ({N} tasks filed)
```

Regardless of prior token (`Draft`, `In Review`, dated variants). Idempotent if already `In Progress`. Post-flip self-check — re-grep + assert; fail → escalate `{reason: "stage_status_r2_flip_failed", stage_id: "{STAGE_ID}"}`.

Pre-flip B5 guard: if Status pre-edit matches NEITHER `Draft` NOR `In Review` NOR `In Progress` NOR `Final` → log `[stage-file] WARN stage {STAGE_ID} status non-canonical: "{raw_line}" — overwriting to In Progress`. Does not block.

### 6.5 R1 — Master-plan top Status flip

Read top-of-file `> **Status:**` line. If equals `Draft` (any variant) → rewrite:

```
> **Status:** In Progress — Stage {STAGE_ID}
```

If already `In Progress` → leave.

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
Validators: exit 0.
next=stage-file-chain-continue
```

**Current branch (`feature/ia-dev-db-refactor`) guardrail:** `docs/ia-dev-db-refactor-implementation.md §3` — "No §Plan Digest ceremony. Do not invoke /author, /plan-digest, /plan-review on this branch." Smoke runs halt here.

**Downstream chain (other branches / Step 7+):** dispatcher `.claude/commands/stage-file.md` continues to plan-author → plan-digest → plan-review → STOP. Final handoff:

- **N≥2:** `Next: claude-personal "/ship-stage {ORCHESTRATOR_SPEC} Stage {STAGE_ID}"`
- **N=1:** `Next: claude-personal "/ship {ISSUE_ID}"`

Hard rule: `/ship-stage` is multi-task only; N=1 uses `/ship`.

---

## Escalation rules

Single-skill NEVER guesses. Immediate halt triggers:

| Trigger | Halt shape |
|---------|-----------|
| Cardinality gate `pause` without user confirmation | Surface violations; wait for user. |
| Sizing gate FAIL | Halt before any DB write; route to `/stage-decompose`. |
| Dep id not in backlog_list result | Halt before any DB write; emit `{reason, unresolvable_ids, pending_tasks}`. |
| `task_insert` unique_violation (non-idempotent) | Retry once; else halt. |
| `task_insert` sequence_gap | Retry once; else halt. |
| Manifest section ambiguous after heuristic | Prompt user; wait. |
| `materialize-backlog.sh` non-zero | Halt post-loop; emit stderr. |
| `validate:dead-project-specs` non-zero | Halt post-loop; emit stderr. |
| R2 Stage Status self-check miss | Escalate; do not retry. |

---

## Idempotency

- `task_insert`: MCP-side unique-on-`(slug, stage_id, title)` triple → returns existing `task_id` on duplicate.
- Manifest append: if `{type:"issue", id:ISSUE_ID}` already present in target section → skip append.
- Spec stub: overwrite with desired final state — no-op if content matches.
- Task-table update: detect row already flipped (`Draft` in Status) → skip.
- Status flips: detect already `In Progress` → no-op.
- `materialize-backlog.sh`: idempotent by design (DB → manifest → BACKLOG.md).

Re-running fully-applied state = exit 0 + zero diff.

---

## Hard boundaries

- Do NOT write yaml under `ia/backlog/` — DB is source of truth.
- Do NOT call `reserve-id.sh` — per-prefix DB sequences own id assignment.
- Do NOT re-query `backlog_issue` per Task — Phase 3 batch-verified.
- Do NOT reorder Tasks — apply in task-table order.
- Do NOT update task-table mid-loop — atomic Edit after Phase 6.1+6.2 exit 0.
- Do NOT edit `BACKLOG.md` directly — `materialize-backlog.sh` regenerates from DB + manifest.
- Do NOT call `domain-context-load` per Task — Phase 1 once per Stage.
- Do NOT commit — user decides.

---

## Changelog

### 2026-04-24 — Step 6 merge: DB-backed single-skill (retire -plan/-apply pair)

**Status:** applied

**Symptom:** Pre-merge two-subagent pair (`stage-file-plan` Opus + `stage-file-apply` Sonnet) wrote yaml files + used `reserve-id.sh` + `materialize-backlog.sh` reading yaml. DB-primary refactor (Step 6 of `docs/ia-dev-db-refactor-implementation.md`) requires DB-only writes via `task_insert` MCP.

**Fix:** Collapsed pair into single skill per design doc B1. Filing phase uses `task_insert` MCP (DB-backed per-prefix sequence); no yaml written. Manifest append logic inlined (Phase 5.3) — derives target section from master-plan H1 slug heuristic (fallback user prompt). Spec stubs still written to `ia/projects/` (body in DB is Step 9 territory). Pre-refactor pair body archived at `ia/skills/_retired/stage-file-plan/SKILL.md` + `ia/skills/_retired/stage-file-apply/SKILL.md`.

**Rollout row:** ia-dev-db-refactor Step 6
