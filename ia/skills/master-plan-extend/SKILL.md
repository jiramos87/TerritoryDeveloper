---
purpose: "Extend an existing `ia/projects/{slug}-master-plan.md` with new Stages sourced from an exploration or extensions doc. Appends — never rewrites existing Stages. Canonical shape: `docs/MASTER-PLAN-STRUCTURE.md`."
audience: agent
loaded_by: skill:master-plan-extend
slices_via: router_for_task, spec_sections, invariants_summary, glossary_discover, glossary_lookup
name: master-plan-extend
description: >
  Use when an existing master plan orchestrator needs new Stages sourced from an exploration doc (with
  persisted `## Design Expansion`) OR an extensions doc (e.g. `{slug}-post-mvp-extensions.md`) that was
  deferred at original author time. Appends new Stage blocks in place — never rewrites existing Stages,
  never overwrites headers, never inserts BACKLOG rows. Fully decomposes every new Stage (Task table) at
  author time — no skeletons. 2-level hierarchy `Stage > Task` (Step + Phase layers removed per
  lifecycle-refactor). Canonical shape: `docs/MASTER-PLAN-STRUCTURE.md`. Triggers:
  "/master-plan-extend {plan} {source}", "extend master plan from exploration", "add new stages to
  orchestrator", "append from extensions doc", "pull deferred stage into master plan".
model: inherit
---

# Master plan — extend orchestrator with new stages from exploration / extensions doc

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: Objectives 2–4 sentences (human-consumed cold); Exit criteria carry type/method/file precision; Relevant surfaces stay explicit (paths + line refs); Mermaid/ASCII verbatim.

No MCP from skill body. Tool recipe Phase 2 only. All other phases derive from the source doc's expansion / extensions block.

**Canonical master-plan shape:** [`docs/MASTER-PLAN-STRUCTURE.md`](../../../docs/MASTER-PLAN-STRUCTURE.md) — authoritative source for file shape, Stage block subsections, 5-column Task table schema, Status enums, lifecycle flip matrix. This skill appends new Stages TO that shape; if this skill drifts, MASTER-PLAN-STRUCTURE.md wins.

**Lifecycle:** AFTER [`master-plan-new`](../master-plan-new/SKILL.md) has authored the orchestrator AND `{SOURCE_DOC}` exists with expansion (or equivalent extensions list). BEFORE [`stage-file`](../stage-file/SKILL.md) of the new stages.
`design-explore` → `master-plan-new` → `master-plan-extend` (this skill) → `stage-file` → `stage-authoring` → `plan-reviewer-mechanical` → `plan-reviewer-semantic` → `spec-implementer` → `verify-loop` → `opus-code-review` → `/ship-stage` (inline closeout). Per canonical flow in [`docs/agent-lifecycle.md`](../../../docs/agent-lifecycle.md).

