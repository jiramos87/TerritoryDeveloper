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
  - Phase A — recipe ship-plan-phase-a (parse + schema + invariants + task_bundle + spec_sections + drift-lint stash)
  - Phase B — compose 3-section digest per task with inline anchor expansion (Opus only)
  - Phase B.5 — split-on-token-overflow guardrail (single bundle dispatch after last sub-pass)
  - Phase C — dispatch master_plan_bundle_apply (server-side body render via mig 0136; drift-lint stash promoted atomically)
  - Phase D — recipe ship-plan-phase-c (glossary backlinks + anchor reindex + plan_filed audit)
  - Phase E — hand-off
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
  - "Phase B enriched-subsection injection MUST be verbatim — no paraphrase, no compression. When source MD subsection absent → emit skip-clause body line, NEVER drop the `### §...` heading."
  - "Phase A.0 — when `docs/explorations/{slug}.html` exists, refresh `.md` sidecar via extract-md before recipe runs. HTML is canonical artifact post-uplift."
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
    # Optional — fills the 4-field §Red-Stage Proof validator block in the
    # stage body. Omit for mechanical / pass-through stages → Phase 7 seeds
    # the skip-clause (target_kind=design_only, proof_status=not_applicable).
    red_stage_proof_block:
      red_test_anchor: tracer-verb-test:Assets/Scripts/X.cs::TestName
      target_kind: tracer_verb           # tracer_verb | visibility_delta | bug_repro | unit | design_only
      proof_artifact_id: tools/scripts/test/x-spec.test.ts
      proof_status: failed_as_expected   # failed_as_expected | green | not_applicable
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

## Phase A.0 — Source resolution (HTML-first)

Resolve the canonical MD input for the recipe. Two cases:

**Case 1 — `docs/explorations/{slug}.html` exists** (post-uplift exploration). Extract canonical MD from the HTML's embedded `<script id="rawMarkdown">` block and refresh the on-disk `.md` sidecar in place:

```bash
npm run design-explore:extract-md {SLUG} > docs/explorations/{slug}.md
```

This guarantees the recipe reads the byte-equivalent of the canonical embed inside HTML — sidesteps drift if user hand-edited the `.md` post-render. Recipe step 1 (`parse_handoff_yaml`) then reads the refreshed `.md` as normal.

**Case 2 — only `docs/explorations/{slug}.md` exists** (legacy exploration, pre-uplift). Skip extraction; recipe reads `.md` directly.

**Stop conditions:**

- Neither `.html` nor `.md` present → `STOPPED — exploration_source_missing: docs/explorations/{slug}.(html|md)`.
- Round-trip extract returns malformed YAML frontmatter → `STOPPED — html_raw_md_block_corrupt`; fall back to direct `.md` read + warn.

Emit working-memory token: `source ∈ {html-extracted, md-legacy, drift-detected}`. Downstream Phase B uses this to decide whether to expect enriched MD subsections (`html-extracted` / `drift-detected` → yes; `md-legacy` → emit skip-clause inside each enriched digest subsection).

## Phase A — recipe `ship-plan-phase-a` (mechanical preflight)

Single recipe call replaces legacy Phases 1-4 + 6 (parse + schema + MCP prefetch + task_bundle + spec_sections batch + drift-lint stash):

```bash
npm run recipe:run -- ship-plan-phase-a --input slug={SLUG}
```

Recipe steps (`tools/recipes/ship-plan-phase-a.yaml`):

1. `parse_handoff_yaml` — bash; reads `docs/explorations/{SLUG}.md` frontmatter; emits parsed JSON.
2. `validate_handoff` — bash; wraps `tools/scripts/validate-handoff-schema.mjs` (TECH-12634). Non-zero exit → STOP.
3. `invariants_summary` + `list_rules` — parallel MCP fetches; merged universal + Unity invariants + rule-id index.
4. `task_bundle_batch` — MCP; primes depends_on + cited-id status maps for all `{prefix}-{id}` keys.
5. `spec_sections` — MCP; batch anchor expansion for ALL anchors across all tasks (replaces sequential per-anchor `spec_section`).
6. `drift_lint_pre_bundle` — MCP `plan_digest_drift_lint`; SQL+regex anchor + glossary + retired-surface scan.
7. `enqueue_drift_findings_staged` — MCP `cron_drift_lint_findings_enqueue(status='staged')`. Stash awaits Phase C bundle_apply success → `promote_drift_lint_staged()` flips staged→queued atomically inside the same tx.

