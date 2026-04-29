---
purpose: "DB-backed task/stage/master-plan lifecycle subsystem — extensions + improvements surfaced from arch-coherence + DB-primary pivot + cross-plan audit (last 2 days)."
audience: design-explore
created: 2026-04-28
status: pending-polling
size_target: tbd (likely 3–5 stages)
---

# DB Lifecycle Extensions — Exploration

## Problem

DB-backed lifecycle subsystem (`ia_master_plans` / `ia_stages` / `ia_tasks` + `ia_master_plan_change_log` + `arch_*` link tables) shipped via lifecycle-refactor + DB-primary pivot (`4ffe8aee`) + arch-coherence Stage 1.1–1.4. Surface now stable; gaps appearing as plans accrete.

Recent friction signal (commits since 2026-04-26 + cross-plan audit `docs/asset-pipeline-stage-13-1-cross-plan-impact-audit.md` 2026-04-28):

- **Workarounds in skill bodies.** `stage-file` SKILL hard_boundary explicitly documents `task_spec_section_write` write-path missing `raw_markdown` column → `task_insert` placeholder dance + "DB workaround / `task_raw_markdown_write` MCP when present".
- **No machine-readable cross-plan health view.** Today's audit doc was hand-authored from preamble ILIKE scans + `ia_stages` group-by. 24 master plans tracked; manual every time.
- **No critical-path / unblock query.** Sibling-unblock chains (e.g. `asset-pipeline 19.3 → multi-scale 2.6 + game-ui consumers`) inferred from human reading of preambles.
- **Drift scope narrow.** `arch_drift_scan` covers only arch decision writes. Glossary churn, retired-surface scans, Task `Intent` quality, Depends-on graph drift — none flagged.
- **Skill changelog convention partial.** `stage-file` / `ship-stage` / `stage-authoring` / `master-plan-{new,extend}` / `stage-decompose` lack persistent `## Changelog` section → `skill-train` retrospect cycle starves on these high-traffic skills.
- **Closeout monolith.** `stage_closeout_apply` MCP applies migrations + archive + status flips + id-purge in one call. Mid-call partial state has no diagnose path; resume = manual SQL.
- **Audit-row dup risk.** `master_plan_change_log_append` lacks UNIQUE constraint mirror that `arch_changelog` got in migration `0038`. Retry on Pass B can append twice.
- **Author-time intent quality unenforced.** Task `Intent` column free prose; vague verbs (`add support for X`, `improve Y`) survive into `/stage-file`. No mirror of `plan-digest-lint` at decompose-time.

Goal: close the small gaps (1-MCP-tool fixes), automate the cross-plan audit surface, and add a thin author-time quality gate — without expanding lifecycle skill count.

## Approaches surveyed

### Approach A — Targeted MCP additions only (small, mechanical)

Land the obvious fillers; keep skill chain unchanged.

- `task_raw_markdown_write` MCP — closes explicit `stage-file` workaround.
- `master_plan_change_log_append` UNIQUE constraint — migration mirror of `0038_arch_changelog_unique`.
- `stage_closeout_apply` PG-txn wrap + structured `stage_closeout_diagnose` returning per-step success.
- `backfill:arch-surfaces` promoted to MCP tool so `arch-drift-scan` resolution path can dispatch inline.

**Pros:** smallest blast radius; each item ≤30 LOC; no skill churn; ships in one Stage.
**Cons:** doesn't address cross-plan health surface or author-time quality gate.

### Approach B — Cross-plan health + critical-path surface (medium)

Add MCP tooling that replaces the hand-written cross-plan audit doc workflow.

- `master_plan_health` MCP — per-slug rollup `{n_stages, n_done, n_in_progress, oldest_in_progress_age_days, missing_arch_surfaces[], drift_events_open, sibling_collisions[]}`.
- `stage_dependency_graph` MCP — walks Task `depends_on` edges + `arch_surfaces` overlap → emits unblock-critical-path tree per slug or globally.
- `master_plan_cross_impact_scan` MCP — replicates the 0428 audit doc shape from queries; emits same impact table + drift hotspots.
- Optional: CLI wrapper `node tools/scripts/lifecycle-status.mjs` so humans can query without an agent session.

**Pros:** removes recurring hand-audit toil; release-rollout tracker auto-populates; "what to ship next" answerable from a query, not a 60-line audit doc.
**Cons:** larger surface (3 MCP tools + tests + fixtures); needs schema for `sibling_collisions` definition.

### Approach C — Author-time quality gates (medium)

Tighten the planning surface so weak plans never reach `/stage-file`.