**Related:** [`master-plan-new`](../master-plan-new/SKILL.md) · [`stage-decompose`](../stage-decompose/SKILL.md) · [`stage-file`](../stage-file/SKILL.md) · [`docs/MASTER-PLAN-STRUCTURE.md`](../../../docs/MASTER-PLAN-STRUCTURE.md) · [`ia/rules/project-hierarchy.md`](../../rules/project-hierarchy.md) · [`ia/rules/orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md).

**Shape refs:** [`ia/templates/master-plan-template.md`](../../templates/master-plan-template.md) · [`blip-master-plan.md`](../../projects/blip-master-plan.md) · [`landmarks-master-plan.md`](../../projects/landmarks-master-plan.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `ORCHESTRATOR_SPEC` | User prompt | SLUG carrier. Accepts repo-relative path forms `ia/projects/{slug}-master-plan.md` OR `ia/projects/{slug}/index.md` (legacy filesystem shapes — files no longer exist post Step 9.6; only basename parses to derive `{slug}`). DB-first via `master_plan_render(slug)` — must return existing plan payload. Must match orchestrator shape (header block + `## Stages` + tracking legend + `## Orchestration guardrails`). |
| `SOURCE_DOC` | User prompt | Path to exploration doc (carries `## Design Expansion` or semantic equivalent) OR extensions doc (`{slug}-post-mvp-extensions.md` listing deferred Stages) — required. Must carry at least one Implementation Point / Roadmap entry not already represented in orchestrator. |
| `SOURCE_SECTION` | User prompt | Optional. When `SOURCE_DOC` is an umbrella multi-bucket exploration (e.g. `full-game-mvp-exploration.md`), specify the bucket heading or section slug (e.g. `Bucket 7 — Audio polish & Blip`). Phase 0 + Phase 2 load only that subsection + its Implementation Points block; remaining buckets ignored to prevent token blow-up and wrong-bucket bleed. |
| `START_STAGE_NUMBER` | User prompt | Optional `N.M` override. Default: next free `N.M` after last existing Stage (e.g. last = Stage 3.2 → new Stages start at Stage 3.3, or Stage 4.1 for new top-level cluster — see Phase 1). Extend appends — never overwrites existing Stages. |
| `SCOPE_BOUNDARY_DOC` | User prompt | Optional sibling doc listing out-of-scope items. Referenced in per-new-stage Relevant surfaces if not already in orchestrator header. |

---

## Phase sequence (gated — each phase depends on the previous)

### Phase 0 — Load + validate

Derive `{SLUG}` from `{ORCHESTRATOR_SPEC}` basename (strip `-master-plan.md` OR `/index.md` suffix — both legacy filesystem shapes). Call `master_plan_render({slug: SLUG})` to fetch DB-backed plan payload (preamble + Stage inventory + Tasks). `not_found` → STOP. Route user to `/master-plan-new {SOURCE_DOC}` (fresh orchestrator).

If `SOURCE_SECTION` provided, note it for Phase 2 scoping. The rendered payload is the source of truth — no filesystem read of `{ORCHESTRATOR_SPEC}` (file no longer exists post Step 9.6).

**Hard-required in rendered preamble (STOP if missing):**

- `## Stages` + tracking legend present (canonical Stage enum `Draft | In Review | In Progress | Final` per MASTER-PLAN-STRUCTURE.md §6.2).
- `## Orchestration guardrails` present (Do / Do not lists).
- At least one Stage row in `ia_stages` (rendered as `### Stage {N}.{M}` block) exists.

**Insert-if-missing (header-repair — do NOT STOP; inject in Phase 6 preamble write):**

- `**Last updated:** {YYYY-MM-DD}` absent → insert under the orchestrator title block (before `**Status:**` or first header field); set to today's date.
- `**Locked decisions (do not reopen in this plan):**` absent → insert as empty bullet-list field in header block.
- `**Hierarchy rules:**` line references old 3-level `step > stage > phase > task` phrasing → replace with canonical line citing `docs/MASTER-PLAN-STRUCTURE.md` first (see MASTER-PLAN-STRUCTURE.md §2).

Header-repair lands via `master_plan_preamble_write` in Phase 6 — collect deltas here, apply atomically with Stage inserts.

Missing hard-required shape in rendered preamble → STOP. Route user to `/master-plan-new {SOURCE_DOC}` (fresh orchestrator).

**Retired-surface detection (soft warn, do NOT STOP):** If preamble carries any `### Step N` block, `**Phases:**` checkbox list, or 6-column Task table with `Phase` column → emit warning in handoff recommending migration to canonical 2-level shape. Do NOT auto-migrate (structural rewrite — user decides). New Stages authored by this skill use canonical shape regardless of legacy drift in existing blocks.

Read `{SOURCE_DOC}` (filesystem — exploration / extensions docs remain on disk). If `SOURCE_SECTION` provided, load only that section + its Implementation Points / Roadmap sub-block; ignore remaining sections. Confirm expansion intent present — literal `## Design Expansion` OR semantic equivalents:

| Intent | Literal heading | Semantic equivalents accepted |
|---|---|---|
| Approach + rationale | `### Chosen Approach` | `### Decision` / `### MVP scope` / `### Deferred Stages` / `### Extensions` |
| Entry / exit points | `### Architecture` | `### Component map` / `### Key types` / `### Names registry` |
| Touched subsystems + invariant risk | `### Subsystem Impact` | `### Related subsystems` / `### Integration points` |
| Staged skeleton for new stages | `### Implementation Points` | `### Roadmap` / `### Deferred stages list` / `### Extension plan` |

Missing approach + staged skeleton intent → STOP. Extensions docs must carry at least an ordered list of deferred Stages (name + one-line objective).

Hold in working memory:

- **Existing Stage inventory** — list of `(N, M, name, status)` tuples from `master_plan_render` payload.
- **Existing Locked decisions** — must not be re-contested by new Stages (parsed from rendered preamble).
- **Source doc Implementation Points / Roadmap / Deferred list** — raw skeleton for new Stages.
- **Source doc Architecture / Component map** — entry / exit points → §"Relevant surfaces" per new Stage.
- **Source doc Subsystem Impact** — touched subsystems + invariant numbers → per-stage guardrails.
- **Source doc Locked decisions (if any)** — new locks to merge into orchestrator header (Phase 7).

### Phase 1 — Start-number resolution + duplication gate

Compute `START_STAGE_NUMBER`:

- User override provided → use it. Gate: must not collide with any existing `N.M` pair; `=` is an overwrite → STOP and ask.
- No override → infer from source doc pattern:
  - Source-doc bucket is a continuation of existing cluster `N` (scope extends same milestone) → `(N, last_M_in_cluster_N + 1)` e.g. existing Stage 3.2 → new Stage 3.3.
  - Source-doc bucket is a new milestone cluster → `(max_N + 1, 1)` e.g. new Stage 4.1.
  - Ambiguous → default to new cluster `(max_N + 1, 1)`; note in Phase 3 digest for caller review.

**Duplication gate:** for each proposed new stage name / objective (from Phase 0 source-doc skeleton), scan existing `### Stage {N}.{M} — {Name}` blocks. Name collision OR near-identical objective (>50% token overlap) → do NOT STOP outright. Apply resolution playbook:

(a) **Existing Stage is Draft AND unpersisted (0 tasks filed)** → merge: rewrite that Stage's Objectives + Exit criteria + Task table to absorb new scope. Report merge in Phase 8 handoff under "Duplication resolved — merged into existing Stage {N}.{M}".
(b) **Existing Stage is In Review / In Progress / Final** → STOP. Immutable once lifecycle has advanced. Ask user: drop new stage, rename to distinct scope, OR open a revision cycle.
(c) **Near-overlap but verifiably distinct scope** → proceed with proposed name; note in Phase 8 handoff ("near-overlap with Stage {N}.{M}; verified distinct scope — {one-line distinction}").

Fail fast — no MCP yet.

### Phase 2 — MCP context (Tool recipe) + surface-path pre-check

Run **Tool recipe** (below) via `domain-context-load` subskill. **Greenfield**: `brownfield_flag = true`. **Brownfield**: `brownfield_flag = false`. **Tooling-only**: `tooling_only_flag = true`. If source-doc references any `Assets/**` path (even future target), treat as brownfield for invariants.

Capture for Phase 4:

- `glossary_anchors` → canonical names replace ad-hoc synonyms in authored prose.
- `spec_sections` → §"Relevant surfaces" per new stage.
- `invariants` → per-new-stage guardrails + append to orchestrator header "Read first" if not already listed (Phase 7 header-sync).

**Surface-path pre-check** — run `surface-path-precheck` subskill ([`ia/skills/surface-path-precheck/SKILL.md`](../surface-path-precheck/SKILL.md)): pass paths from source-doc Architecture / Component map. Use returned `line_hint` in surfaces; mark `(new)` for `exists: false`.

Skip pre-check → downstream stages cite ghost line numbers.

### Phase 3 — New-stage proposal + digest

Produce outline (caveman one-liners) per proposed new stage BEFORE full decomposition:

```
Stage {START}.{M} — {Name} — {one-line objective} — {est 2–6 tasks}
Stage {START}.{M+1} — {Name} — {one-line objective} — {est 2–6 tasks}
...
```

Emit the planned-stages digest in the return message. Do NOT pause — subagents run single-shot; the gate moves to the caller. Digest must include:

- Ordering rationale (dep chain — scaffolding → data → logic → integration; flag if source doc implies different chain).
- Names (≤6 words each; confirm no collision with existing Stages per Phase 1).
- Scope boundary (note anything dropped per `SCOPE_BOUNDARY_DOC` or `SOURCE_SECTION` filtering).

After emitting digest, proceed directly to Phase 4. Caller reviews digest and re-fires if changes needed before persisting.

Why this gate exists: full decomposition (Phases 4–5) is expensive (task tables + intents). Digest lets caller catch scope / naming / ordering issues; re-fire with updated scope if unhappy.

### Phase 4 — Stage decomposition (new stages only)

Per confirmed new stage (ALL of them), author the canonical Stage block per MASTER-PLAN-STRUCTURE.md §3:

```markdown
### Stage {N}.{M} — {Name}

**Status:** Draft

**Notes:** tasks _pending_ — not yet filed

**Backlog state (Stage {N}.{M}):** 0 filed

**Objectives:** {2–4 sentences — what this stage lands + why. Ties back to source doc rationale. Human-consumed cold; full English OK per caveman exception}.

**Exit criteria:**

- {concrete observable outcome 1 — cites type / method / file path where verifiable}
- {outcome 2}
- {glossary row additions, if canonical terms introduced}

**Art:** {None / list of art assets needed from source doc; else `None`}.

**Relevant surfaces (load when stage opens):**

- {source doc ref + sections}
- {MCP-routed spec section refs (via Phase 2)}
- {invariant numbers from Subsystem Impact}
- {prior stage outputs — surfaces shipped by previous Stage (existing OR sibling new Stage)}
- {code paths — entry / exit points from source doc Architecture; mark `(new)` for non-existent paths per Phase 2 pre-check}

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T{N}.{M}.1 | {short name ≤6 words} | _pending_ | _pending_ | {≤2 sentences — concrete deliverable: types, methods, file paths. Reference existing patterns where applicable} |
| T{N}.{M}.2 | {short name} | _pending_ | _pending_ | {...} |
| T{N}.{M}.3 | {short name} | _pending_ | _pending_ | {...} |

#### §Stage File Plan

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

_pending — populated by Stage audit pass when all Tasks reach Done post-verify._

#### §Stage Closeout Plan

_pending — populated inline by `/ship-stage` Pass B `stage_closeout_apply` when all Tasks reach `Done`._
```

**Task table schema (5 columns, canonical per MASTER-PLAN-STRUCTURE.md §3):** `Task | Name | Issue | Status | Intent`. NO `Phase` column. Task id format `T{N}.{M}.{K}`. Status enum `_pending_ → Draft → In Review → In Progress → Done (archived)`.

**Depth rule:** All new stages fully decomposed at author time. No skeletons. Reuse Phase 2 MCP output — no additional tool calls per stage.

**Stage-ordering heuristic** (earliest first): Scaffolding → Data model → Runtime logic → Integration + tests. Follow unless source doc declares different dep chain.

**Combined Stage count (existing + new):** target ≤10; 11+ = flag scope creep (warn, do not block; recommend splitting into sibling master plan).

**Task intent concreteness bar:** avoid vague verbs ("add support for X", "handle Y"). Instead cite the thing being shipped — types / methods / file paths. Concrete intent survives the wait between authoring + `stage-file` materialization.

**Task sizing heuristic** (same as `master-plan-new` Phase 4):

- **Correct scope:** 2–5 files forming one algorithm layer. Tasks at this size keep `spec_section` reloads to ≤2.
- **Too small (merge):** single file, single function, single constant. Merge with an adjacent same-domain task in the same Stage.
- **Too large (split):** touches >3 unrelated subsystems or needs >6 phases of implementation. Split at subsystem seam.

### Phase 5 — Cardinality gate

Run `cardinality-gate-check` subskill ([`ia/skills/cardinality-gate-check/SKILL.md`](../cardinality-gate-check/SKILL.md)): pass Stage → Task count map for each **new** Stage from Phase 4. Scope: **new Stages only** — do NOT re-gate existing stages. Cardinality rule (`ia/rules/project-hierarchy.md`): ≥2 Tasks/Stage (hard), ≤6 soft.

Subskill returns `{stages_lt_2, stages_gt_6, single_file_tasks, oversized_tasks, verdict}`:

- `verdict = pause` → surface violations to user; ask split, merge, or justify. Proceed only after user confirms or fixes. Phrase split/merge question in player/designer-visible outcomes (releasable slices, user-visible checkpoints), not stage numbers or task-count math. Ids / stage numbers go on a trailing `Context:` line. Full rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).
- `verdict = proceed` → continue to Phase 6.

