# Master-plan structure — canonical

**Purpose:** single source of truth for every `ia/projects/*-master-plan.md` orchestrator. Defines the file shape, Stage block shape, Task table shape, Status enum, lifecycle flip matrix, and cardinality rules. Every skill that authors, extends, decomposes, files, or closes a Stage cites this doc.

**Scope:** orchestrator docs only (`*-master-plan.md`). Per-issue project specs (`ia/projects/{ISSUE_ID}.md`) follow `ia/templates/project-spec-template.md` + `ia/rules/plan-apply-pair-contract.md` — NOT this doc.

**Authority:** this file + [`ia/rules/project-hierarchy.md`](../rules/project-hierarchy.md) are authoritative. [`ia/templates/master-plan-template.md`](../templates/master-plan-template.md) is the seed fixture — conforms to this doc. Any skill with inline structure definition is DRIFT; must defer here.

---

## 1. Hierarchy — two levels, flat

```
master plan (file)
└── Stage N.M           (### H3 heading — shippable compilable increment)
    └── Task            (row in Stage Tasks table — 1 BACKLOG id + 1 spec)
```

**Removed (post lifecycle-refactor — do NOT reintroduce):**

- `### Step N` headings — deleted. Stages are flat siblings under `## Stages`.
- `#### Stage N.M` (H4) — Stages are H3, not H4.
- `**Phases:**` checkbox blocks inside Stage — deleted. One Stage = one atomic shippable unit, decomposed directly into Tasks.
- `Phase` column in Task table — deleted. Table is 5 columns, not 6.
- Stage skeletons / "decomposition deferred" blocks — `ship-plan` decomposes ALL Stages at author time via `master_plan_bundle_apply`. Lazy-decompose fires only when a new Stage skeleton is intentionally version-bumped by `ship-plan --version-bump` with a deferred marker — expanded in place during next `ship-plan` pass.

---

## 2. File canonical shape

### 2.1 Filename + placement

```
ia/projects/{slug}-master-plan.md
```

`{slug}` = kebab-case domain handle (e.g. `sprite-gen`, `grid-asset-visual-registry`). Never numbered — numbers belong to Stages, not the file.

### 2.2 Top-level headings (required, in order)

```markdown
# {Title} — Master Plan ({SCOPE_LABEL})

> **Last updated:** YYYY-MM-DD
> **Status:** {Draft | In Review | In Progress — Stage N.M / TECH-XX | Final}
> **Scope:** {one-line scope}
> **Exploration source:** `{DOC_PATH}`
> **Locked decisions (do not reopen):** {bulleted list}
> **Sibling orchestrators in flight:** {optional; parallel-work rule}
> **Hierarchy rules:** `docs/MASTER-PLAN-STRUCTURE.md` (this doc; Stage > Task 2-level) · `ia/rules/orchestrator-vs-spec.md` · `ia/rules/plan-apply-pair-contract.md`
> **Read first if landing cold:** {bulleted list}

---

## Stages

> **Tracking legend:** {Stage / Task status flip matrix summary — see §6 below}

### Stage 1.1 — {Stage Name}
...

### Stage 1.2 — {Stage Name}
...

---

## Orchestration guardrails

**Do:** {bulleted list}

**Do not:** {bulleted list}
```

### 2.3 Header field rules

| Field | Type | Rules |
|-------|------|-------|
| H1 title | string | `# {Title} — Master Plan ({SCOPE_LABEL})`. `SCOPE_LABEL` = bracketed tag (`MVP`, `IA Infrastructure`, `Post-MVP Extension`, etc.). |
| `Last updated` | date | ISO `YYYY-MM-DD`. Updated by every skill that mutates the file. |
| `Status` | enum | See §6.1. Flipped by lifecycle skills, never by hand. |
| `Scope` | sentence | Chosen Approach + Non-scope boundary. Reference scope-boundary doc when present. |
| `Exploration source` | path list | Relative paths under `docs/` or `ia/`. Ground-truth link. |
| `Locked decisions` | bullets | MVP scope locks / architecture locks lifted from exploration. Do NOT reopen in Stage-level work. |
| `Sibling orchestrators` | optional | Shared-branch collisions; parallel-work rule (no concurrent `/stage-file` or `/ship-stage` on siblings). |
| `Hierarchy rules` | path list | MUST cite this doc first, then `orchestrator-vs-spec.md` + `plan-apply-pair-contract.md`. |
| `Read first if landing cold` | bullets | 4–6 entries. Must include MCP-first directive + invariant refs flagged by `invariants_summary`. |