- `intent_lint` MCP — Task `Intent` column author-time validator (≤2 sentences, glossary-aligned, concrete-verb gate). Mirrors `plan-digest-lint` shape; runs at `stage-decompose` + `master-plan-extend` Phase N.
- `task_intent_glossary_align` MCP — given Task row → intent ↔ glossary alignment + retired-surface scan. Reusable from `/plan-review` semantic pass too.
- Skill `## Changelog` presence enforced — add validator `validate:skill-changelog-presence`; backfill the 6 skills missing the section.
- Stage block `Arch surfaces` typed empty marker — replace literal `none` string with `is_cross_cutting: bool` + empty array.

**Pros:** shrinks the drift detection problem upstream; `skill-train` cycle gets fed for high-traffic skills; "vague intent" + retired-surface bugs caught before spec authoring.
**Cons:** touches skill body prose (5 skills); migration churn for `Arch surfaces` empty marker; weakest immediate ROI without (B).

### Approach D — All three combined, sequenced

Same items as A + B + C, ordered: A first (mechanical fillers, unblocks workarounds), then B (rollup/critical-path), then C (author-time quality). One master plan, 3 Stages.

**Pros:** complete; each Stage shippable independently; later Stages benefit from earlier MCP additions (e.g. `master_plan_health` can surface `intent_lint` warnings once both land).
**Cons:** longest commitment; needs cardinality discipline (≥2 Tasks/Stage, ≤6 soft) — likely 4–5 Stages once decomposed.

### Approach E — Defer until next friction wave

Park this exploration; revisit after sprite-gen drift resolved + 5 medium-drift plans (`web-platform`, `landmarks`, `utilities`, `distribution`, `ui-polish`, `citystats-overhaul`) get `/arch-drift-scan` passes. Drift hotspots may absorb half the items naturally.

