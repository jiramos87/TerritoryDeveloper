---
purpose: "Extend an existing `ia/projects/{slug}-master-plan.md` with new Steps sourced from an exploration or extensions doc. Appends — never rewrites existing Steps."
audience: agent
loaded_by: skill:master-plan-extend
slices_via: router_for_task, spec_sections, invariants_summary, glossary_discover, glossary_lookup
name: master-plan-extend
description: >
  Use when an existing master plan orchestrator needs new Steps sourced from an exploration doc (with
  persisted `## Design Expansion`) OR an extensions doc (e.g. `{slug}-post-mvp-extensions.md`) that was
  deferred at original author time. Appends new Step blocks in place — never rewrites existing Steps,
  never overwrites headers, never inserts BACKLOG rows. Fully decomposes every new Step (stages → phases
  → tasks) at author time — no skeletons. Applies `stage-decompose` Phase 2 rules per new step. Triggers:
  "/master-plan-extend {plan} {source}", "extend master plan from exploration", "add new steps to
  orchestrator", "append from extensions doc", "pull deferred step into master plan".
---

# Master plan — extend orchestrator with new steps from exploration / extensions doc

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: Objectives 2–4 sentences (human-consumed cold); Exit criteria carry type/method/file precision; Relevant surfaces stay explicit (paths + line refs); Mermaid/ASCII verbatim.

No MCP from skill body. Tool recipe Phase 2 only. All other phases derive from the source doc's expansion / extensions block.

**Lifecycle:** AFTER [`master-plan-new`](../master-plan-new/SKILL.md) has authored the orchestrator AND `{SOURCE_DOC}` exists with expansion (or equivalent extensions list). BEFORE [`stage-file`](../stage-file/SKILL.md) of the new stages.
`design-explore` → `master-plan-new` → `master-plan-extend` (this skill) → `stage-file` → `project-new` → `project-spec-kickoff` → `project-spec-implement` → `project-stage-close` (non-final) → `project-spec-close` (umbrella).

