---
name: ship-plan
purpose: >-
  DB-backed bulk plan-authoring skill that replaces `stage-file` + `stage-authoring`. Reads lean
  handoff YAML frontmatter from `docs/explorations/{slug}.md`, pre-fetches glossary + router +
  invariants once, inlines anchor expansion at digest write, runs synchronous drift lint
  (anchor + glossary + retired-surface), and dispatches one atomic `master_plan_bundle_apply`
  Postgres tx — `ia_master_plans` + `ia_stages` + `ia_tasks` + `ia_task_specs` rows materialised
  in a single call.
audience: agent
loaded_by: "skill:ship-plan"
slices_via: none
description: >-
  Single-skill bulk plan author. Input = lean handoff YAML frontmatter at top of
  `docs/explorations/{slug}.md` (emitted by `design-explore` Phase 4). One Opus xhigh pass
  pre-fetches glossary + router + invariants once per plan, builds a 3-section §Plan Digest
  body per task (§Goal + §Red-Stage Proof + §Work Items, ~30 lines), inlines anchor expansion
  at digest write, runs synchronous drift lint, and dispatches one
  `master_plan_bundle_apply(jsonb)` MCP call. No filesystem mirror — DB sole source of truth.
  Replaces the `stage-file` + `stage-authoring` two-step roundtrip.
  Triggers: "/ship-plan {SLUG}", "ship plan", "bulk-author plan from handoff yaml".
phases:
  - Parse handoff YAML frontmatter
  - Validate handoff schema
  - Pre-fetch shared MCP context (glossary + router + invariants once per plan)
  - Pre-load task_bundle_batch context
  - Compose 3-section digest per task with inline anchor expansion
  - Drift lint (anchor + glossary + retired-surface) per task
  - Dispatch master_plan_bundle_apply (atomic Postgres tx)
  - Hand-off
triggers:
  - /ship-plan {SLUG}
  - ship plan
  - bulk-author plan from handoff yaml
argument_hint: "{slug} [--force-model {model}]"
model: opus
reasoning_effort: high
input_token_budget: 180000
pre_split_threshold: 160000
tools_role: pair-head
tools_extra:
  - mcp__territory-ia__router_for_task
  - mcp__territory-ia__glossary_discover
  - mcp__territory-ia__glossary_lookup
  - mcp__territory-ia__invariants_summary
  - mcp__territory-ia__spec_section
  - mcp__territory-ia__list_rules
  - mcp__territory-ia__rule_content
  - mcp__territory-ia__master_plan_state
  - mcp__territory-ia__master_plan_bundle_apply
  - mcp__territory-ia__task_bundle_batch
  - mcp__territory-ia__plan_digest_verify_paths
  - mcp__territory-ia__cron_journal_append_enqueue
  - mcp__territory-ia__cron_audit_log_enqueue
  - mcp__territory-ia__cron_glossary_backlinks_enqueue
  - mcp__territory-ia__cron_anchor_reindex_enqueue
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
  - handoff YAML frontmatter (verbatim)
hard_boundaries:
  - "Do NOT write `## §Plan Author` section — retired."
  - "Do NOT call `task_insert` / `stage_insert` / `master_plan_insert` / `task_spec_section_write` per row — single `master_plan_bundle_apply` only."
  - "Do NOT regress to per-Task authoring on token overflow — split into ⌈N/2⌉ bulk sub-passes; bundle still dispatches once after the last sub-pass."
  - "Do NOT skip drift lint — anchor + glossary + retired-surface lint runs synchronously before bundle dispatch. Findings go to in-memory buffer, NOT inline ctx preamble."
  - "Do NOT write drift_lint_summary row before master_plan_bundle_apply succeeds — version row FK required (Review Note 5). Use `cron_audit_log_enqueue` (audit_kind=drift_lint_summary)."
  - "Do NOT call `lifecycle_stage_context` per Task — pre-fetch once at Phase 3."
  - "Do NOT write code, run verify, or flip Task status — handoff to `/ship-cycle` (or legacy `/ship-stage`) handles execution."
  - "Do NOT edit `ia/specs/glossary.md` — propose candidates in handoff `notes:` field only."
  - "Do NOT touch `.claude/settings.json` `permissions.defaultMode` or `mcp__territory-ia__*` wildcard."
  - "Do NOT write task spec bodies to filesystem — bundle apply persists to DB only."
caller_agent: ship-plan
---