**Pros:** zero risk; lets real friction signal accumulate before fix.
**Cons:** workarounds harden into permanent shape (`stage-file` SKILL prose stops being a "TODO" once it's been there 3 weeks); audit doc workflow gets repeated 2–3 more times.

## Recommendation

Lean **Approach D — sequenced A→B→C** but defer formal selection until polling closes. Ranking by leverage × mechanical applicability:

1. `task_raw_markdown_write` MCP — closes named workaround.
2. `master_plan_health` MCP — replaces hand-written cross-plan audit.
3. Skill `## Changelog` backfill + validator — feeds `skill-train` retrospect.
4. `stage_closeout_apply` txn wrap + `stage_closeout_diagnose`.
5. `stage_dependency_graph` MCP.
6. `intent_lint` MCP + `task_intent_glossary_align`.
7. `master_plan_change_log_append` UNIQUE constraint.
8. `backfill:arch-surfaces` promoted to MCP.

Items 1–4 are pure infrastructure — no skill prose churn. Items 5–8 affect lifecycle skill phases, so they batch into a second Stage with tests + fixture updates.

## Open questions

1. **Scope discipline.** Is this one master plan covering A+B+C, or two (A+B as `db-lifecycle-mvp-extensions` + C as separate `lifecycle-author-quality` later)?
2. **`master_plan_health` shape.** Which fields are mandatory vs optional? Does it return rollup-only or per-Stage breakdown? Does it merge with `runtime_state` for last-verify timestamps?
3. **`stage_dependency_graph` scope.** Walks only `depends_on` edges, or also `arch_surfaces` overlap + sibling-orchestrator collisions? Topological sort or just edge list?
4. **`intent_lint` enforcement.** Hard gate (blocks `/stage-file`) or soft warning (logs to `drift_warnings`)? Glossary-alignment threshold?
5. **`Arch surfaces` empty marker.** Migrate `none` → `is_cross_cutting: bool` flag now (touches `0036_stage_arch_surfaces_extension` reshape work) or accept literal-string convention as ledgered?
6. **Skill changelog backfill.** Author empty `## Changelog` sections + let entries accrue, or seed with synthesized entries from recent commit history?
7. **Stage cardinality.** Items 1–8 → does this fit ≤6 Tasks/Stage soft cap, or split into 2–3 Stages? (Likely 3 Stages once decomposed.)
8. **Validator chain impact.** New `validate:skill-changelog-presence` joins `validate:all`; cost vs CI runtime budget?
9. **Migration ordering.** `master_plan_change_log_append` UNIQUE add — does any in-flight plan have known dup audit rows that need cleanup before constraint?
10. **`backfill:arch-surfaces` promotion.** MCP tool wrapper around shell script, or rewrite as native TS reading from DB? Authority over when it runs (manual vs `arch-drift-scan` resolution path)?

## Methodology notes

- Friction signal sourced from: commit log since `2026-04-26`, `stage-file` / `ship-stage` / `stage-authoring` SKILL.md hard_boundaries + agent-body workarounds, today's cross-plan impact audit doc.
- DB schema reviewed: `db/migrations/0015..0040` (DB-primary lifecycle + arch tables).
- Tool surface reviewed: `tools/mcp-ia-server/src/tools/{master-plan,stage,task,arch,plan-digest}*.ts`.
- Out of scope this exploration: `/code-review` chain, `verify-loop`, sprite-gen drift, web-platform drift, Unity bridge tooling.

### Architecture Decision — DEC-A18 (locked 2026-04-28)

**slug**: `DEC-A18`
**title**: `db-lifecycle-extensions-2026-04-28`
**status**: `active` (locked via `arch_decision_write` 2026-04-28 16:29:55-04; row id 18; changelog row id 6, kind `design_explore_decision`)

**rationale**: DB-architecture leverage points compound — each shaves agent token cost + speeds skill chain. Lock now to amortize migration churn into one window.

**alternatives considered**:
- `wait-for-friction` — defer until pain compounds across more plans; rejected (workarounds harden into permanent SKILL prose after ~3 weeks)
- `mechanical-fillers-now-arch-drift-later` — split A from B+C into two plans; rejected (loses sequencing leverage; later Stages benefit from earlier MCP additions)
- `targeted-subset-only` — ship single highest-pain opportunity, defer rest; rejected (each opportunity has ≤30 LOC blast radius; bundle amortizes review + verify cycles)

**affected `arch_surfaces` (mapped to live rows)**:
- `data-flows/persistence`
- `interchange/agent-ia`

**unmapped surfaces (recommend register pre-Stage 1 — Stage X candidates)**:
- `ia_master_plans` / `ia_stages` / `ia_tasks` / `ia_master_plan_change_log` tables (no `arch_surfaces` row)
- `arch_surfaces` / `arch_decisions` self-reference tables
- `mcp_tool_registry` surface (logical surface — tool catalog drift)
- `lifecycle_skill_chain` surface (skill author-time invariants)
- `validator_chain` surface (`validate:all` chain composition)

**scope locks**:
- **BF=forward-only backfill** — new columns leave existing rows NULL; document NULL-tolerance in MCP tool contracts. No historical synthesize.
- **SK=inline skill edits** — per-opportunity SKILL.md diffs land in same Stages as MCP/schema changes (no separate skill-edit Stage).

**MCP write log (2026-04-28 post-restart)**:

```text
1. arch_decision_write → ok (slug=DEC-A18, id=18, status=active)
2. arch_changelog_append → ok (id=6, kind=design_explore_decision, deduped=false)
3. arch_drift_scan → FAILED (SQL error: "column mp.status does not exist" — tool bug)
```

### Architecture Decision Drift Scan

**status**: blocked by tool defect 2026-04-28.

**defect**: `mcp__territory-ia__arch_drift_scan` returned SQL error code 42703 — `column mp.status does not exist`. Hint suggests tool's SQL references `mp.status` but live schema only has `s.status` on Stages table. Master-plan-level status column missing or aliased differently.

**impact**: drift scan deferred — DEC-A18 lock still effective (decision row + changelog row written). Master plans not auto-flagged for surface drift this run; manual verification required pre-Stage X if any open plan touches `data-flows/persistence` / `interchange/agent-ia`.

**follow-up**: file BUG- against `arch_drift_scan` tool (likely in `tools/mcp-ia-server/src/tools/arch.ts` SQL query). Re-run drift scan after fix.

## Opportunities surveyed (post-pivot 2026-04-28)

Post-Q8 pivot: widened scope beyond A/B/C to survey DB-arch leverage opportunities. 13 surfaced; user picked 6 (marked `[picked]`). Picks fold into Approach D scope alongside A+B+C original items.

1. **`ia_tasks.estimated_size` + `risk_tag` cols.**
   - Friction: sizing memory absent; same Task class re-estimated per plan.
   - Shape: 2 nullable cols (`estimated_size text`, `risk_tag text[]`); `task_insert` + `task_raw_markdown_write` accept; `master_plan_health` rolls up.
   - Leverage: feeds future estimation calibration; cheap to backfill.
   - Risk: free-text drift without enum.

2. **`expected_files_touched` + `actual_files_touched` JSONB cols.** `[picked]`
   - Friction: no diff-anomaly signal; hand-grep commit-vs-plan diffs.
   - Shape: 2 JSONB cols on `ia_tasks`; `expected` written at `/stage-file`, `actual` written at `/ship-stage` Pass B from commit diff; new MCP `task_diff_anomaly_scan(slug)` flags Tasks where `actual ⊄ expected ∪ tolerance_globs`.
   - Leverage: catches scope creep + missed-file regressions; reuses existing commit-sha denorm.
   - Risk: glob tolerance config; baseline noise on first 2–3 plans.

3. **`ia_tasks.commit_sha` denorm.**
   - Friction: stage-level `commit_sha` only; Task→sha lookup requires log walk.
   - Shape: nullable `commit_sha text` on `ia_tasks`; `/ship-stage` Pass B writes per-Task.
   - Leverage: enables Task-level diff queries + `task_diff_anomaly_scan` precision.
   - Risk: redundant if stages stay 1-Task; useful at ≥3-Task stages.

4. **`ia_stages.depends_on[]` col + `master_plan_next_actionable(slug)` MCP.** `[picked]`
   - Friction: critical-path / next-unblock answered by reading preambles.
   - Shape: `depends_on text[]` (Stage slug refs) on `ia_stages`; MCP `master_plan_next_actionable(slug)` returns Stage list where `status='pending' AND all(depends_on).status IN ('done','in_progress')`.
   - Leverage: replaces `stage_dependency_graph` simpler-call use; B2 stays for full graph.
   - Risk: cycle detection at insert time.

5. **`ia_master_plans.parent_slug` + `plan_kind` cols.**
   - Friction: spinoff plans (e.g. `asset-pipeline-stage-13.1` from `asset-pipeline`) lack programmatic lineage.
   - Shape: `parent_slug text`, `plan_kind text` (enum-ish: `mvp`, `spinoff`, `extension`).
   - Leverage: cross-plan audit can group by lineage; scope-creep detection.
   - Risk: deferred — no immediate signal beyond audit doc.

6. **MCP `task_batch_insert` (multi-row, intra-batch dep resolve).** `[picked]`
   - Friction: `/stage-file` writes Tasks one-by-one; `depends_on` referencing same-batch Tasks needs round-trip.
   - Shape: array input; resolves intra-batch refs by transient label; single PG txn; returns id map.
   - Leverage: 5–10× faster `/stage-file` apply; atomic rollback on partial fail.
   - Risk: label-collision validation.

7. **MV `ia_master_plan_health` + MCP `master_plan_health`.** `[picked]`
   - Friction: B1 ad-hoc query slow on 24+ plans; rollup re-computed per call.
   - Shape: materialized view on `ia_master_plans` × `ia_stages` × `ia_tasks` × `ia_master_plan_change_log` × `arch_surfaces`; refreshed on `stage_closeout_apply` + `task_insert`; MCP `master_plan_health(slug?)` reads MV; merges with `runtime_state` for last-verify per Q3.
   - Leverage: replaces B1 ad-hoc; sub-50ms rollup; feeds future dashboard.
   - Risk: refresh trigger placement; staleness window.

8. **MCP `task_dep_register`.** `[picked]`
   - Friction: SKILL bodies note "register depends_on after `task_insert`" workaround; no atomic surface.
   - Shape: `task_dep_register(task_id, depends_on: text[])`; validates target Task ids exist + no cycle; single PG txn.
   - Leverage: closes named SKILL workaround; pairs with #6 for full atomic Stage author.
   - Risk: cycle check cost on dense graphs.

9. **UNIQUE `(slug, kind, commit_sha)` on `ia_master_plan_change_log`.**
   - Already in Approach A2 — duplicate listing.
   - Migration mirror of `0038_arch_changelog_unique`.

10. **MCP `stage_decompose_apply` (decompose↔file atomic prose→rows).** `[picked]`
    - Friction: `/stage-decompose` + `/stage-file` are separate commands; partial-fail leaves prose committed without DB rows.
    - Shape: `stage_decompose_apply(slug, stage_idx, prose_block, tasks: TaskInsert[])`; single PG txn; rollback on any sub-step fail; emits structured diagnose.
    - Leverage: collapses 2-step author into 1; prevents prose-DB drift; pairs with #6.
    - Risk: scope overlap with `/stage-file` skill — needs SKILL.md update to dispatch to MCP.

11. **`ia_tasks.intent` column.**
    - Friction: `Intent` field only in raw_markdown prose; not queryable.
    - Shape: extract `Intent:` line into denormalized text col; `intent_lint` (C1) reads from col, not prose.
    - Leverage: makes C1 simpler + faster; query-able for `task_intent_glossary_align` (C2).
    - Risk: schema migration on existing rows; back-author intent extraction.

12. **`ia_tasks.parent_task_id` FK.**
    - Friction: sub-task / split-task lineage absent.
    - Shape: nullable FK to self.
    - Leverage: future task-split workflow; not needed for current scope.
    - Risk: deferred — no immediate signal.

13. **MCP `arch_surface_load_count(surface)`.**
    - Friction: hot-surface detection (which arch surface gets touched most) hand-counted.
    - Shape: query over `arch_surface_links` group-by + count; emits top-N.
    - Leverage: feeds drift hotspot ranking; informs "extract subsystem" decisions.
    - Risk: deferred — useful but not blocking; 1-query convenience tool.

Closing line: Picks fold into Approach D scope alongside A+B+C original items. User selection 2026-04-28: ids 2, 4, 6, 7, 8, 10.

---

## Design Expansion

### Chosen Approach

**Approach D — sequenced A→B→C** + 6 picked DB-arch leverage opportunities (#2, #4, #6, #7, #8, #10), under DEC-A18 lock. One master plan, 3 Stages, total ≤18 Tasks. Sequencing rationale: mechanical fillers (Stage 1) unblock named SKILL workarounds + provide MCP primitives that Stage 2 cross-plan health surface composes; Stage 3 author-time quality gates layer on top of Stage 2's health rollup. Scope locks: BF=forward-only backfill (NULL-tolerant cols), SK=inline skill edits (per-opportunity SKILL.md diffs in same Stage as schema/MCP).

### Components

- **Schema migration runner** — sequenced PG migrations `0041`–`0046` (per-opportunity DDL + UNIQUE constraint + materialized view).
- **MCP tool surface** — 10 new tools: `task_raw_markdown_write`, `master_plan_health`, `master_plan_next_actionable`, `master_plan_cross_impact_scan`, `task_diff_anomaly_scan`, `task_batch_insert`, `task_dep_register`, `stage_decompose_apply`, `stage_closeout_diagnose`, `intent_lint`, `task_intent_glossary_align`.
- **Skill chain integrations** — `/stage-file`, `/stage-decompose`, `/master-plan-extend`, `/ship-stage` Phase rewires + 6 skills (`stage-file`, `ship-stage`, `stage-authoring`, `master-plan-new`, `master-plan-extend`, `stage-decompose`) get persistent `## Changelog` section.
- **Validator hooks** — `validate:skill-changelog-presence` (joins `validate:all`) + `validate:master-plan-health-rollup` smoke (cron-style sanity check).
- **MV refresh trigger** — `ia_master_plan_health` materialized view refresh on `stage_closeout_apply` txn commit + on `task_insert`. Refresh strategy: synchronous within closeout txn for staleness=0 on read; alternative async LISTEN/NOTIFY noted as Stage 2 implementation choice.

### Data flow

- **Author write-path**: `/stage-decompose` → `intent_lint` (soft warn, terminal output per Q6) → `stage_decompose_apply` (atomic prose+rows in single PG txn) → DB rows persist with `depends_on[]` + `expected_files_touched`.
- **Ship write-path**: `/ship-stage` Pass B → `task_dep_register` resolves remaining edges → commit → `task_diff_anomaly_scan` flags drift (`expected_files_touched` ↔ commit-derived `actual_files_touched`) → `stage_closeout_apply` (txn-wrapped) → MV refresh.
- **Read-path (agent)**: caller → `master_plan_health(slug)` reads MV → merges with `runtime_state` last-verify timestamp → caveman summary back to agent.
- **Cross-plan audit path**: `master_plan_cross_impact_scan()` → joins `ia_master_plans` × `arch_surface_links` × open `arch_drift_events` → emits same shape as today's hand-written audit doc.

### Interfaces / contracts

MCP tool signatures (caveman one-liners; all returns include `ok: bool` + structured `error` on failure):

- `task_raw_markdown_write(task_id: text, body: text) -> {ok}` — closes named `stage-file` workaround; persists raw markdown into existing `ia_tasks.raw_markdown` after migration `0041`.
- `master_plan_health(slug?: text) -> {n_stages, n_done, n_in_progress, oldest_in_progress_age_days, missing_arch_surfaces[], drift_events_open, sibling_collisions[], last_verify_at}` — reads MV; merges `runtime_state.json` for `last_verify_at`. NULL-tolerant: missing cols return null, not error.
- `master_plan_next_actionable(slug: text) -> [{stage_id, slug, depends_on_resolved: bool}]` — depends_on resolution; replaces preamble reads.
- `master_plan_cross_impact_scan() -> {plans: [{slug, drift_open, missing_surfaces[], sibling_collisions[]}]}` — replaces hand-written audit doc shape.
- `task_diff_anomaly_scan(slug: text) -> [{task_id, expected_files[], actual_files[], unexpected_files[], missed_files[]}]` — JSONB col diff; tolerance globs read from `ia_master_plans.tolerance_globs jsonb` (default `[]`).
- `task_batch_insert(stage_id: text, tasks: TaskInsert[]) -> {id_map: {label: task_id}}` — intra-batch dep resolve via transient `label` field; single PG txn; rejects label collisions.
- `task_dep_register(task_id: text, depends_on: text[]) -> {ok}` — atomic with cycle check (Tarjan SCC on insert).
- `stage_decompose_apply(slug: text, stage_idx: int, prose_block: text, tasks: TaskInsert[]) -> {ok, ids: text[]}` — single PG txn wrapping prose write + rows insert; rollback on any sub-step fail.
- `stage_closeout_diagnose(slug: text, stage_idx: int) -> {steps: [{name, ok, error?}]}` — partial-state recovery; read-only, no mutation.
- `intent_lint(intent_text: text) -> {ok, warnings: [{rule, msg}]}` — soft gate (`ok: false` does not block; emits warnings only).
- `task_intent_glossary_align(task_id: text) -> {aligned: bool, retired_surfaces[], missing_terms[]}` — reusable from `/plan-review` semantic pass.

### Non-scope (explicit defer list)

- Opportunities #1, #3, #5, #11, #12, #13 — deferred to future `db-lifecycle-post-mvp-extensions.md`.
- Cross-plan audit MVP punts: deeper sibling-collision algorithms (Q5 inline-only); CLI human wrapper (B optional).
- Backfill of historical rows (BF=a forward-only locked).
- `/code-review`, `/verify-loop`, sprite-gen, web-platform, Unity bridge tooling.
- Skill `## Changelog` synthesized historical entries (Q6 SK=a inline — empty section seed only).

### Architecture diagram

```mermaid
flowchart LR
    A[/stage-decompose/] --> B[intent_lint MCP]
    B --> C[stage_decompose_apply MCP]
    C --> D[(ia_stages + ia_tasks + depends_on[])]
    E[/stage-file/] --> F[task_batch_insert MCP]
    F --> G[task_dep_register MCP]
    G --> D
    H[/ship-stage Pass B/] --> I[stage_closeout_apply txn]
    I --> J[(ia_master_plan_change_log UNIQUE)]
    I --> K[MV ia_master_plan_health refresh]
    L[/agent query/] --> M[master_plan_health MCP]
    M --> K
    M --> N[runtime_state JSON]
    O[task_diff_anomaly_scan] --> D
    P[arch_drift_scan] --> Q[(arch_decisions + arch_changelog)]
    R[master_plan_cross_impact_scan] --> K
    R --> Q
```

**Entry points (callers → tools)**:
- Agent in `/stage-decompose` skill → `intent_lint` + `stage_decompose_apply`
- Agent in `/stage-file` skill → `task_batch_insert` + `task_dep_register` + `task_raw_markdown_write`
- Agent in `/ship-stage` Pass B → `stage_closeout_apply` + `task_diff_anomaly_scan`
- Agent in `/master-plan-extend` Phase N → `master_plan_health`
- Agent ad-hoc → `master_plan_health` / `master_plan_next_actionable` / `master_plan_cross_impact_scan` / `stage_closeout_diagnose`

**Exit points (tools → consumers)**:
- DB rows in `ia_tasks` / `ia_stages` / `ia_master_plan_change_log`
- MV `ia_master_plan_health` refreshed on closeout commit
- Caveman summary back to agent (or structured JSON for tool-chained calls)

### Subsystem Impact

| Subsystem | Dependency nature | Invariant risk | Breaking vs additive | Mitigation |
|---|---|---|---|---|
| `ia_tasks` schema | Writes — 4 new cols (`expected_files_touched`, `actual_files_touched`, `commit_sha` denorm in opp #3 deferred, `raw_markdown` already present) | Invariant 13 (id counter) — n/a; schema change only | Additive (NULL-tolerant per BF=a) | Document NULL handling in MCP tool contracts; no historical backfill |
| `ia_stages` schema | Writes — 1 new col (`depends_on text[]`) | n/a | Additive | Cycle check on insert (Tarjan SCC); empty array default |
| `ia_master_plan_change_log` schema | Writes — UNIQUE `(slug, kind, commit_sha)` constraint | Invariant n/a; data-integrity gain | Potentially breaking if existing dup rows | Pre-migration dedup audit (Q9 open question); fail loudly on dup at migration time |
| `arch_surfaces` table | Reads — DEC-A18 surface mapping | n/a — surface lookup | Additive | Recommend register `lifecycle_skill_chain` + `mcp_tool_registry` + `validator_chain` surfaces pre-Stage 1 |
| `runtime_state.json` | Reads — `last_verify_at` merge into `master_plan_health` | n/a | Additive (read-only) | Tolerate missing file (default `null`) |
| MCP tool registry | Writes — 10 new tool descriptors | Universal safety: schema cache reload required | Additive | Stage X documents user-side restart checkpoint between Stages |
| Lifecycle skill chain (`/stage-decompose`, `/stage-file`, `/ship-stage`, `/master-plan-extend`) | Writes — SKILL.md body diffs (per-opportunity inline edits per SK=a) | Caveman authoring rule applies; agent-output-caveman.md compliance | Additive (workaround removal + new MCP dispatch) | Same-Stage skill+MCP edits prevent skill drift |
| Validator chain (`validate:all`) | Writes — `validate:skill-changelog-presence` joins; cost vs CI runtime budget (Q8 open) | n/a | Additive | Single-pass file scan; expected <1s |
| Architecture sub-specs (`ia/specs/architecture/*`) | Reads — surface_slugs lookup | n/a | n/a — gap noted | MCP `spec_section` keys for architecture/* not registered (per Phase 5 tool result `spec_not_found`); fallback to direct file read for any lifecycle doc cross-ref |

**Invariants flagged**: none at risk (all writes additive + NULL-tolerant; id counter + force-loaded denylist untouched). MCP `invariants_summary` skipped — tooling-only design, no Unity C# / runtime touches.

### Implementation Points

**Stage 1 — Mechanical fillers (Approach A items + #8 + UNIQUE constraint)** [6 Tasks]

- [ ] **T1.1** — migration `0041_task_raw_markdown_extend` (verify col present; if not, add `raw_markdown text`); MCP `task_raw_markdown_write`
- [ ] **T1.2** — migration `0042_master_plan_change_log_unique` — UNIQUE `(slug, kind, commit_sha)`; pre-migration dedup audit + fail-loud on dup
- [ ] **T1.3** — `stage_closeout_apply` PG txn wrap + MCP `stage_closeout_diagnose` (read-only, per-step)
- [ ] **T1.4** — MCP `task_dep_register` with Tarjan SCC cycle check
- [ ] **T1.5** — SKILL inline edits: `/stage-file` agent-body removes `task_raw_markdown_write` workaround block; `/ship-stage` agent-body removes `task_dep_register` post-insert workaround note
- [ ] **T1.6** — `backfill:arch-surfaces` shell script promoted to MCP tool (TS rewrite reading from DB)

Risk: Q9 (dup audit rows pre-UNIQUE) — gate T1.2 on dedup query result.

**Stage 2 — Cross-plan health surface (Approach B + #4 + #7)** [6 Tasks]

- [ ] **T2.1** — migration `0043_ia_stages_depends_on` — `depends_on text[]` col; cycle check on insert
- [ ] **T2.2** — migration `0044_ia_master_plan_health_mv` — MV defs + sync refresh trigger on `stage_closeout_apply` commit + on `task_insert`
- [ ] **T2.3** — MCP `master_plan_health(slug?)` — reads MV; merges `runtime_state` JSON
- [ ] **T2.4** — MCP `master_plan_next_actionable(slug)` — depends_on resolution
- [ ] **T2.5** — MCP `master_plan_cross_impact_scan` — replaces hand audit doc shape
- [ ] **T2.6** — SKILL inline edits: `/stage-decompose` writes `depends_on[]` at decompose time; `/master-plan-extend` Phase N reads `master_plan_health` for cardinality gate input

Risk: MV refresh placement — synchronous adds <50ms to closeout txn vs. async LISTEN/NOTIFY staleness window. Default to sync; document escape hatch.

**Stage 3 — Author-time quality gates (Approach C + #2 + #6 + #10)** [6 Tasks]

- [ ] **T3.1** — migrations `0045_ia_tasks_files_touched` (JSONB cols `expected_files_touched` + `actual_files_touched` + `tolerance_globs jsonb` on `ia_master_plans`) + `0046_skill_changelog_validator` (validator wiring scaffold)
- [ ] **T3.2** — MCP `intent_lint` (soft warn) + `task_intent_glossary_align`
- [ ] **T3.3** — MCP `task_batch_insert` (intra-batch dep resolve)
- [ ] **T3.4** — MCP `stage_decompose_apply` (atomic prose+rows)
- [ ] **T3.5** — MCP `task_diff_anomaly_scan` (JSONB diff with tolerance globs)
- [ ] **T3.6** — SKILL inline edits: `/stage-decompose` calls `intent_lint` (soft warn at terminal output per Q6); `/stage-decompose` + `/stage-file` collapse via `stage_decompose_apply`; 6 skills (`stage-file`, `ship-stage`, `stage-authoring`, `master-plan-new`, `master-plan-extend`, `stage-decompose`) get empty `## Changelog` section seeded (no synthesized history per SK=a)

Risk: `intent_lint` rule registry tunability — externalize as JSON for `skill-train` adjustment.

**Deferred / out of scope**:
- Opportunities #1, #3, #5, #11, #12, #13 → future `db-lifecycle-post-mvp-extensions.md` exploration doc
- Historical row backfill (BF=a forward-only locked)
- Skill changelog synthesized entries (SK=a inline — empty seed only)
- Stage X (pre-Stage 1 prep): register unmapped surfaces in `arch_surfaces` (`lifecycle_skill_chain`, `mcp_tool_registry`, `validator_chain`) — recommend file before Stage 1, but not gating

### Examples

**`master_plan_health(slug="asset-pipeline")`**

Input:
```json
{ "slug": "asset-pipeline" }
```

Output:
```json
{
  "ok": true,
  "n_stages": 14,
  "n_done": 12,
  "n_in_progress": 1,
  "oldest_in_progress_age_days": 3,
  "missing_arch_surfaces": [],
  "drift_events_open": 0,
  "sibling_collisions": [],
  "last_verify_at": "2026-04-28T14:32:11Z"
}
```

Edge case — plan has zero stages (just-created):
```json
{
  "ok": true,
  "n_stages": 0,
  "n_done": 0,
  "n_in_progress": 0,
  "oldest_in_progress_age_days": null,
  "missing_arch_surfaces": [],
  "drift_events_open": 0,
  "sibling_collisions": [],
  "last_verify_at": null
}
```

**`task_batch_insert`**

Input (intra-batch label refs):
```json
{
  "stage_id": "stage_db-lifecycle-extensions_1",
  "tasks": [
    { "label": "label_a", "intent": "Add raw_markdown col + MCP", "depends_on": [] },
    { "label": "label_b", "intent": "Wire SKILL body to new MCP", "depends_on": ["label_a"] }
  ]
}
```

Output:
```json
{
  "ok": true,
  "id_map": {
    "label_a": "task_db-lifecycle-extensions_1.1",
    "label_b": "task_db-lifecycle-extensions_1.2"
  }
}
```

Edge case — label collision:
```json
{ "ok": false, "error": { "code": "label_collision", "label": "label_a" } }
```

**`stage_decompose_apply`**

Input: `slug="db-lifecycle-extensions"`, `stage_idx=1`, prose_block (Stage 1 markdown), tasks array (6 TaskInserts with labels). Output `{ok: true, ids: [...]}`. Edge case — partial-fail on Task 4 insert: rollback rolls back prose write + first 3 inserts; emits structured diagnose.

**`task_diff_anomaly_scan(slug="db-lifecycle-extensions")`**

Input:
```json
{ "slug": "db-lifecycle-extensions" }
```

Output (one Task with drift):
```json
{
  "ok": true,
  "results": [
    {
      "task_id": "task_db-lifecycle-extensions_1.1",
      "expected_files": ["db/migrations/0041_*.sql", "tools/mcp-ia-server/src/tools/task.ts"],
      "actual_files": ["db/migrations/0041_task_raw_markdown_extend.sql", "tools/mcp-ia-server/src/tools/task.ts", "Assets/Scripts/Foo.cs"],
      "unexpected_files": ["Assets/Scripts/Foo.cs"],
      "missed_files": []
    }
  ]
}
```

Edge case — `expected_files_touched` is NULL (Task created pre-migration `0045`): tolerated (BF=a forward-only); returns `expected_files: []` + skip drift comparison.

**`intent_lint`**

Input (vague):
```json
{ "intent_text": "add support for X" }
```

Output:
```json
{
  "ok": false,
  "warnings": [
    { "rule": "concrete-verb", "msg": "vague verb 'add support'; prefer 'wire', 'extend', 'migrate'" }
  ]
}
```

Edge case — empty intent:
```json
{ "intent_text": "" }
```
→ `{ "ok": true, "warnings": [{ "rule": "non-empty", "msg": "intent empty" }] }` (soft warn — does not block).

### Review Notes

Phase 8 ran as inline structured self-review (Plan subagent deferred — same MCP cache miss session that blocked DEC-A18 writes). All BLOCKING items resolved before persist. NON-BLOCKING + SUGGESTIONS captured below; user may re-run external Plan-subagent review post-restart for second-pass sign-off.

**BLOCKING (resolved before persist)**:
- (Resolved) Phase 3 missed `master_plan_cross_impact_scan` interface — added under Interfaces / contracts.
- (Resolved) MV refresh strategy ambiguity — sync-vs-async called out under Components + T2.2 risk note.
- (Resolved) Skill changelog backfill list vague — 6 skills enumerated by name in T3.6.

**NON-BLOCKING**:
- T2.1 cycle check cost on dense graphs not yet benchmarked. Recommend smoke target: 100-stage synthetic plan with random-50%-edge density → assert <50ms insert.
- `task_diff_anomaly_scan` baseline noise on first 2–3 plans (per opp #2 risk) — recommend Stage 3 closeout includes baseline-tolerance tuning pass.
- Stage X (pre-Stage 1 surface registration) is recommended but not gating — promotion depends on whether `lifecycle_skill_chain` becomes target of further DEC-A* decisions.

**SUGGESTIONS**:
- Externalize `intent_lint` rule registry to JSON (under `ia/state/` or `tools/mcp-ia-server/data/`) for `skill-train` tunability without code edit.
- Consider `master_plan_health` returning per-Stage breakdown (Q2 partial) as Phase-2 follow-up; current rollup-only shape covers MVP query needs.
- `task_diff_anomaly_scan` tolerance globs schema: store on `ia_master_plans.tolerance_globs jsonb` (default `[]`); document common globs (test fixtures, generated indexes).

### Expansion metadata

- Date: 2026-04-28
- Model: claude-opus-4-7
- Approach selected: D (sequenced A→B→C + 6 picked DB-arch opportunities)
- Blocking items resolved: 3
- DEC-A18 status: locked 2026-04-28 (decision row id 18 + changelog row id 6; drift scan deferred — tool SQL bug)
- Subagent review mode: inline self-review (Plan subagent deferred to same restart cycle)

---

## Next step

Run `/design-explore docs/db-lifecycle-extensions-exploration.md` to drive selection (Approach A / B / C / D / E) + expand into Design Expansion block + master-plan seed.