Recipe outputs: `{handoff_json, invariants, rule_index, task_batch, anchor_bundle, drift_findings_job_id}`. Consumed by Phase B digest composition.

Resume hook — when `target_version > 1 AND parent_plan_slug IS NOT NULL`: recipe step `parse_handoff_yaml` reads `ia_ship_stage_journal` for `payload_kind=phase_checkpoint` rows; later phases skip already-resolved.

Stop conditions (recipe exits non-zero):
- `handoff_yaml_missing` / `handoff_yaml_invalid_yaml` / `slug_mismatch` → Phase 1 step.
- `handoff_schema_invalid` → Phase 2 step.
- `mcp_unavailable` → Phase 3-5 steps (do NOT fall back to filesystem).

### Phase A.1 — Blueprint loader (task_kind branch)

After recipe returns `task_batch`, scan each task in `handoff_json.stages[].tasks[]` for `task_kind` field:

**Branch A — `task_kind: ui_from_db`:**

1. Read `ia/templates/blueprints/ui-from-db.md` (canonical blueprint).
2. Assert 5 deterministic H2 section ids in order: `Schema-Probe` / `Bake-Apply` / `Render-Check` / `Console-Sweep` / `Tracer`. Missing or reordered → halt with `STOPPED — blueprint_section_ids_invalid: ia/templates/blueprints/ui-from-db.md`.
3. Check `bake_handler_version:` stamp. If differs from current `UiBakeHandler` schema_version → emit warning `blueprint_version_drift: blueprint={N} handler={M}` (not a halt).
4. At Phase B digest composition: replace standard 3-section §Plan Digest template with 5-section blueprint expansion. Per blueprint section, fill `{{slots}}` from task `digest_outline` + stage `exit`.
5. Cache loaded + slot-filled blueprint as `BLUEPRINT_UI_FROM_DB` for reuse across multiple `ui_from_db` tasks.

**Branch B — all other `task_kind` values (default: `implementation`, `refactor`, `docs`, `tooling`, absent):**

No change — Phase B proceeds with standard 3-section digest template.

---

## Phase B — Compose 3-section digest + 8 enriched subsections per task (Opus-owned)

Per task, build `§Plan Digest` body with the legacy 3 sub-sections + 8 enriched sub-sections (when source MD carries them per [`ia/rules/design-explore-output-schema.md`](../../rules/design-explore-output-schema.md)). Total = 11 sub-sections per task on post-uplift explorations; 3 + N skipped sub-sections on legacy `md-legacy` source.

**Legacy 3 sub-sections** — file-based Markdown templates (TECH-15901) under `ia/templates/digest-sections/`:

- `goal.md` — slots: `{{intent_one_liner}}`, `{{primary_surface}}`, `{{glossary_terms}}`
- `red-stage-proof.md` — slots: `{{anchor_kind}}`, `{{path}}`, `{{method}}`, `{{description}}`, `{{failing_baseline}}`, `{{green_criteria}}`
- `work-items.md` — slot: `{{work_item_lines}}` (bullet list)

Concatenate three filled templates under `## §Plan Digest` heading. Structure identical across FEAT/BUG/TECH tasks — only slot values differ.

**Enriched 8 sub-sections** — read verbatim from the per-task `#### Task {ID} — Enriched` block in the exploration MD body (or the per-stage `#### Stage {ID} — Enriched` block for stage-level fields). Inject in fixed order as `### §...` headings after the legacy 3:

| Section heading | Source subsection (under task / stage MD block) | Source field name (in YAML `enriched:`) | Per-X |
|---|---|---|---|
| `### §Visual Mockup` | `##### Visual Mockup` | `visual_mockup_svg` | task |
| `### §Before / After` | `##### Before / After Code` | `before_after_code` | task |
| `### §Edge Cases` | `##### Edge Cases` (stage block) | `edge_cases[]` (stage) | stage → repeats across all tasks |
| `### §Glossary Anchors` | `##### Glossary Anchors` | `glossary_anchors[]` | task |
| `### §Failure Modes` | `##### Failure Modes` | `failure_modes[]` | task |
| `### §Decision Dependencies` | `##### Decision Dependencies` | `decision_dependencies` | task |
| `### §Shared Seams` | `##### Shared Seams` (stage block) | `shared_seams[]` (stage) | stage → repeats across all tasks |
| `### §Touched Paths Preview` | `##### Touched Paths Preview` | `touched_paths_with_preview` | task |

**Injection rule — VERBATIM, no paraphrase.** Read the source MD subsection body byte-for-byte and inject under the `### §...` heading. No re-formatting, no token compression, no "summarize as N bullets". This is the load-bearing contract — spec-implementer + verify-loop downstream rely on the canonical content shape.

**Skip-clause.** When source MD subsection is absent (legacy `md-legacy` source, or `optional` mandatory-band field not authored on a Stage 2+ task) → emit the section heading + body line:

```markdown
### §Edge Cases

_skipped — source absent (legacy doc shape; pre-uplift exploration)._
```

Do NOT fail the task. Legacy plans ship with reduced enrichment. Output token `n_enriched_subsections_skipped` per task in Phase E handoff summary.

**Anchor inline expansion at digest write.** For every `{anchor-kind}:{path}::{method}` ref in §Red-Stage Proof or every glossary slug in §Goal, embed the resolved body from `anchor_bundle` (returned by Phase A recipe step 5) directly in the digest. No deferred `@anchor` placeholders.

**Drop sections** (vs legacy 7-section shape): §Acceptance / §Pending Decisions / §Implementer Latitude / §Test Blueprint / §Invariants & Gate. §Acceptance subsumed by §Red-Stage Proof + §Edge Cases + §Failure Modes (when enriched present). §Invariants & Gate moved to stage exit criteria. §Pending Decisions resolved upstream at design-explore Phase 1.

### Phase B.5 — Token-split guardrail

Total input tokens (handoff YAML + Phase A outputs) > 180k → split into ⌈N/2⌉ bulk sub-passes per stage. Each sub-pass replays Phase A outputs + per-stage handoff slice. Sub-passes append to a single `bundle.tasks[]` array — **bundle dispatch (Phase C) still runs once** after the last sub-pass.

Drift-lint runs synchronously inside Phase A recipe step 6 (`plan_digest_drift_lint` MCP); findings staged at status=`staged` via step 7. Phase C `master_plan_bundle_apply` flips staged→queued atomically; cron drainer writes the change-log row. No agent-owned drift-lint loop.

---

## Phase C — Dispatch master_plan_bundle_apply (atomic Postgres tx)

### C.1 Build bundle jsonb (server-side body render)

Mig 0136 extended `master_plan_bundle_apply` to render stage body server-side from `red_stage_proof_block`. Agent passes the 4-field jsonb verbatim from handoff YAML; SQL fn formats the markdown body. When `red_stage_proof_block` is absent, SQL fn seeds the skip-clause default (`target_kind=design_only` + `proof_status=not_applicable`).

Bundle jsonb shape:

```json
{
  "plan": {
    "slug": "{slug}",
    "title": "{plan_title}",
    "parent_plan_id": {parent_plan_id_or_null},
    "version": {target_version}
  },
  "stages": [
    {
      "stage_id": "{id}",
      "title": "{title}",
      "exit_criteria": "{exit}",
      "red_stage_proof_block": { "red_test_anchor": "...", "target_kind": "...", "proof_artifact_id": "...", "proof_status": "..." }
    }
  ],
  "tasks": [
    { "task_key": "{prefix}-{id}", "stage_id": "{stage_id}", "prefix": "{prefix}", "title": "{title}", "depends_on": [...], "kind": "{kind}", "touched_paths": [...], "body": "{composed §Plan Digest body}" }
  ]
}
```