# Ship-plan skill — DB-backed bulk plan-authoring

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role.** Single-pass bulk plan-author skill that replaces the `stage-file` + `stage-authoring` chain. Reads lean handoff YAML, pre-fetches shared MCP context once, composes 3-section digests with inline anchor expansion, drift-lints, and dispatches one `master_plan_bundle_apply` Postgres tx.

**Contract.** [`ia/rules/plan-digest-contract.md`](../../rules/plan-digest-contract.md) §3-section relaxed shape. Rubric injected verbatim into authoring prompt — no post-author lint MCP call, no retry loop.

**Upstream.** `design-explore` Phase 4 emits `docs/explorations/{slug}.md` carrying the lean handoff YAML frontmatter + Design Expansion body. **Downstream.** `/ship-cycle` (Sonnet 4.6 low-effort iterative implement) for new plans; legacy `/ship-stage` for grandfathered plans.

---

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `SLUG` | 1st arg | Bare master-plan slug (e.g. `blip`). Must match `slug:` in handoff YAML. |
| `--force-model {model}` | optional | Override model (`sonnet` / `opus` / `haiku`). Default = frontmatter `model: opus`. |

Handoff YAML frontmatter shape (lean) at top of `docs/explorations/{slug}.md`:

```yaml
---
slug: {slug}
parent_plan_id: {prior-version-id-or-null}
target_version: {N}
stages:
  - id: 1.0
    title: "Tracer slice"
    exit: "..."
    red_stage_proof: |
      pseudo-code test...
    tasks:
      - id: 1.0.1
        title: "..."
        prefix: TECH
        depends_on: []
        digest_outline: "..."
        touched_paths: ["Assets/Scripts/X.cs"]
        kind: code | doc-only | mcp-only
---
```

Schema validated by `tools/scripts/validate-handoff-schema.mjs` (TECH-12634).

---

## Phase 1 — Parse handoff YAML frontmatter

Read `docs/explorations/{SLUG}.md`. Extract leading YAML block delimited by `---` lines. Parse with `yaml` module. Required top-level keys: `slug`, `target_version`, `stages[]`. Each stage requires `id`, `title`, `exit`, `red_stage_proof`, `tasks[]`. Each task requires `id`, `title`, `prefix`, `digest_outline`, `kind`. Optional: `parent_plan_slug`, `depends_on`, `touched_paths`.

Missing frontmatter → halt with `STOPPED — handoff_yaml_missing: docs/explorations/{SLUG}.md`. `slug` mismatch → halt with `STOPPED — slug_mismatch: arg={SLUG} yaml={yaml.slug}`.

### Phase 1 resume hook — phase_checkpoint read

After parsing YAML, derive `resolved_phases` set:

```
IF yaml.target_version > 1 AND yaml.parent_plan_slug IS NOT NULL:
    rows = journal_get(slug=SLUG, payload_kind="phase_checkpoint")
    resolved_phases = { row.payload.phase_id for row in rows }
ELSE:
    resolved_phases = {}   # New plan — skip read entirely
```

Later phases (6, 7) check `if "ship-plan.{N}.{label}" in resolved_phases: skip`. Resume reader is read-only; no write at this point.

Prior drift summary read (when `parent_plan_slug` non-null):

```sql
SELECT id, payload FROM ia_master_plan_change_log
WHERE slug = $1 AND kind = 'drift_lint_summary'
ORDER BY recorded_at DESC LIMIT 1
```

If row exists → Phase 6 may skip already-resolved drift items (use `payload.n_unresolved` as gate: 0 → drift clean from prior run).

---

## Phase 2 — Validate handoff schema

Invoke `node tools/scripts/validate-handoff-schema.mjs docs/explorations/{SLUG}.md`. Non-zero exit → halt with `STOPPED — handoff_schema_invalid: {stderr}`. Schema validator owns: required-key check + cardinality (≤6 stages, ≤5 tasks/stage for new plans), prefix enum (`TECH` / `FEAT` / `BUG` / `ART` / `AUDIO`), kind enum (`code` / `doc-only` / `mcp-only`), id-format (`{stage_id}.{task_idx}` or `{major}.{minor}.{task_idx}` for sub-stages).

Existing-plan grandfather clause — `parent_plan_id` non-null skips cardinality cap.

---

## Phase 3 — Pre-fetch shared MCP context (once per plan)

Single batched read — reused across all N tasks:

1. `mcp__territory-ia__router_for_task({ task_keywords: yaml.stages.flatMap(s => s.tasks.map(t => t.title)).join(" ") })` → router hits.
2. `mcp__territory-ia__glossary_discover({ query: ... })` → glossary anchors.
3. `mcp__territory-ia__invariants_summary({})` → universal + Unity invariants merged.
4. `mcp__territory-ia__list_rules({})` → rule-id index for retired-surface scan.