Also covers Phase 4 task sizing: single-file/function/struct tasks → `single_file_tasks`; >3 unrelated subsystems → `oversized_tasks`.

### Phase 6 — Persist (DB-only)

DB MCP writes only — no filesystem Edits. Operations (in order):

1. **Preamble write (atomic header sync):** Build new preamble string by merging current rendered preamble + header-repair deltas (Phase 0) + source-doc additions:
   - `**Last updated:**` → today's date (insert if absent).
   - `**Exploration source:**` → append `` `{SOURCE_DOC}` (§{new-stage-relevant sections}) — extension source for Stages {START}.{M_first}..{END}.{M_last} `` if not present.
   - `**Locked decisions (do not reopen in this plan):**` → append new bullets from source doc.
   - `**Read first if landing cold:**` → merge new invariant numbers from Phase 2.
   - `**Hierarchy rules:**` → replace retired 3-level phrasing with canonical line.
   - Call `master_plan_preamble_write({slug: SLUG, preamble: <new preamble string>})`.

2. **Stage block insertion (per new Stage):** For each new `Stage {START}.{M_first}` ... `Stage {END}.{M_last}` block from Phase 4:
   - Call `stage_insert({slug: SLUG, stage_id: "{N}.{M}", title: "{Name}", body: "<full Stage block markdown>"})`.

   **MCP gap:** `stage_insert` not yet wired (Step 9.6.8 only landed `master_plan_render` / `stage_render` / `master_plan_preamble_write` / `master_plan_change_log_append`). Skill blocked on this gap — add to Step 9.6 followup tracker. Workaround until wired: emit Stage block markdown in handoff, ask user to insert manually via SQL.