**Related:** [`master-plan-new`](../master-plan-new/SKILL.md) · [`stage-decompose`](../stage-decompose/SKILL.md) · [`stage-file`](../stage-file/SKILL.md) · [`ia/rules/project-hierarchy.md`](../../rules/project-hierarchy.md) · [`ia/rules/orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md) · [`ia/skills/README.md`](../README.md).

**Shape refs:** [`blip-master-plan.md`](../../projects/blip-master-plan.md) · [`sprite-gen-master-plan.md`](../../projects/sprite-gen-master-plan.md) · [`ia/templates/master-plan-template.md`](../../templates/master-plan-template.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `ORCHESTRATOR_SPEC` | User prompt | Path to existing `ia/projects/{slug}-master-plan.md` — required. Must exist. Must match orchestrator shape (header block + `## Steps` + tracking legend + `## Orchestration guardrails`). |
| `SOURCE_DOC` | User prompt | Path to exploration doc (carries `## Design Expansion` or semantic equivalent) OR extensions doc (`{slug}-post-mvp-extensions.md` listing deferred Steps) — required. Must carry at least one Implementation Point / Roadmap entry not already represented in orchestrator. |
| `SOURCE_SECTION` | User prompt | Optional. When `SOURCE_DOC` is an umbrella multi-bucket exploration (e.g. `full-game-mvp-exploration.md`), specify the bucket heading or section slug (e.g. `Bucket 7 — Audio polish & Blip`). Phase 0 + Phase 2 load only that subsection + its Implementation Points block; remaining buckets ignored to prevent token blow-up and wrong-bucket bleed. |
| `START_STEP_NUMBER` | User prompt | Optional integer override. Default: `last_existing_step_number + 1`. Extend appends — never overwrites existing Steps. |
| `SCOPE_BOUNDARY_DOC` | User prompt | Optional sibling doc listing out-of-scope items. Referenced in per-new-step Relevant surfaces if not already in orchestrator header. |

---

## Phase sequence (gated — each phase depends on the previous)

### Phase 0 — Load + validate

Read `{ORCHESTRATOR_SPEC}`. If `SOURCE_SECTION` provided, note it for Phase 2 scoping (full doc still read here for shape check).

**Hard-required (STOP if missing):**

- `## Steps` + tracking legend present (enum `Draft | In Review | In Progress — {active child} | Final`).
- `## Orchestration guardrails` present (Do / Do not lists).
- At least one `### Step {N}` block exists.

**Insert-if-missing (header-repair — do NOT STOP; inject BEFORE continuing validation):**

- `**Last updated:** {YYYY-MM-DD}` absent → insert under the orchestrator title block (before `**Status:**` or first header field); set to today's date.
- `**Locked decisions (do not reopen in this plan):**` absent → insert as empty bullet-list field in header block.

After header-repair, continue. Missing hard-required shape → STOP. Route user to `/master-plan-new {SOURCE_DOC}` (fresh orchestrator) OR hand-migrate to template shape first.

Read `{SOURCE_DOC}`. If `SOURCE_SECTION` provided, load only that section + its Implementation Points / Roadmap sub-block; ignore remaining sections. Confirm expansion intent present — literal `## Design Expansion` OR semantic equivalents:

| Intent | Literal heading | Semantic equivalents accepted |
|---|---|---|
| Approach + rationale | `### Chosen Approach` | `### Decision` / `### MVP scope` / `### Deferred Steps` / `### Extensions` |
| Entry / exit points | `### Architecture` | `### Component map` / `### Key types` / `### Names registry` |
| Touched subsystems + invariant risk | `### Subsystem Impact` | `### Related subsystems` / `### Integration points` |
| Phased skeleton for new steps | `### Implementation Points` | `### Roadmap` / `### Deferred steps list` / `### Extension plan` |

Missing approach + phased skeleton intent → STOP. Extensions docs must carry at least an ordered list of deferred Steps (name + one-line objective) — otherwise route user to `/design-explore {SOURCE_DOC}` first.

Hold in working memory:

- **Existing Step count** (highest `### Step {N}` number in orchestrator).
- **Existing Step names** (for duplication gate Phase 1).
- **Existing Locked decisions** (do-not-reopen list — must not be re-contested by new Steps).
- **Source doc Implementation Points / Roadmap / Deferred list** — raw skeleton for new steps.
- **Source doc Architecture / Component map** — entry / exit points → §"Relevant surfaces" per new step.
- **Source doc Subsystem Impact** — touched subsystems + invariant numbers → per-stage guardrails.
- **Source doc Locked decisions (if any)** — new locks to merge into orchestrator header (Phase 7).

### Phase 1 — Start-number resolution + duplication gate

Compute `START_STEP_NUMBER`:

- User override provided → use it (gate: must be > last existing step number; `=` is an overwrite — STOP and ask).
- No override → `last_existing_step_number + 1`.

**Duplication gate:** for each proposed new step name / objective (from Phase 0 source-doc skeleton), scan existing `### Step {N} — {Name}` blocks. Name collision OR near-identical objective (>50% token overlap) → do NOT STOP outright. Apply resolution playbook:

(a) **Existing Step is Draft** → merge: add the new scope as an additional Stage inside the existing Draft step (extend its stage list). Report merge in Phase 8 handoff under "Duplication resolved — merged into existing Step {N}".
(b) **Existing Step is Final** → STOP. Final steps are immutable. Ask user: drop new step, rename to a distinct scope, OR open a revision cycle.
(c) **Near-overlap but verifiably distinct scope** → proceed with proposed name; note in Phase 8 handoff ("near-overlap with Step {N}; verified distinct scope — {one-line distinction}").

Fail fast — no MCP yet.

### Phase 2 — MCP context (Tool recipe) + surface-path pre-check

Run **Tool recipe** (below) via `domain-context-load` subskill. **Greenfield**: `brownfield_flag = true`. **Brownfield**: `brownfield_flag = false`. **Tooling-only**: `tooling_only_flag = true`. If source-doc references any `Assets/**` path (even future target), treat as brownfield for invariants.

Capture for Phases 4–5:

- `glossary_anchors` → canonical names replace ad-hoc synonyms in authored prose.
- `spec_sections` → §"Relevant surfaces" per new step / stage.
- `invariants` → per-new-stage guardrails + append to orchestrator header "Read first" if not already listed (Phase 7 header-sync).

**Surface-path pre-check** — run `surface-path-precheck` subskill ([`ia/skills/surface-path-precheck/SKILL.md`](../surface-path-precheck/SKILL.md)): pass paths from source-doc Architecture / Component map. Use returned `line_hint` in surfaces; mark `(new)` for `exists: false`.

Skip pre-check → downstream stages cite ghost line numbers.

### Phase 3 — New-step proposal + user confirm

Produce outline (caveman one-liners) per proposed new step BEFORE full decomposition:

```
Step {START} — {Name} — {one-line objective} — {est 2–4 stages}
Step {START+1} — {Name} — {one-line objective} — {est 2–4 stages}
...
```

Emit the planned-steps digest in the return message. Do NOT pause — subagents run single-shot; the gate moves to the caller. Digest must include:

- Ordering rationale (dep chain — earlier phases = earlier steps; flag if source doc implies different chain).
- Names (≤6 words each; confirm no collision with existing Steps per Phase 1).
- Scope boundary (note anything dropped per `SCOPE_BOUNDARY_DOC` or `SOURCE_SECTION` filtering).

After emitting digest, proceed directly to Phase 4. Caller reviews digest and re-fires if changes needed before persisting.

Why this gate exists: full decomposition (Phases 4–6) is expensive (task tables + intents). Digest lets caller catch scope / naming / ordering issues; re-fire with updated scope if unhappy.

### Phase 4 — Step decomposition (new steps only)

Per confirmed new step (ALL of them, starting at `START_STEP_NUMBER`), author the Step block shape matching `ia/templates/master-plan-template.md`:

```markdown
### Step {N} — {Name}

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step {N}):** 0 filed

**Objectives:** {2–4 sentences — what this step lands + why. Ties back to source doc rationale.}

**Exit criteria:**

- {concrete observable outcome 1 — cites type / method / file path where verifiable}
- {outcome 2}
- ...

**Art:** {None / list of art assets needed from source doc; else `None`}.

**Relevant surfaces (load when step opens):**

- {source doc ref + sections}
- {MCP-routed spec section refs (via Phase 2)}
- {invariant numbers from Subsystem Impact}
- {prior step outputs (existing Steps 1..START-1, plus new Steps START..N-1) — surfaces shipped by prior step}
- {code paths — entry/exit points from source doc Architecture; mark `(new)` for non-existent paths per Phase 2 pre-check}
```

**Depth rule:** All new steps fully decomposed (stages → phases → tasks). No skeletons. Reuse Phase 2 MCP output — no additional tool calls per step. New Step {START} cites Step {START-1} (the last existing step) in its Relevant surfaces as the prior-step handoff.

Rules mirror `master-plan-new` Phase 4:

- 1 step = ≥1 user-visible capability OR coherent scaffolding layer.
- Hard dep chain → earlier phases = earlier steps.
- Horizontal expansion → same step, additional stages.
- Combined step count (existing + new) target ≤8; 9+ = flag scope creep (warn, do not block).

### Phase 5 — Stage decomposition (new steps only)

All new steps. Subdivide each into **stages** — each = sub-milestone with own exit criteria. Apply `ia/skills/stage-decompose/SKILL.md` Phase 2 rules; reuse Phase 2 MCP output. Rules:

- Each stage lands on green-bar boundary (compiles, tests pass, no partial state merged).
- Soft deps within step OK; no stage blocks step's close.
- Target 2–4 stages per step.

**Stage-ordering heuristic** (earliest first):

1. **Scaffolding / infrastructure** — bootstrap, persistent bindings, project settings, AudioMixer groups, scene setup. No data model yet.
2. **Data model** — ScriptableObject / blittable struct / serialized fields. Typed but inert.
3. **Runtime logic** — DSP kernel / update loop / compute code. Consumes data model.
4. **Integration + tests** — call sites, EditMode/PlayMode tests, golden fixtures. Lands last in the step.

Follow order unless source doc declares different dep chain.

Per stage, author the block shape (6-column task table — matches `ia/templates/master-plan-template.md`):

```markdown
#### Stage {N}.{M} — {Name}

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** {1–3 sentences — what this stage lands}.

**Exit:**

- {observable outcome 1 — cites type / method / file path}
- {outcome 2}
- {glossary row additions, if canonical terms introduced}

**Phases:**

- [ ] Phase 1 — {shippable increment description}.
- [ ] Phase 2 — {...}.
- [ ] Phase N — {...}.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T{N}.{M}.1 | {short name ≤6 words} | 1 | _pending_ | _pending_ | {≤2 sentences — concrete deliverable: types, methods, file paths. Reference existing patterns where applicable} |
| T{N}.{M}.2 | {short name} | 1 | _pending_ | _pending_ | {...} |
| T{N}.{M}.3 | {short name} | 2 | _pending_ | _pending_ | {...} |
```

**Task intent concreteness bar:** avoid vague verbs ("add support for X", "handle Y"). Instead cite the thing being shipped — types / methods / file paths. Concrete intent survives the wait between authoring + `stage-file` materialization.

**Task sizing heuristic** (same as `master-plan-new` Phase 5):

- **Correct scope:** 2–5 files forming one algorithm layer. Tasks at this size keep `spec_section` reloads to ≤2.
- **Too small (merge):** single file, single function, single constant. Merge with an adjacent same-domain task in the same phase.
- **Too large (split):** touches >3 unrelated subsystems or needs >6 phases. Split at subsystem seam.

### Phase 6 — Cardinality gate

Run `cardinality-gate-check` subskill ([`ia/skills/cardinality-gate-check/SKILL.md`](../cardinality-gate-check/SKILL.md)): pass phase → tasks map for each **new** stage from Phase 5. Scope: **new stages only** — do NOT re-gate existing stages. Cardinality rule (`ia/rules/project-hierarchy.md`): ≥2 tasks/phase (hard), ≤6 soft.

Subskill returns `{phases_lt_2, phases_gt_6, single_file_tasks, oversized_tasks, verdict}`:
- `verdict = pause` → surface violations to user; ask split, merge, or justify. Proceed only after user confirms or fixes. Phrase split/merge question in player/designer-visible outcomes (releasable slices, user-visible checkpoints), not stage numbers or task-count math. Ids / stage numbers go on a trailing `Context:` line. Full rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).
- `verdict = proceed` → continue to Phase 7.