---

## 3. Stage block canonical shape

Every Stage block under `## Stages` is an H3 heading + the subsection sequence below. Order is fixed.

### 3.1 Heading

```markdown
### Stage N.M — {Stage Name}
```

- `N` = major increment id (1, 2, 3…).
- `M` = minor increment id within `N` (1, 2, 3…). `N.M` pair uniquely identifies the Stage.
- `Stage Name` = ≤8 words, noun-phrase, describes the shippable outcome.

### 3.2 Required subsections (in order)

```markdown
**Status:** {Draft | In Review | In Progress | Final}

**Notes:** {one-line context — e.g. "tasks _pending_ — not yet filed"}

**Backlog state (Stage N.M):** {X filed}

**Objectives:** {2–4 sentences — what this Stage lands + why}

**Exit criteria:**
- {concrete observable outcome 1}
- {outcome 2}

**Art:** {None / list from Design Expansion}

**Arch surfaces:** {comma-separated `arch_surfaces.slug` list — e.g. `layers/system-layers, decisions/dec-a12` — OR literal `none` for cross-cutting tooling Stages}

**Relevant surfaces (load when stage opens):**
- {exploration doc refs + sections}
- {MCP-routed spec refs}
- {invariant numbers}
- {prior stage surfaces}
- {code paths — mark `(new)` for non-existent}

**§Tracer Slice (Stage 1.0 ONLY — mandatory per §3.5):**
- `verb:` {one player/agent verb}
- `hardcoded_scope:` {list}
- `stubbed_systems:` {list}
- `throwaway:` {list}
- `forward_living:` {list}

**§Visibility Delta (Stages 2+ ONLY — mandatory per §3.6):** {single sentence — what player/agent sees/feels new this stage; unique within plan}

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T{N}.{M}.1 | {short name ≤6 words} | _pending_ | _pending_ | {≤2 sentences — concrete deliverable} |

#### §Stage File Plan
_pending — populated by `/stage-file` planner pass._

#### §Plan Fix
_pending — populated by `/plan-review` when fixes are needed._
```

> **Closeout subsection removed.** Stage closeout is no longer authored as a `#### §Stage Closeout Plan` subsection in the master plan. Closeout fires inline in `/ship-stage` Pass B via the `stage_closeout_apply` MCP — single call applies shared migration tuples + N archive ops (`ia_tasks.archived_at`) + N status flips + N id-purge ops. Legacy `§Stage Audit` subsection has also been retired from the master plan (opus-auditor was dropped from `/ship-stage` Pass B per `3ac2d6e`).

### 3.3 Task table — written schema (5 columns, no Phase column)

| Column | Required | Type / format | Filled by | Rules |
|--------|----------|---------------|-----------|-------|
| `Task` | yes | string `T{N}.{M}.{K}` | `ship-plan` `master_plan_bundle_apply` tx | Hierarchical id `T{STAGE_N}.{STAGE_M}.{TASK_K}`. Monotonic within Stage. Never renumbered after filing. |
| `Name` | yes | string ≤6 words | Author | Short handle. Also used as BACKLOG row title + project-spec file name hint. |
| `Issue` | yes | `_pending_` OR `**{PREFIX}-NNN**` | `_pending_` at author time; `ship-plan` `master_plan_bundle_apply` tx fills with `**TECH-NNN**` (or `BUG-`, `FEAT-`, `ART-`, `AUDIO-`). Bold formatting required. |
| `Status` | yes | enum | `ship-plan` tx / `ship-plan` §Plan Digest pass / `spec-implementer` / `/ship-stage` Pass B | `_pending_ → Draft → In Review → In Progress → Done (archived)`. See §6.2. |
| `Intent` | yes | string ≤2 sentences | Author | Concrete deliverable — cite types / methods / file paths. Avoid vague verbs (`add support for X`, `improve Y`). |

**Column order is fixed.** Do NOT insert extra columns (Priority, Owner, Phase, etc.). Per-Task Priority lives in the BACKLOG yaml, not the master plan table.