3. **Change-log audit row:** Call `master_plan_change_log_append({slug: SLUG, kind: "plan_extended", body: "Extended via {SOURCE_DOC} — +N stages ({START}.{M_first}..{END}.{M_last}), +M tasks"})`.

4. **Orchestration guardrails:** do NOT modify unless source doc introduces a new guardrail category (rare — user must explicitly request). Default behavior: leave intact.

**Do NOT:**

- Touch existing Stage rows in `ia_stages` — not even cosmetic edits (retired-surface warnings go in handoff, not auto-fix).
- Overwrite top-of-preamble `**Status:**` line — lifecycle skills flip it. (Exception: Phase 6c R6 demote — see below.)
- Insert BACKLOG rows. Create flat `ia/projects/{ISSUE_ID}.md` stubs. Tasks stay `_pending_`.
- Read or write `ia/projects/{slug}-master-plan.md` OR `ia/projects/{slug}/index.md` OR `ia/projects/{slug}/stage-*.md` — DB is sole source of truth post Step 9.6.
- Rename or delete `{SOURCE_DOC}`. Do not edit its expansion block.
- Commit. User decides when.

### Phase 6b — Regenerate progress dashboard

Run `progress-regen` subskill ([`ia/skills/progress-regen/SKILL.md`](../progress-regen/SKILL.md)): `npm run progress` from repo root. Non-blocking — failure does NOT block Phase 6c; log exit code and continue.