Also covers Phase 5 task sizing: single-file/function/struct tasks → `single_file_tasks`; >3 unrelated subsystems → `oversized_tasks`.

### Phase 7 — Persist in place

Edit `{ORCHESTRATOR_SPEC}`. Operations (in order, atomic — single Write or sequential Edits):

1. **Header sync (idempotent — for each field: Grep → Edit if exists, else inject):**
   - `**Last updated:**` present → update to today's date. Absent → insert line under orchestrator title block (before `**Status:**`). (May already exist after Phase 0 header-repair; skip if already today's date.)
   - `**Exploration source:**` present → append `{SOURCE_DOC}` entry if not already listed, format: `` `{SOURCE_DOC}` (§{new-step-relevant sections}) — extension source for Steps {START}..{END} ``. Absent → insert field + entry.
   - `**Locked decisions (do not reopen in this plan):**` present → append new bullets from source doc. Absent → insert field + bullets. (Field may already exist after Phase 0 header-repair.)
   - `**Read first if landing cold:**` present AND Phase 2 surfaced new invariant numbers → merge them in. Absent → skip (do not add field from this sync alone).
2. **Step block insertion:**
   - Locate insertion point: last `### Step {N}` block END → immediately before the closing `---` separator that precedes `## Orchestration guardrails`.
   - Insert each new `### Step {START}`..`### Step {END}` block in order, fully decomposed (from Phases 4–5 output).