#### 3.3a Stage-level `arch_surfaces[]` (DEC-A12)

Stage-level architecture-surface declaration — links each Stage to ≥1 `arch_surfaces.slug` row through the `stage_arch_surfaces` join table. Cross-cutting tooling Stages (build scripts, validators with no surface anchor) declare literal `none`. Backfill driver: `npm run backfill:arch-surfaces`. Lint gate: `npm run validate:arch-coherence`.

| Field | Type | Validation | FK semantics | Empty marker | Required |
|---|---|---|---|---|---|
| `arch_surfaces` | `text[]` (JSON-side) → `stage_arch_surfaces` rows DB-side | each entry MUST exist in `arch_surfaces.slug` (Invariant #12 — no auto-create) | composite FK `(slug, stage_id)` → `ia_stages`; `surface_slug` → `arch_surfaces.slug` | literal `none` (whole-Stage marker, not array element) | yes |

**Example:**

```markdown
**Arch surfaces:** layers/system-layers, decisions/dec-a12
```

**MCP write:** `stage_insert(slug, stage_id, ..., arch_surfaces: ["layers/system-layers", "decisions/dec-a12"])`. Unknown slug → `IaDbValidationError: unknown arch_surfaces slugs: ... — Invariant #12 forbids auto-create`.

**MCP read:** `stage_render` / `master_plan_render` emit `arch_surfaces: string[]` per Stage block (sorted ascending by slug for stable downstream output).


**Cardinality:**

- **Hard ≥2 Tasks per Stage.** Single-task Stage requires a Decision Log waiver in master-plan header `Locked decisions` block.
- **Soft ≤6 Tasks per Stage.** Split at ≥7 — large Stages usually hide nested grouping.

### 3.4 Stage subsections — purpose + ordering

The two `####` subsections under every Stage are lifecycle pair-seam anchors. Order is canonical (below). Empty subsections carry a `_pending — populated by {skill} {when}_` sentinel line.

| # | Subsection | Pair-head (Opus) | Pair-tail (Sonnet) | Purpose |
|---|-----------|------------------|--------------------|---------|
| 1 | `#### §Stage File Plan` | `stage-file` planner pass | `stage-file` applier pass | Seam #2 (`plan-apply-pair-contract.md`). Reserves ids + materializes `ia_tasks` rows + body stubs for every pending Task in the Stage. |
| 2 | `#### §Plan Fix` | `plan-reviewer-mechanical` + `plan-reviewer-semantic` | `plan-applier` Mode plan-fix | Seam #1. Stage-wide drift scan after `stage-file` applier pass; emits targeted fix tuples before first `/implement`. |

Retired variants (**do NOT reintroduce**): `#### §Stage Audit` subsection (opus-auditor pass dropped from `/ship-stage` Pass B per `3ac2d6e`); `#### §Stage Closeout Plan` subsection (collapsed into `/ship-stage` Pass B inline closeout via `stage_closeout_apply` MCP — single call applies shared migration tuples + N archive ops + N status flips + N id-purge ops); per-Task `§Closeout Plan` inside project specs (collapsed into Pass B inline closeout). Closeout is no longer a pair seam.

### 3.5 §Tracer Slice subsection — Stage 1.0 only (MANDATORY)

Anchor: `#tracer-slice`. Source contract: `docs/prototype-first-methodology-design.md §6 D9`. Sourced upstream from `design-explore` §Core Prototype block (per D10 / D11 — mechanical 1:1 mapping, no invention at master-plan time).

**Applies to Stage 1.0 only.** Every other Stage MUST omit §Tracer Slice and instead carry §Visibility Delta (§3.6).

**5 fields, all non-empty (validator gate):**

| Field | Type | Meaning |
|---|---|---|
| `verb` | string | What the player/agent can DO at end of Stage 1.0. One verb-phrase, free-form, non-empty (per D2). |
| `hardcoded_scope` | list | Hardcoded data / scenes / configs accepted as Stage 1.0 input (per D4). |
| `stubbed_systems` | list | Stub methods returning constants — peripheral systems not yet real (per D4). |
| `throwaway` | list | Visible-layer items acceptable for Stage 2+ rewrite (per D7). |
| `forward_living` | list | Structural-layer items locked forward — survive past Stage 1.0 unchanged (per D7). |

**Empty / missing field → `validate:plan-prototype-first` CI red (Stage 1.3 deliverable).**

**Example:**

```markdown
**§Tracer Slice (Stage 1.0 ONLY — mandatory per §3.5):**
- `verb:` player can place a road tile on grid and see it render
- `hardcoded_scope:` single 8x8 grid scene; one road sprite; no terrain variation
- `stubbed_systems:` GridManager.GetCellCost returns constant 1; HeightMap returns flat 0
- `throwaway:` road sprite (replaced by atlas Stage 2.x); 8x8 grid (replaced by 64x64)
- `forward_living:` GridManager.PlaceTile API; ITileRenderer interface
```

### 3.6 §Visibility Delta line — Stages 2+ only (MANDATORY)

Anchor: `#visibility-delta`. Source contract: `docs/prototype-first-methodology-design.md §6 D9`. Sourced upstream from `design-explore` §Iteration Roadmap rows (per D10 / D11 — one row → one Stage delta line).

**Applies to every Stage with `N.M ≠ 1.0`.** Stage 1.0 MUST omit §Visibility Delta and instead carry §Tracer Slice (§3.5).

**Single sentence, non-empty, unique within plan (validator gate):**

- One-line statement answering "what does the player/agent see/feel that they didn't before this stage?" (per D9).
- Free-form prose; non-empty.
- MUST be unique across all Stages within one master plan (no two Stages claim same delta — `validate:plan-prototype-first` CI red on duplicate or empty per D9).
- Plumbing-only Stages forbidden post Stage 1.0 — every Stage MUST declare its player-visible delta or be merged into one that does (per D8).

**Example:**

```markdown
**§Visibility Delta (Stages 2+ ONLY — mandatory per §3.6):** Player sees road tiles snap to grid corners and animate in-place when adjacent tiles update.
```

### 3.7 §Red-Stage Proof block — Stages 2+ (MANDATORY per `ia/rules/tdd-red-green-methodology.md`)

Anchor: `#red-stage-proof`. Source contract: [`ia/rules/tdd-red-green-methodology.md`](../rules/tdd-red-green-methodology.md). Every Stage of every non-grandfathered master plan MUST carry a §Red-Stage Proof block with all 4 fields filled. Validator gate: `validate:plan-red-stage` CI red on any missing or empty block.

**4 required fields:**

| Field | Type | Purpose | Example |
|-------|------|---------|---------|
| `red_test_anchor` | anchor grammar string | Points to the test method that was **red** (failing) before implementation. Parsed by `tools/lib/red-stage-anchor-resolver.ts`. | `tracer-verb-test:tools/scripts/test/validate-plan-red-stage.test.mjs::CIRedOnEmptyRedStageProofBlock` |
| `target_kind` | enum | Category of the red test. See enum below. | `tracer_verb` |
| `proof_artifact_id` | path or `n/a` | Repo-relative path to the test file containing the red test. `n/a` allowed only when `target_kind=design_only`. | `tools/scripts/test/validate-plan-red-stage.test.mjs` |
| `proof_status` | enum | Current status of the red test. See enum below. | `failed_as_expected` |

**`target_kind` enum:**

| Value | Meaning |
|-------|---------|
| `tracer_verb` | Stage 1.0 tracer-verb test — proves the player-visible verb fires end-to-end. |
| `visibility_delta` | Stages 2+ visibility-delta test — proves the new player-visible surface is absent pre-impl. |
| `bug_repro` | Regression test reproducing a filed BUG-NNNN before the fix. |
| `design_only` | Stage is purely design / doc work with no code surface to test. `proof_artifact_id=n/a` allowed. |

**`proof_status` enum:**

| Value | Meaning |
|-------|---------|
| `pending` | Test not yet written or run. |
| `failed_as_expected` | Test runs red before implementation — proof exists. |
| `unexpected_pass` | Test passed before implementation (unexpected); escalate for review. |
| `not_applicable` | No testable surface (`target_kind=design_only`). |

**Anchor grammar — 4 forms:**

| Grammar form | Example | `target_kind` it pairs with |
|---|---|---|
| `tracer-verb-test:{path}::{method}` | `tracer-verb-test:tools/scripts/__tests__/validate-plan-red-stage.test.mjs::greenPlanExitsZero` | `tracer_verb` |
| `visibility-delta-test:{path}::{method}` | `visibility-delta-test:tools/scripts/__tests__/validate-plan-red-stage.test.mjs::CIRedOnEmptyRedStageProofBlock` | `visibility_delta` |
| `BUG-NNNN:{path}::{method}` | `BUG-3210:Assets/Tests/EditMode/Economy/SomeReproTest.cs::ReproducesNegativeBalance` | `bug_repro` |
| `n/a` | `n/a` | `design_only` |

**Skip-clause:** Stages with `target_kind=design_only` may set `proof_artifact_id=n/a` and `proof_status=not_applicable` — `validate:plan-red-stage` exits 0 for these Stages.

**Grandfathering:** master plans with `created_at < 2026-05-03` are skipped silently (warn-only). Plans on or after the cutover date are fully enforced.

**Empty / missing block → `validate:plan-red-stage` CI red.**

**Example:**

```markdown
**§Red-Stage Proof:**
- `red_test_anchor:` visibility-delta-test:tools/scripts/__tests__/validate-plan-red-stage.test.mjs::CIRedOnEmptyRedStageProofBlock
- `target_kind:` visibility_delta
- `proof_artifact_id:` tools/scripts/__tests__/validate-plan-red-stage.test.mjs
- `proof_status:` failed_as_expected
```

---

## 4. Section ordering — full master plan

```
1. H1 title + header block (§2.2)
2. --- separator
3. ## Stages  (umbrella H2 — single occurrence)
4. ### Stage 1.1 — ...  (H3 — repeat per Stage)
   4.1 Status / Notes / Backlog state / Objectives / Exit criteria / Art / Arch surfaces / Relevant surfaces
   4.2 §Tracer Slice (Stage 1.0 ONLY — §3.5) XOR §Visibility Delta (Stages 2+ ONLY — §3.6)
   4.3 Tasks table
   4.4 #### §Stage File Plan
   4.5 #### §Plan Fix
5. --- separator (after last Stage)
6. ## Orchestration guardrails  (H2 — single occurrence, terminal)
```

No other H2 headings are permitted between `## Stages` and `## Orchestration guardrails`. No Step H3 / Phase H4 ever. Appendices (Decision Log, Open Questions) move into `docs/` or the exploration doc — not the master plan body.

---

## 5. Cardinality gate (hard vs soft)

| Gate | Rule | Enforced by |
|------|------|-------------|
| Hard | ≥2 Tasks per Stage | `ship-plan` lean YAML cardinality gate · `design-explore` Phase 4 task list gate |
| Soft | ≤6 Tasks per Stage | Same skills; warn + recommend split, don't block |
| Hard | ≥1 Stage per master plan | `ship-plan` Phase 1 |
| Hard | Every Stage has `#### §Stage File Plan` + `#### §Plan Fix` subsections (sentinel or populated) | `ship-plan` `master_plan_bundle_apply` tx |
| Hard | Task table has exactly 5 columns (`Task | Name | Issue | Status | Intent`) | Same + `stage-file` planner pass parser |

---

## 6. Status enum

### 6.1 Master-plan header `Status`

```
Draft | In Review | In Progress — Stage N.M / TECH-XX | Final
```

| State | Meaning | Flip trigger |
|-------|---------|--------------|
| `Draft` | Initial — no Task filed yet. | `ship-plan` `master_plan_bundle_apply` tx writes `Draft`. |
| `In Review` | Mid-authoring — `ship-plan --version-bump` re-author pass. | `ship-plan --version-bump` Phase 7 (temporary). |
| `In Progress — Stage N.M / TECH-XX` | ≥1 Task filed; plan actively worked. | `ship-plan` `master_plan_bundle_apply` tx R1 flips on first Task ever filed. |
| `Final` | All Stages are `Final`. | `/ship-stage` Pass B inline closeout R5 on last Stage close. |

`ship-plan --version-bump` R6 demotes `Final → In Progress` when new Stages appended to a Final plan.

### 6.2 Stage header `Status`

```
Draft | In Review | In Progress | Final
```

| State | Meaning | Flip trigger |
|-------|---------|--------------|
| `Draft` | Authored, no Task filed. | `ship-plan` `master_plan_bundle_apply` tx writes `Draft`. |
| `In Review` | Post-`plan-review` drift scan pending fix-apply. | `plan-reviewer-mechanical` + `plan-reviewer-semantic` write `In Review` when `§Plan Fix` non-empty. |
| `In Progress` | ≥1 Task filed in this Stage. | `ship-plan` `master_plan_bundle_apply` tx R2 flips on first Task filed in the Stage. |
| `Final` | Every Task row in Stage = `Done`; closeout applied. | `/ship-stage` Pass B inline closeout R3 on last Task archived. |

> **Footnote — prototype-first gate:** Stage 1.0 §Tracer Slice (§3.5) and Stages 2+ §Visibility Delta (§3.6) are mandatory subsections. The dedicated CI gate is `validate:plan-prototype-first` — it asserts (a) Stage 1.0 carries §Tracer Slice with all 5 fields non-empty, (b) every Stage 2+ carries a non-empty §Visibility Delta line, (c) §Visibility Delta lines are unique within a plan. Validator script lands in **Stage 1.3** of `prototype-first-methodology` master plan; until then, presence is contract-only (manual review). Existing 17 pending plans grandfathered until per-plan retrofit.

### 6.3 Task row `Status`

```
_pending_ → Draft → In Review → In Progress → Done (archived)
```

| State | Meaning | Flip trigger |
|-------|---------|--------------|
| `_pending_` | Not yet filed — no `ia_tasks` row. | `ship-plan` lean YAML task list seeds at author time. |
| `Draft` | `ia_tasks` row + body stub exist; §Plan Digest not yet written. | `ship-plan` `master_plan_bundle_apply` tx flips on row materialization. |
| `In Review` | §Plan Digest written into Task body. | `ship-plan` §Plan Digest bulk pass. |
| `In Progress` | `/implement` dispatched. | `spec-implementer` Phase 0. |
| `Done (archived)` | `ia_tasks.archived_at` set; spec deleted. | `/ship-stage` Pass B inline closeout per-Task archive op. |

**Retired values (do NOT reintroduce):** `Skeleton`, `Planned`. Both replaced by `_pending_` + `Draft` post lifecycle-refactor.

---

## 7. Lifecycle skill flip matrix

One-line binding from skill → structural surface it mutates. Every authoring skill MUST cite this doc as hierarchy authority.

| Skill | Reads | Writes | Section authority |
|-------|-------|--------|------------------|
| `design-explore` Phase 4 | Exploration doc §Design Expansion + decision answers | Lean YAML frontmatter seed (`slug`, `stages[]`, `tasks[]`) handed to `ship-plan` | §2 (slug/version model), §3 (Stage skeleton) |
| `ship-plan` `master_plan_bundle_apply` tx | Lean YAML seed + (on `--version-bump`) existing master plan | New master plan file or version-bumped file; all Stages + Tasks `_pending_`; `ia_master_plans` + `ia_tasks` rows | §2 (file shape), §3 (Stage block), §6 (Status `Draft`) |
| `ship-plan` §Plan Digest bulk pass | Task spec stubs (post-materialization) | `§Plan Digest` in each Task body via `task_spec_section_write` MCP | Task status `Draft → In Review` (§6.3) |
| `plan-reviewer-mechanical` + `plan-reviewer-semantic` | Stage + all Task specs | `#### §Plan Fix` tuples | §3.4 subsection #2 |
| `plan-applier` Mode plan-fix | `§Plan Fix` tuples | Edits Task specs verbatim | — |
| `ship-cycle` Pass A | Task body §Plan Digest (per Stage batch) | Source code + Task body; per-task `unity:compile-check` + `task_status_flip(implemented)` | Task status `In Review → In Progress` (§6.3) |
| `opus-code-reviewer` | Task diff vs spec | Task body §Code Review / `§Code Fix Plan` | — (intra-spec) |
| `plan-applier` Mode code-fix | `§Code Fix Plan` tuples | Edits source code per tuples | — |
| `/ship-stage` Pass B inline closeout (`stage_closeout_apply` MCP) | Stage block + N filed Task bodies | Sets `ia_tasks.archived_at`; deletes specs from filesystem mirror; flips Task rows `Done (archived)`; Stage `In Progress → Final` (R3); master plan `In Progress → Final` (R5); shared migration ops + N id-purge ops | §6.1 R5, §6.2 R3, §6.3 `Done (archived)` |
| `ship-stage` | Stage block | Two-pass orchestrator — Pass A (`ship-cycle`) per-Stage batch implement; Pass B per-Stage verify-loop + code-review + inline closeout + single stage commit | — (chain) |
| `ship-final` | All Stages Final; seeded-count=0; `test:ia` deferred band | `ia_master_plans.status = Final`; publishes ship-final digest; no per-task flips | §6.1 R5 terminal |

Full seam contract: [`ia/rules/plan-apply-pair-contract.md`](../rules/plan-apply-pair-contract.md). Status flip matrix: [`ia/rules/orchestrator-vs-spec.md`](../rules/orchestrator-vs-spec.md).

---

## 8. Orchestration guardrails — canonical block

Every master plan terminates with the `## Orchestration guardrails` H2. Canonical body:

```markdown
## Orchestration guardrails

**Do:**

- Open one Stage at a time. Next Stage opens only after current Stage closes via `/ship-stage` Pass B.
- Run `/ship-plan {this-doc}` to materialize `_pending_` Tasks via `master_plan_bundle_apply` tx.
- Update Stage + Task `Status` via lifecycle skills — do NOT edit by hand.
- Preserve locked decisions. Changes require explicit re-decision + sync edit to exploration + scope-boundary docs.
- Extend via `/ship-plan --version-bump {this-doc} {source-doc}` — do NOT hand-insert new Stage blocks.

**Do not:**

- Close the orchestrator — orchestrators are permanent (`orchestrator-vs-spec.md`). Stage close fires inline in `/ship-stage` Pass B.
- Silently promote post-MVP items into MVP Stages — they belong in scope-boundary doc.
- Merge partial Stage state — every Stage lands on a green bar.
- Insert `ia_tasks` rows directly into this doc — only `ship-plan` `master_plan_bundle_apply` tx materializes them.
- Hand-insert new Stages past the last persisted `### Stage N.M` block — run `/ship-plan --version-bump`.
```

Plans MAY append plan-specific Do/Do-not bullets but MUST preserve the canonical lines.

---

## 9. Validators

| Validator | What it checks | When it runs |
|-----------|----------------|--------------|
| `npm run validate:master-plan-status` | Header `Status` ↔ Stage `Status` ↔ Task row status ↔ `ia_tasks` row consistency (R1–R6) | CI + post-`/stage-file` + post-`/ship-stage` Pass B |
| `npm run validate:backlog-yaml` | BACKLOG yaml schema (legacy artifacts only) | CI + post-`/stage-file` |
| `npm run validate:dead-project-specs` | Orphan `ia/projects/{ISSUE_ID}.md` filesystem mirrors with no `ia_tasks` row | CI + post-`/ship-stage` Pass B |
| `npm run validate:all` | Aggregate — runs all of the above | CI + every Stage seam closure |

Any structural drift (Step heading, Phase column, H4 Stage, retired `§Stage Audit` / `§Stage Closeout Plan` subsection reintroduced) surfaces as `validate:master-plan-status` non-zero exit.

---

## 10. Cross-references

- [`ia/rules/project-hierarchy.md`](../rules/project-hierarchy.md) — hierarchy table, cardinality rationale, lazy materialization, learnings-flow-backward, ephemeral spec lifecycle.
- [`ia/rules/orchestrator-vs-spec.md`](../rules/orchestrator-vs-spec.md) — orchestrator vs project-spec distinction + full Status flip matrix R1–R6.
- [`ia/rules/plan-apply-pair-contract.md`](../rules/plan-apply-pair-contract.md) — `§Plan` tuple shape + pair seams (plan-review, code-review) + validators + escalation rule + idempotency. Closeout no longer a pair seam (folded into `/ship-stage` Pass B inline). `stage-file` pair seam retired — replaced by `ship-plan master_plan_bundle_apply tx`.
- [`ia/templates/master-plan-template.md`](../templates/master-plan-template.md) — seed fixture consumed by `ship-plan` `master_plan_bundle_apply` tx (conforms to this doc).
- [`ia/templates/project-spec-template.md`](../templates/project-spec-template.md) — per-issue spec shape (NOT master-plan; sibling doc).

## 11. Changelog

### 2026-05-05 — ship-protocol Stage 5: 4-skill pipeline retirement migration

Retired `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file`, `stage-authoring` skill dirs (git-rm). Updated all authoring references to new pipeline: `design-explore → ship-plan → ship-cycle → ship-final`. Changes:

- **§1 Removed block** — updated stage-skeleton note: `master-plan-new` → `ship-plan`; `master-plan-extend` → `ship-plan --version-bump`; `stage-decompose` → inline expansion during next `ship-plan` pass.
- **§6 Status flip owners** — `stage-file applier pass` → `ship-plan master_plan_bundle_apply tx` (R1, R2); `stage-authoring bulk pass` → `ship-plan §Plan Digest bulk pass`; added `ship-final` terminal row.
- **§7 lifecycle skill flip matrix** — replaced `master-plan-new`, `master-plan-extend`, `stage-decompose`, `stage-file planner/applier pass`, `stage-authoring` rows with `design-explore Phase 4`, `ship-plan master_plan_bundle_apply tx`, `ship-plan §Plan Digest bulk pass`, `ship-cycle Pass A`, `ship-final` rows.
- **§8 orchestration guardrails** — `/stage-file` → `/ship-plan`; `/master-plan-extend` → `/ship-plan --version-bump`; `stage-file applier pass` → `ship-plan master_plan_bundle_apply tx`.
- **§10 cross-references** — template seed note: `master-plan-new` → `ship-plan master_plan_bundle_apply tx`.

### 2026-04-25 — DB-primary refactor + skill-files-audit retirement scrub

Major rewrite reflecting DB-primary refactor (Postgres `ia_*` schema source of truth post Step 6/9.x) + Phase A retirement scrub. Changes:

- **§3.2 sentinel block** — removed `#### §Stage Audit` and `#### §Stage Closeout Plan` sentinel subsections; explanatory note added.
- **§3.3 Task table** — `stage-file-apply` → `stage-file applier pass`; closeout owner → `/ship-stage Pass B inline closeout`.
- **§3.4 pair table** — reduced from 4 rows to 2 (kept §Stage File Plan + §Plan Fix; dropped §Stage Audit + §Stage Closeout Plan).
- **§4 section ordering** — removed 4.4 §Stage Audit + 4.5 §Stage Closeout Plan.
- **§5 cardinality** — Stage subsection requirement reduced to §Stage File Plan + §Plan Fix.
- **§6 Status flip owners** — `stage-file applier pass` (R1, R2), `stage-authoring bulk pass` (Task In Review), `/ship-stage Pass B inline closeout` (R3, R5, Task Done archive).
- **§7 lifecycle skill flip matrix** — collapsed plan-author + plan-digest → `stage-authoring`; collapsed stage-file-plan + stage-file-apply → `stage-file planner pass + applier pass`; dropped stage-closeout-plan + plan-applier Mode stage-closeout rows; added single `/ship-stage Pass B inline closeout (stage_closeout_apply MCP)` row; renamed `plan-review` → `plan-reviewer-mechanical + plan-reviewer-semantic`; added `plan-applier Mode code-fix` row.
- **§8 guardrails** — orchestrators permanent; Stage close fires inline in `/ship-stage` Pass B; `BACKLOG rows` → `ia_tasks rows`.
- **§9 validators** — `validate:dead-project-specs` checks orphan filesystem mirrors; structural drift includes retired §Stage Audit / §Stage Closeout Plan reintroduction.
- **§10 cross-ref** — `4 pair seams` → `3 pair seams (plan-review, stage-file, code-review)`; closeout no longer a pair seam.

### 2026-04-24 — Initial

First canonical master-plan structure doc. Consolidates shape rules previously scattered across `master-plan-template.md` HTML comments + `project-hierarchy.md` table + inline skill definitions in `master-plan-new` / `master-plan-extend` / `stage-decompose` / `stage-file-plan` / `stage-file-apply` / `ship-stage`. Adds written schema for 5-column Task table (previously only in template HTML comment). Adds mandatory `§Stage Audit` subsection in Stage block (previously convention-only in some plans). Removes Steps + Phases + Phase column + H4 Stages (all retired post lifecycle-refactor).