### Phase 6c — Demote top Status if currently Final (R6)

After persisting new Stage blocks, check the rendered preamble `> **Status:**` line:

- If top Status reads `Final` AND `appended_stages ≥ 1`: rewrite top Status to `In Progress — Stage {N_first_new}.{M_first_new} pending (extensions appended)` where `(N_first_new, M_first_new)` = first new Stage identifier. Land via second `master_plan_preamble_write` call (preamble re-fetch via `master_plan_render` → string-replace Status line → write).
- If top Status is NOT `Final` (e.g. already `In Progress`, `Draft`): leave unchanged.
- Rationale: a `Final` plan that gains new Stages is no longer complete — the top Status must reflect that active work remains.
- This flip is idempotent: re-running when Status already reflects the new stage produces zero diff.

### Phase 7 — Handoff

Single concise message (caveman) naming:

- `{ORCHESTRATOR_SPEC}` extended — `+N stages · +M tasks`. New Stage range `{START}.{M_first}..{END}.{M_last}`.
- Source doc referenced in header Exploration source / Read-first.
- Locked decisions delta: `{count}` new locks appended OR `none`.
- Invariants flagged by number + which new stages they gate.
- Cardinality gate: resolved splits / justifications captured.
- Duplication gate: clean OR `{count}` near-duplicates resolved (renamed / merged / dropped).
- Retired-surface warnings (if detected in Phase 0): list legacy blocks (`### Step N`, `**Phases:**`, Phase column) + recommend future migration to canonical shape.
- Next step: `claude-personal "/stage-file {ORCHESTRATOR_SPEC} Stage {START}.{M_first}"` to file the first new stage's pending tasks as BACKLOG rows + project-spec stubs.