3. **Orchestration guardrails:** do NOT modify unless source doc introduces a new guardrail category (rare — user must explicitly request). Default behavior: leave intact.

**Do NOT:**

- Touch existing `### Step 1..(START-1)` blocks — not even cosmetic edits.
- Overwrite orchestrator header `**Status:**` line — lifecycle skills (`stage-file`, `/closeout`, `project-stage-close`) flip it.
- Insert BACKLOG rows. Create `ia/projects/{ISSUE_ID}.md` stubs. Tasks stay `_pending_`.
- Rename or delete `{SOURCE_DOC}`. Do not edit its expansion block.
- Commit. User decides when.

### Phase 7b — Regenerate progress dashboard

Run `progress-regen` subskill ([`ia/skills/progress-regen/SKILL.md`](../progress-regen/SKILL.md)): `npm run progress` from repo root. Non-blocking — failure does NOT block Phase 7c; log exit code and continue.

### Phase 7c — Demote top Status if currently Final (R6)

After persisting new Step blocks, check the plan top-of-file `> **Status:**` line:

- If top Status reads `Final` AND `appended_steps ≥ 1` (i.e. at least one new Step was added in Phase 7): rewrite top Status to `In Progress — Step {N_first_new} / Stage {N_first_new}.1` where `N_first_new` = `START_STEP_NUMBER`.
- If top Status is NOT `Final` (e.g. already `In Progress`, `Draft`): leave unchanged.
- Rationale: a `Final` plan that gains new Steps is no longer complete — the top Status must reflect that active work remains.
- This flip is idempotent: re-running when Status already reflects the new step produces zero diff.

### Phase 8 — Handoff

Single concise message (caveman) naming:

- `{ORCHESTRATOR_SPEC}` extended — `+N steps · +M stages · +P phases · +Q tasks`. New Step range `{START}..{END}`.
- Source doc referenced in header Exploration source / Read-first.
- Locked decisions delta: `{count}` new locks appended OR `none`.
- Invariants flagged by number + which new stages they gate.
- Cardinality gate: resolved splits / justifications captured.
- Duplication gate: clean OR `{count}` near-duplicates resolved (renamed / merged / dropped).
- Next step: `claude-personal "/stage-file {ORCHESTRATOR_SPEC} Stage {START}.1"` to file the first new stage's pending tasks as BACKLOG rows + project-spec stubs.

**Umbrella flip (if applicable):** If `{ORCHESTRATOR_SPEC}` is a child orchestrator under an umbrella plan (detected by: umbrella's bucket/step table references `{slug}-master-plan.md`, OR user provides umbrella path):
- Find child row in umbrella's bucket / step table where Status = `Planned` or blank.
- Flip to `In Progress` (first EXTEND of a previously-Planned child).
- Append to umbrella `## Change log`: `{YYYY-MM-DD} — {ORCHESTRATOR_SPEC} extended via {SOURCE_DOC}; status → In Progress.`
- Include flip confirmation in handoff message: "Umbrella `{umbrella-path}` — child row `{slug}` → In Progress."

---

## Tool recipe (territory-ia) — Phase 2 only

Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs:

- `keywords`: English tokens from source-doc Chosen Approach + Subsystem Impact + Architecture component names.
- `brownfield_flag`: `true` for greenfield (new subsystem, no existing code paths touched AND no `Assets/**` paths detected); `false` for brownfield (full recipe). If source-doc references any `Assets/**` path (even as a future target), treat as brownfield.
- `tooling_only_flag`: `true` for tooling/pipeline-only plans.

Use returned `glossary_anchors` for canonical names in Phases 4–5; `router_domains` + `spec_sections` for Relevant surfaces; `invariants` for header sync + per-new-stage guardrails.

Also run **`list_specs`** / **`spec_outline`** only if a routed domain references a spec whose sections weren't returned by `domain-context-load`. **Brownfield fallback.**

**Surface-path pre-check (Phase 2 sub-step — greenfield + brownfield):** run `surface-path-precheck` subskill on paths from source-doc Architecture / Component map. Use returned `line_hint` in surfaces; mark `(new)` for `exists: false`.

---

## Guardrails

- IF `{ORCHESTRATOR_SPEC}` does not exist → STOP. Route user to `/master-plan-new {SOURCE_DOC}` (fresh orchestrator).
- IF `{ORCHESTRATOR_SPEC}` shape check fails (missing header / Steps / legend / guardrails) → STOP. Report malformed orchestrator; do not attempt auto-heal.
- IF `{SOURCE_DOC}` missing expansion + phased skeleton intent → STOP. Route user to `/design-explore {SOURCE_DOC}` first.
- IF `START_STEP_NUMBER` ≤ last existing step number → STOP. Overwriting existing Steps requires a fresh revision cycle, not this skill.
- IF proposed new step duplicates an existing step name / objective → apply Phase 1 resolution playbook: Draft existing step → merge as additional Stage; Final existing step → STOP and ask rename/drop/revision-cycle; near-overlap with distinct scope → proceed with note.
- IF any new stage phase has <2 tasks after Phase 6 → STOP. Ask split or justify before persisting.
- IF any new stage phase has 7+ tasks after Phase 6 → STOP. Suggest split; persist only after user confirms or justifies.
- IF router returns `no_matching_domain` for a new subsystem → note gap in "Relevant surfaces" as `{domain} — no router match; load by path: {file}`, continue.
- IF source doc introduces a locked decision that contradicts an existing Locked decision in `{ORCHESTRATOR_SPEC}` → STOP. Contradictions require explicit re-decision + edit to original exploration doc — not appendable via this skill.
- Do NOT touch existing `### Step 1..(START-1)` blocks.
- Do NOT insert BACKLOG rows. Do NOT create `ia/projects/{ISSUE_ID}.md` specs. Tasks stay `_pending_` — `stage-file` materializes them later.
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
  START_STEP_NUMBER: {optional override, else last existing step + 1}
  SCOPE_BOUNDARY_DOC: {optional sibling doc}

Phase 0 validates orchestrator shape; inserts missing Last-updated / Locked-decisions header fields (no STOP for these); loads only SOURCE_SECTION from SOURCE_DOC if provided. Phase 1 computes start number + runs duplication gate (playbook: Draft existing → merge Stage; Final → STOP; distinct scope → note). Phase 2 Tool recipe uses territory-ia MCP slices (greenfield skips router / spec_sections / invariants_summary UNLESS Assets/** paths detected); no full spec reads. Phase 3 emits planned-steps digest — do NOT pause; proceed to Phase 4 immediately. Phases 4–5 fully decompose every new step (no skeletons). Phase 6 cardinality gate: ≥2 tasks per phase AND ≤6 tasks per phase — pause on violation. Phase 7 persists in place — header sync idempotent (Grep → Edit-if-exists, else inject); insert new Step blocks before final `---` + `## Orchestration guardrails`; NEVER touch existing Steps. Phase 8 handoff includes umbrella child-row flip if applicable.
```

---

## Next step

After persist: recommend first new stage to file.

`claude-personal "/stage-file {ORCHESTRATOR_SPEC} Stage {START}.1"` — new steps are already fully decomposed; file stages in step order as each parent step closes.

---

## Changelog

### 2026-04-17 — 6-gap audit patches (release-rollout bootstrap)

**Status:** applied — `9822c08`

**Scope:** Gaps surfaced during `full-game-mvp` rollout dry-run by prior audit agent. Fixes landed in-place; this entry documents the audit trail + dual-writes to tracker `## Skill Iteration Log`.

**Fix #1 — First-run guardrail (header-repair without STOP).**
- Symptom: Phase 0 stopped on older orchestrators missing `**Last updated:**` / `**Locked decisions:**` header fields — blocked legitimate extends.
- Fix: Phase 0 "Insert-if-missing" sub-step injects absent header fields BEFORE continuing validation; STOP reserved for hard-required shape (Steps / legend / Orchestration guardrails).
- Location: §Phase 0 — "Insert-if-missing (header-repair — do NOT STOP; inject BEFORE continuing validation)".

**Fix #2 — Phase 7a header sync idempotence.**
- Symptom: Re-running extend on same source doc duplicated `**Exploration source:**` entries + Locked-decisions bullets.
- Fix: Per-field Grep → Edit-if-exists else inject. Entries only appended when not already present. Today-dated `**Last updated:**` skipped if already set.
- Location: §Phase 7 step 1 — "Header sync (idempotent — for each field: Grep → Edit if exists, else inject)".

**Fix #3 — Partial section load for multi-bucket source docs.**
- Symptom: `full-game-mvp-exploration.md` (1008 lines, 10 buckets) blew token budget + caused cross-bucket bleed when extending a single child plan.
- Fix: Added `SOURCE_SECTION` input. Phase 0 loads only the named bucket subsection + its Implementation Points / Roadmap block; remaining sections ignored for Phase 2 scoping.
- Location: §Inputs `SOURCE_SECTION` + §Phase 0 first paragraph.

**Fix #4 — Phase 3 re-fire protection (subagent single-shot).**
- Symptom: Phase 3 "pause for user confirm" stalled subagent dispatch — subagents run single-shot + cannot hold interactive state across caller boundary.
- Fix: Phase 3 emits planned-steps digest in return message. Does NOT pause. Proceeds directly to Phase 4. Caller re-fires with updated scope if unhappy.
- Location: §Phase 3 — "Emit the planned-steps digest in the return message. Do NOT pause".

**Fix #5 — Umbrella row-flip on child extend.**
- Symptom: Extending a child orchestrator (e.g. `blip-master-plan.md`) did not flip the umbrella's bucket-table row from `Planned` to `In Progress`.
- Fix: Phase 8 detects umbrella parentage (bucket table reference OR user-supplied umbrella path). Flips child row status + appends umbrella `## Change log` entry. Handoff message confirms flip.
- Location: §Phase 8 — "Umbrella flip (if applicable)".

**Fix #6 — Duplication gate resolution playbook.**
- Symptom: Original duplication gate stopped outright on any name collision — blocked legitimate stage-level extension of a Draft step.
- Fix: Three-branch playbook: (a) Draft existing step → merge new scope as additional Stage inside it; (b) Final existing step → STOP (immutable); (c) near-overlap with verifiably distinct scope → proceed + note in handoff.
- Location: §Phase 1 — "Duplication gate: resolution playbook (a) / (b) / (c)" + §Guardrails matching bullet.

**Rollout row:** (setup) — applied pre-rollout; unblocks every subsequent EXTEND call against full-game-mvp children.

**Tracker aggregator:** [`ia/projects/full-game-mvp-rollout-tracker.md#skill-iteration-log-aggregator`](../../projects/full-game-mvp-rollout-tracker.md).

---