Single call: `mcp__territory-ia__master_plan_bundle_apply({ bundle })`. Tx body:

1. Insert plan + stages + tasks rows.
2. Render stage body from `red_stage_proof_block` (mig 0136 server-side fn).
3. Call `promote_drift_lint_staged(plan_slug, version)` — flips `cron_drift_lint_findings_jobs` row from `staged` → `queued`. Drainer then writes to `ia_master_plan_change_log`.

Whole tx commits atomically. Constraint failure → rollback; re-author offending field then re-dispatch once. Returns `{plan_slug, stages_inserted, tasks_inserted, drift_lint_promoted: true}`.

`task_key` allocated by Postgres fn (per-prefix monotonic id from `ia_id_sequences`).

---

## Phase D — recipe `ship-plan-phase-c` (post-bundle async fan-out)

Single recipe call replaces legacy Phase 7.5 inline enqueues:

```bash
npm run recipe:run -- ship-plan-phase-c --input slug={SLUG} version={VERSION}
```

Recipe steps (`tools/recipes/ship-plan-phase-c.yaml`):

1. `cron_glossary_backlinks_enqueue` — MCP; drainer shells `glossary-backlink-enrich.mjs`.
2. `cron_anchor_reindex_enqueue` — MCP; drainer shells `generate:ia-indexes --write-anchors`.
3. `cron_audit_log_enqueue(audit_kind=plan_filed)` — MCP; provenance row.

All three enqueues non-blocking; drainer cadence `*/5 * * * *`. Recipe returns `{glossary_job_id, anchor_job_id, audit_job_id}`.

---

## Phase E — Hand-off

Caveman summary block:

```
ship-plan done. SLUG={slug} VERSION={target_version} PLAN_INSERTED=true STAGES={stages_inserted} TASKS={tasks_inserted} (split: {sub_pass_count} sub-pass(es))
Per-stage:
  Stage 1.0: red_stage_proof anchor={resolved_anchor} target_kind={tracer_verb|visibility_delta|bug_repro|design_only}
  ...
Per-task:
  {task_key}: §Plan Digest written ({n_work_items} work items); fold: {n_term_replacements}/{n_retired_refs_replaced}; glossary_warnings: {n_glossary_warnings}
  ...
drift_lint_findings_job_id: {job_id} (status=queued, n_resolved={n_resolved}, n_unresolved={n_unresolved})
DB writes: 1 master_plan_bundle_apply OK (incl. promote_drift_lint_staged); 3 Phase D async enqueues (glossary backlinks + anchor reindex + plan_filed audit).
next=ship-cycle Stage 1.0
```

Then dispatcher emits next-step handoff:

- **New plan (parent_plan_id null):** `Next: claude-personal "/ship-cycle {SLUG} Stage 1.0"`
- **Versioned plan (parent_plan_id non-null):** `Next: claude-personal "/ship-cycle {SLUG} Stage {first_stage_id}"` (resume gate picks first non-done stage)

---

## Escalation rules

Structured halt shape:

```json
{ "escalation": true, "phase": "A|B|C|D", "reason": "...", "task_key?": "...", "failing_field?": "...", "stderr?": "..." }
```

Triggers:

- Phase A: `handoff_yaml_missing` / `handoff_yaml_invalid_yaml` / `slug_mismatch` / `handoff_schema_invalid` / `mcp_unavailable` (recipe step exits non-zero).
- Phase B: `digest_compose_failed` after 2 retries / `anchor_resolution_failed` (Phase A `anchor_bundle` missing required ref).
- Phase C: `bundle_apply_constraint_violation` after 1 retry (re-author offending field then re-dispatch once; second failure escalates). DB unavailable → escalate.
- Phase D: enqueue failure → warning log, NOT a halt (non-blocking fan-out).