Cache as `SHARED_CONTEXT` Tier 2 ephemeral block. **Do NOT re-fetch per Task.**

---

## Phase 4 — Pre-load task_bundle_batch context

Call `mcp__territory-ia__task_bundle_batch({ slug: "{SLUG}", task_keys: yaml.stages.flatMap(s => s.tasks.map(t => `${t.prefix}-${t.id}`)) })` — single MCP roundtrip pre-loads all task contexts (depends_on resolution + cited-id status maps + commit history shells). Cache as `TASK_BATCH` block.

`task_bundle_batch` is the new MCP tool registered by TECH-12635. Pre-creation of plan rows happens via `master_plan_bundle_apply` at Phase 7 — `task_bundle_batch` here primes the cache for downstream `/ship-cycle` consumption.

---

## Phase 5 — Compose 3-section digest per task with inline anchor expansion

Per task, build `§Plan Digest` body with exactly 3 sub-sections by reading the file-based Markdown templates (TECH-15901) under `ia/templates/digest-sections/` and filling `{{slot}}` values:

- `ia/templates/digest-sections/goal.md` — slots: `{{intent_one_liner}}`, `{{primary_surface}}`, `{{glossary_terms}}`
- `ia/templates/digest-sections/red-stage-proof.md` — slots: `{{anchor_kind}}`, `{{path}}`, `{{method}}`, `{{description}}`, `{{failing_baseline}}`, `{{green_criteria}}`
- `ia/templates/digest-sections/work-items.md` — slot: `{{work_item_lines}}` (bullet list)

Concatenate the three filled templates under a `## §Plan Digest` heading. Structure is identical across FEAT/BUG/TECH tasks — only slot values differ.

**Drop sections** (vs legacy 7-section relaxed shape): §Acceptance, §Pending Decisions, §Implementer Latitude, §Test Blueprint, §Invariants & Gate. §Acceptance subsumed by §Red-Stage Proof. §Invariants & Gate moved to stage exit criteria. §Pending Decisions resolved upstream at design-explore Phase 1 (zero unresolved decisions reach ship-plan — hard rule).

### 5.1 Inline anchor expansion at digest write

For every `{anchor-kind}:{path}::{method}` ref in §Red-Stage Proof or every glossary slug in §Goal, call `mcp__territory-ia__spec_section` (router hits cached at Phase 3) inline and embed the resolved body directly in the digest. No deferred `@anchor` placeholders — implementer reads the digest and the cited surface in one read.

### 5.2 Token-split guardrail

Total input tokens (handoff YAML + SHARED_CONTEXT + TASK_BATCH) > 180k → split into ⌈N/2⌉ bulk sub-passes per stage. Each sub-pass replays SHARED_CONTEXT + per-stage handoff slice. Sub-passes append to a single `bundle.tasks[]` array — **bundle dispatch (Phase 7) still runs once** after the last sub-pass.

---

## Phase 6 — Drift lint (synchronous; per task before bundle dispatch)

**Ctx-discipline: in-memory buffer, not inline preamble.** Collect ALL drift findings into an in-memory
`drift_findings` buffer object (do NOT append verbose findings to the author-prompt context). Buffer shape
mirrors `drift_lint_summary` payload schema (`ia/rules/ship-stage-journal-schema.md §drift_lint_summary`).
Phase 7 dispatches `master_plan_bundle_apply` FIRST, then enqueues `cron_audit_log_enqueue` (audit_kind=drift_lint_summary) row from
the buffer. Author prompt at Phase 7 receives only: `drift_lint_summary_id={row_id} ({n_resolved} resolved,
{n_unresolved} unresolved)` — the 1-line ref, not the full findings.

**Resume read (parent_plan_slug non-null):** Phase 1 resume hook already queried prior `drift_lint_summary`
row. If prior row has `n_unresolved=0` → drift clean from prior run; Phase 6 may skip re-lint of previously
passing tasks. Flag each task: `"skip_drift_lint": true` when prior `decisions_resolved` includes the task
anchor.

Per composed §Plan Digest body, run three sub-lints:

After completing all three sub-lints, write a phase checkpoint:

```json
{
  "session_id": "{SESSION_ID}",
  "slug": "{SLUG}",
  "stage_id": "{first stage id}",
  "phase": "ship-plan.6.drift_lint",
  "payload_kind": "phase_checkpoint",
  "payload": {
    "phase_id": "ship-plan.6.drift_lint",
    "decisions_resolved": ["drift_lint:anchor_pass", "drift_lint:glossary_pass", "drift_lint:retired_surface_pass"],
    "pending_decisions": [],
    "next_phase": "ship-plan.7.bundle_apply",
    "ctx_drop_hint": ["drift_lint_inline_log"]
  }
}
```

Note: drift findings buffer is held **in-memory** at this point. Do NOT write `drift_lint_summary` row here — write AFTER `master_plan_bundle_apply` succeeds at Phase 7 (Review Note 5 — version row must exist first).

### 6.1 Anchor lint

Every `{anchor-kind}:{path}::{method}` ref in §Red-Stage Proof MUST resolve. Resolution = `path` exists on HEAD AND `method` is a valid identifier on that path's symbol table. Failure → re-author that task's §Red-Stage Proof with a corrected anchor; halt the plan only after 2 retries fail (`STOPPED — anchor_unresolved: {ref}`).

Anchor grammar: `{anchor-kind}:{path}::{method}` where `{anchor-kind}` ∈ {`tracer-test`, `visibility-delta-test`, `bug-repro-test`, `unit-test`}.

### 6.2 Glossary alignment

Every domain-style noun phrase in §Goal MUST match `ia/specs/glossary.md` spelling. Mismatch → replace inline with canonical spelling. Term not in glossary → leave as-is + emit `glossary_warning: {term}` in hand-off (do NOT edit glossary; do NOT add §Open Questions row from this skill).

### 6.3 Retired-surface scan

Hard-coded list of retired surface names that MUST NOT appear in any §Plan Digest body:

| Retired | Live successor |
|---------|---------------|
| `/enrich` / `spec-enrich` | folded into ship-plan |
| `/kickoff` / `spec-kickoff` | folded into ship-plan |
| `/author {id}` / `plan-author` | folded into ship-plan |
| `/plan-digest` / `plan-digest` | folded into ship-plan |
| `/stage-file` / `stage-file` | folded into ship-plan |
| `/stage-authoring` / `stage-authoring` | folded into ship-plan |
| `project-spec-close` / `project-stage-close` | folded into ship-cycle Pass B |
| `docs/implementation/{slug}-stage-X.Y-plan.md` ref | drop entirely |
| `§Mechanical Steps` (heading) | `§Work Items` |

Match → replace inline with the live successor. Counter: `n_retired_refs_replaced`.

Retired-surface rule list also consulted via `mcp__territory-ia__rule_content({ rule_id: "retired-surfaces" })` for cross-skill consistency.

---

## Phase 7 — Dispatch master_plan_bundle_apply (atomic Postgres tx)

Build the bundle jsonb shape:

```json
{
  "plan": {
    "slug": "{slug}",
    "title": "{plan_title}",
    "parent_plan_id": {parent_plan_id_or_null},
    "version": {target_version}
  },
  "stages": [
    { "stage_id": "{id}", "title": "{title}", "exit_criteria": "{exit}", "red_stage_proof": "{red_stage_proof}", ... }
  ],
  "tasks": [
    { "task_key": "{prefix}-{id}", "stage_id": "{stage_id}", "prefix": "{prefix}", "title": "{title}", "depends_on": [...], "kind": "{kind}", "touched_paths": [...], "body": "{composed §Plan Digest body}" }
  ]
}
```

Single call: `mcp__territory-ia__master_plan_bundle_apply({ bundle })`. Returns `{plan_slug, stages_inserted, tasks_inserted}`. Any constraint failure rolls back the whole tx — re-author offending field then re-dispatch.

After `master_plan_bundle_apply` succeeds, write a phase checkpoint AND the drift lint summary in order:

1. `cron_journal_append_enqueue` with `payload_kind=phase_checkpoint` (cron drains async to `ia_ship_stage_journal`):

```json
{
  "session_id": "{SESSION_ID}",
  "slug": "{SLUG}",
  "phase": "ship-plan.7.bundle_apply",
  "payload_kind": "phase_checkpoint",
  "payload": {
    "phase_id": "ship-plan.7.bundle_apply",
    "decisions_resolved": ["bundle_apply:ok", "plan:{SLUG}:v{VERSION}:inserted"],
    "pending_decisions": [],
    "next_phase": "ship-plan.8.handoff",
    "ctx_drop_hint": ["bundle_jsonb", "task_digest_bodies"]
  }
}
```