**Umbrella flip (if applicable):** If `{ORCHESTRATOR_SPEC}` is a child orchestrator under an umbrella plan (detected by: umbrella's Stage/bucket table references `{slug}-master-plan.md`, OR user provides umbrella path):

- Derive `{UMBRELLA_SLUG}` from umbrella path basename. Call `master_plan_render({slug: UMBRELLA_SLUG})` to fetch umbrella preamble.
- Find child row in umbrella's table where Status = `Planned` or blank.
- Build new umbrella preamble: flip child row Status → `In Progress`. Land via `master_plan_preamble_write({slug: UMBRELLA_SLUG, preamble: <updated>})`.
- Call `master_plan_change_log_append({slug: UMBRELLA_SLUG, kind: "child_extended", body: "{SLUG} extended via {SOURCE_DOC}; status → In Progress."})`.
- Include flip confirmation in handoff message: "Umbrella `{UMBRELLA_SLUG}` — child row `{SLUG}` → In Progress."

---

## Tool recipe (territory-ia) — Phase 2 only

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs:

- `keywords`: English tokens from source-doc Chosen Approach + Subsystem Impact + Architecture component names.
- `brownfield_flag`: `true` for greenfield (new subsystem, no existing code paths touched AND no `Assets/**` paths detected); `false` for brownfield. If source-doc references any `Assets/**` path (even as a future target), treat as brownfield.
- `tooling_only_flag`: `true` for tooling/pipeline-only plans.

Use returned `glossary_anchors` for canonical names in Phase 4; `router_domains` + `spec_sections` for Relevant surfaces; `invariants` for header sync + per-new-stage guardrails.

Also run **`list_specs`** / **`spec_outline`** only if a routed domain references a spec whose sections weren't returned by `domain-context-load`. **Brownfield fallback.**

**Surface-path pre-check (Phase 2 sub-step):** run `surface-path-precheck` subskill on paths from source-doc Architecture / Component map.

---

## Guardrails

- IF `master_plan_render({slug: SLUG})` returns `not_found` → STOP. Route user to `/master-plan-new {SOURCE_DOC}` (fresh orchestrator).
- IF rendered preamble shape check fails (missing header / Stages / legend / guardrails) → STOP. Report malformed orchestrator; do not attempt auto-heal.
- IF `{SOURCE_DOC}` missing expansion + staged skeleton intent → STOP. Route user to `/design-explore {SOURCE_DOC}` first.
- IF `START_STAGE_NUMBER` collides with an existing `N.M` pair → STOP. Overwriting existing Stages requires a fresh revision cycle, not this skill.
- IF proposed new stage duplicates an existing stage name / objective → apply Phase 1 resolution playbook: Draft unpersisted Stage → merge; In Review+ → STOP and ask rename/drop/revision-cycle; near-overlap with distinct scope → proceed with note.
- IF any new Stage has <2 Tasks after Phase 5 → STOP. Ask split or justify before persisting.
- IF any new Stage has 7+ Tasks after Phase 5 → STOP. Suggest split; persist only after user confirms or justifies.
- IF router returns `no_matching_domain` for a new subsystem → note gap in "Relevant surfaces" as `{domain} — no router match; load by path: {file}`, continue.
- IF source doc introduces a locked decision that contradicts an existing Locked decision in `{ORCHESTRATOR_SPEC}` → STOP. Contradictions require explicit re-decision + edit to original exploration doc — not appendable via this skill.
- IF authored new Stage block uses `#### Stage` H4 heading, `**Phases:**` checkbox list, or 6-column Task table with `Phase` column → STOP and re-author. Those are RETIRED surfaces (see MASTER-PLAN-STRUCTURE.md §1).
- Do NOT touch existing Stage rows in `ia_stages` (even to clean retired-surface drift — route user to a separate migration task).
- Do NOT read or write `ia/projects/{slug}-master-plan.md` OR `ia/projects/{slug}/index.md` OR `ia/projects/{slug}/stage-*.md` OR flat `ia/projects/{ISSUE_ID}.md` — DB is sole source of truth post Step 9.6.
- Do NOT insert BACKLOG rows. Do NOT create flat `ia/projects/{ISSUE_ID}.md` specs. Tasks stay `_pending_` — `stage-file` materializes them later.
- Do NOT delete or rename `{SOURCE_DOC}`. Do NOT edit its expansion / extensions block.
- Do NOT commit — user decides when.

---

## Seed prompt

```markdown
Run the master-plan-extend workflow against {ORCHESTRATOR_SPEC} using {SOURCE_DOC}.

Follow ia/skills/master-plan-extend/SKILL.md end-to-end. Inputs:
  ORCHESTRATOR_SPEC: {path to existing master plan}
  SOURCE_DOC: {path to exploration or extensions doc}
  SOURCE_SECTION: {optional — bucket/section heading if SOURCE_DOC is multi-bucket umbrella}
  START_STAGE_NUMBER: {optional N.M override, else inferred}
  SCOPE_BOUNDARY_DOC: {optional sibling doc}

Canonical master-plan shape: docs/MASTER-PLAN-STRUCTURE.md (file shape, Stage block, 5-col Task table, Status enums). 2-level hierarchy Stage > Task (no Steps, no Phases). Phase 0 validates orchestrator shape; inserts missing Last-updated / Locked-decisions / Hierarchy-rules header fields (no STOP); loads only SOURCE_SECTION from SOURCE_DOC if provided; soft-warns on retired-surface drift (Step / Phases / Phase-col) in existing blocks but does NOT auto-migrate. Phase 1 computes start N.M + runs duplication gate (playbook: Draft unpersisted → merge; In Review+ → STOP; distinct scope → note). Phase 2 Tool recipe uses territory-ia MCP slices (greenfield skips router / spec_sections / invariants_summary UNLESS Assets/** paths detected); no full spec reads. Phase 3 emits planned-stages digest — do NOT pause; proceed to Phase 4 immediately. Phase 4 fully decomposes every new Stage (5-col Task table + 4 pending subsections §Stage File Plan / §Plan Fix / §Stage Audit / §Stage Closeout Plan). Phase 5 cardinality gate: ≥2 tasks per Stage AND ≤6 tasks per Stage — pause on violation. Phase 6 persists in place — header sync idempotent; insert new Stage blocks before final `---` + `## Orchestration guardrails`; NEVER touch existing Stages. Phase 6c R6 demotes top Status Final → In Progress when new Stages appended. Phase 7 handoff includes umbrella child-row flip if applicable.
```

---

## Next step

After persist: recommend first new stage to file.

`claude-personal "/stage-file {ORCHESTRATOR_SPEC} Stage {START}.{M_first}"` — new stages are already fully decomposed; file in order.

---

## Changelog

### 2026-04-24 — lifecycle-refactor alignment

**source:** canonical-structure consolidation (MASTER-PLAN-STRUCTURE.md authored)

**deviation:** skill described 3-level Step > Stage > Phase > Task hierarchy. Extended plans by appending `### Step {N}` blocks with H4 Stages, `**Phases:**` checkbox list, 6-column Task table carrying `Phase` column. Per post-lifecycle-refactor 2-level hierarchy (`ia/rules/project-hierarchy.md`), canonical shape is H3 Stages with 5-column Task table (no Phase column). Rewrote Phase 0 (retired-surface soft-warn + Hierarchy-rules header-repair), Phase 1 (compute `START_STAGE_NUMBER` N.M instead of `START_STEP_NUMBER`; duplication gate now Draft unpersisted / In Review+ branches), collapsed Phase 4 (Step decomposition) + Phase 5 (Stage decomposition) → single Phase 4 (Stage decomposition directly from Implementation Points; 4 pending subsections incl. §Stage Audit), Phase 6c R6 flip now `Final → In Progress — Stage {N_first_new}.{M_first_new}`. Cite `docs/MASTER-PLAN-STRUCTURE.md` as authoritative shape source.

### 2026-04-17 — 6-gap audit patches (release-rollout bootstrap)

**Status:** applied — `9822c08`

**Scope:** Gaps surfaced during `full-game-mvp` rollout dry-run. Six fixes landed:

**Fix #1 — First-run guardrail (header-repair without STOP).** Phase 0 "Insert-if-missing" sub-step injects absent header fields BEFORE continuing validation.
**Fix #2 — Phase 6 header sync idempotence.** Per-field Grep → Edit-if-exists else inject.
**Fix #3 — Partial section load for multi-bucket source docs.** Added `SOURCE_SECTION` input.
**Fix #4 — Phase 3 re-fire protection (subagent single-shot).** Emits planned-stages digest; does NOT pause.
**Fix #5 — Umbrella row-flip on child extend.** Phase 7 detects umbrella parentage, flips child row, appends Change log.
**Fix #6 — Duplication gate resolution playbook.** Three-branch playbook (merge / STOP / distinct-scope).

**Tracker aggregator:** [`docs/full-game-mvp-rollout-tracker.md#skill-iteration-log-aggregator`](../../../docs/full-game-mvp-rollout-tracker.md).

---