Do NOT write task spec bodies to filesystem on any escalation.

---

## Guardrails

### DB read batching guardrail

Before issuing the first DB read, list every question needed for this phase. Batch into one `db_read_batch` MCP call OR one typed MCP slice (`catalog_panel_get`, `catalog_archetype_get`, `master_plan_state`, `task_bundle_batch`, `spec_section`). Sequential reads only when query N depends on result of N-1.

---

## Changelog

- 2026-05-08 — fix: stage body emit drift (`/ship-final` Phase 4 plan-red-stage halt). Phase 7.0 added — composes stage `body` with 4-field §Red-Stage Proof block; defaults skip-clause (`target_kind=design_only`, `proof_status=not_applicable`) when handoff yaml omits `stages[].red_stage_proof_block`. Companion mig 0113 extends `master_plan_bundle_apply` SQL fn to accept `stages[].body`. Source: bug-log (large-file-atomization-cutover-refactor v1; 6 violations halted /ship-final).
- 2026-05-09 — lesson: `cityscene-mainmenu-panel-rollout 2.0` Wave A0 → bake → runtime gap. Stage 1.0 task specs registered `UiActionRegistry` shell + `MainMenuRegistrySeed` canonical action ids but never gated **action-wire conformance** in §Red-Stage Proof — bake handler had no `UiActionTrigger` attach helper, controller-side action ids drifted from `panels.json` canonical (silent dispatch miss, compile-clean). For UI-bake stages, §Red-Stage Proof must include a runtime click-fires-handler assertion (Path A bridge or Play-Mode test), NOT just DB row + bake screenshot. Add handoff-yaml convention `stages[].action_wire_proof: required|skip` for any stage that emits `params_json.action`. Drift gates to mention in §Work Items: (a) `panels.json` action id ≡ controller register id (one source of truth = panels.json); (b) every button-kind switch case in `UiBakeHandler` includes `AttachUiActionTrigger` (validator stub `validate:bake-handler-action-coverage`). Pending stages of any UI-bake plan that ship visible buttons → flag the same gates upstream so handoff yaml carries them.
- 2026-05-13 — feat: HTML-first uplift (design-explore-html-effectiveness-uplift plan). Phase A.0 added — source resolution: extracts canonical MD from `docs/explorations/{slug}.html` (when present) via `npm run design-explore:extract-md {SLUG}`, refreshing the on-disk `.md` sidecar before recipe Phase A reads it. Legacy `.md`-only explorations bypass extraction. Phase B prompt extended — composes 11 sub-sections per task (legacy 3 + 8 enriched per [`ia/rules/design-explore-output-schema.md`](../../rules/design-explore-output-schema.md)). Skip-clause: enriched subsection absent → `_skipped — source absent_` body; section heading always emitted. Hard boundary: Phase B injection MUST be verbatim (no paraphrase / compression / re-formatting); spec-implementer + verify-loop downstream rely on canonical content shape.
- 2026-05-10 — refactor: lifecycle skills mechanical-work move-out (Phase 5 of cheeky-growing-panda plan). Phases 1-4 + 6 collapsed to recipe `ship-plan-phase-a` (parse + schema + invariants + task_bundle + spec_sections batch + drift-lint stash). Phase 7.0 stage body composition dropped — mig 0136 server-side render owns it (agent passes `red_stage_proof_block` jsonb verbatim). Phase 7.5 collapsed to recipe `ship-plan-phase-c` (glossary backlinks + anchor reindex + plan_filed audit). Drift-lint findings buffer replaced with crash-safe `cron_drift_lint_findings_jobs` two-phase commit (staged → queued via `promote_drift_lint_staged()` SQL fn called atomically inside `master_plan_bundle_apply` tx). Agent prompt body shrinks ~60 % (parsing + lint + body composition removed). Companion migs 0132 (stage_id canonical) + 0135 (drift-lint queue) + 0136 (server-side body render).