2. `cron_audit_log_enqueue` with `audit_kind=drift_lint_summary` (body = in-memory drift findings buffer from Phase 6, JSON-stringified). Cron drains to `ia_master_plan_change_log`. Returns `job_id`. Ref `drift_lint_summary_job_id={job_id}` stored for Phase 8 summary line.

Payload schema: `ia/rules/ship-stage-journal-schema.md §drift_lint_summary`.

`task_key` is allocated by the Postgres function (per-prefix monotonic id from `ia_id_sequences`). The composed §Plan Digest is sent in the `body` field per task and persisted to `ia_tasks.body` — DB sole source of truth (read back via `task_spec_body` MCP).

---

## Phase 7.5 — Post-bundle glossary back-link enrich (TECH-15903)

After `master_plan_bundle_apply` succeeds, enqueue the back-link enricher (async — fire-and-forget):

```
mcp__territory-ia__cron_glossary_backlinks_enqueue({ slug: {slug}, plan_id: {plan_uuid} })
```

Cron supervisor drains the job by shelling to `node tools/scripts/glossary-backlink-enrich.mjs --plan-id {slug}` (cadence `*/5 * * * *`; drains within 5 min). Upserts `ia_glossary_backlinks` rows keyed `(plan_id, term, section_id)`. Cache-backed via `ia_mcp_context_cache` (TECH-15902). Enqueue returns < 100 ms. Non-blocking: enqueue failure emits a warning log but does not halt the plan.

Also enqueue an anchor reindex to sync `ia_spec_anchors` after the new task spec bodies land:

```
mcp__territory-ia__cron_anchor_reindex_enqueue({ paths: ["ia/specs/glossary.md"] })
```

Cron supervisor drains by running `npm run generate:ia-indexes -- --write-anchors` (cadence `*/5 * * * *`; drains within 5 min). Non-blocking: enqueue failure emits a warning log but does not halt the plan.

---

## Phase 8 — Hand-off

Caveman summary block:

```
ship-plan done. SLUG={slug} VERSION={target_version} PLAN_INSERTED=true STAGES={stages_inserted} TASKS={tasks_inserted} (split: {sub_pass_count} sub-pass(es))
Per-stage:
  Stage 1.0: red_stage_proof anchor={resolved_anchor} target_kind={tracer_verb|visibility_delta|bug_repro|design_only}
  ...
Per-task:
  {task_key}: §Plan Digest written ({n_work_items} work items); fold: {n_term_replacements}/{n_retired_refs_replaced}; glossary_warnings: {n_glossary_warnings}
  ...
drift_lint_summary_job_id: {job_id} ({n_resolved} resolved, {n_unresolved} unresolved)
DB writes: 1 master_plan_bundle_apply OK; 1 cron_audit_log_enqueue (drift_lint_summary, drained async); 0 task_spec_section_write (replaced by bundle).
next=ship-cycle Stage 1.0
```

Then dispatcher emits next-step handoff:

- **New plan (parent_plan_id null):** `Next: claude-personal "/ship-cycle {SLUG} Stage 1.0"`
- **Versioned plan (parent_plan_id non-null):** `Next: claude-personal "/ship-cycle {SLUG} Stage {first_stage_id}"` (resume gate picks first non-done stage)

---

## Escalation rules

Structured halt shape:

```json
{ "escalation": true, "phase": N, "reason": "...", "task_key?": "...", "failing_field?": "...", "stderr?": "..." }
```

Triggers:

- Phase 1: `handoff_yaml_missing` / `handoff_yaml_invalid_yaml` / `slug_mismatch`.
- Phase 2: `handoff_schema_invalid`.
- Phase 3-4: `mcp_unavailable` (do NOT fall back to filesystem).
- Phase 5: `digest_compose_failed` after 2 retries.
- Phase 6: `anchor_unresolved` after 2 retries OR `retired_surface_persistent` after 2 retries.
- Phase 7: `bundle_apply_constraint_violation` after 1 retry (re-author offending field then re-dispatch once; second failure escalates).

DB unavailable at Phase 7 → escalate. Do NOT write task spec bodies to filesystem.

---

## Guardrails

### DB read batching guardrail

Before issuing the first DB read, list every question needed for this phase. Batch into one `db_read_batch` MCP call OR one typed MCP slice (`catalog_panel_get`, `catalog_archetype_get`, `master_plan_state`, `task_bundle_batch`, `spec_section`). Sequential reads only when query N depends on result of N-1.

---

## Changelog

(empty — populated on first ship-plan run + by future skill-train passes)
