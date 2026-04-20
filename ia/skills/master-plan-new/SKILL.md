---
purpose: "Use after design-explore has persisted `## Design Expansion` in an exploration doc: decompose Implementation Points into step/stage/phase/task skeleton and author `ia/projects/{slug}-master-plan.md` as a permanent orchestrator."
audience: agent
loaded_by: skill:master-plan-new
slices_via: router_for_task, spec_sections, invariants_summary
name: master-plan-new
description: >
  Use when an exploration doc under `docs/` carries a persisted `## Design Expansion` block and the work
  needs a multi-step plan rather than a single BACKLOG issue. Produces `ia/projects/{slug}-master-plan.md`
  — an orchestrator doc (NOT closeable, NEVER deleted by automation) with ALL steps fully decomposed into
  stages → phases → tasks (no skeleton/lazy materialization), tasks seeded as `_pending_` for later
  `stage-file`. Applies `stage-decompose` Phase 2 rules to every step. Triggers: "/master-plan-new {path}",
  "turn expanded design into master plan", "create orchestrator from exploration", "author master plan
  from design expansion", "new multi-step plan from docs/{slug}.md".
model: inherit
---

# Master plan — author orchestrator doc from expanded exploration

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: Objectives 2–4 sentences (human-consumed cold); Exit criteria carry type/method/file precision; Relevant surfaces stay explicit (paths + line refs); Mermaid/ASCII verbatim.

No MCP from skill body. Tool recipe Phase 2 only. All other phases derive from expansion block (literal `## Design Expansion` or semantic equivalents per Phase 0 table).

**Lifecycle:** AFTER [`design-explore`](../design-explore/SKILL.md), BEFORE [`stage-file-plan`](../stage-file-plan/SKILL.md).
`design-explore` → `master-plan-new` → `stage-file-plan` + `stage-file-apply` → `project-new` → `project-spec-implement` → `/closeout` (Stage-scoped).

**Related:** [`design-explore`](../design-explore/SKILL.md) · [`stage-file-plan`](../stage-file-plan/SKILL.md) · [`stage-file-apply`](../stage-file-apply/SKILL.md) · [`project-new`](../project-new/SKILL.md) · [`ia/rules/project-hierarchy.md`](../../rules/project-hierarchy.md) · [`ia/rules/orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md) · [`ia/skills/README.md`](../README.md).

**Shape refs:** [`blip-master-plan.md`](../../projects/blip-master-plan.md) (Step-1-heavy, Steps 2–3 skeletons) · [`multi-scale-master-plan.md`](../../projects/multi-scale-master-plan.md) (mixed state).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `DOC_PATH` | User prompt | Path to exploration doc with expansion block (literal `## Design Expansion` or semantic equivalents per Phase 0) — required |
| `SLUG` | User prompt OR inferred | Kebab-case stem for `ia/projects/{SLUG}-master-plan.md`. Default: exploration doc filename stem stripped of `-exploration` / `-design` suffix |
| `SCOPE_BOUNDARY_DOC` | User prompt | Optional sibling doc (e.g. `{slug}-post-mvp-extensions.md`) listing out-of-scope items. Referenced in Scope header line |

---

## Phase sequence (gated — each phase depends on the previous)

### Phase 0 — Load + validate

Read `{DOC_PATH}`. Confirm expansion block present — literal or semantic equivalents (table below).

**Required content (by intent, not literal heading):**

| Intent | Literal heading (design-explore output) | Semantic equivalents accepted |
|---|---|---|
| Approach + rationale | `### Chosen Approach` | `### Decision` / `### Locked decisions` / `### MVP scope` |
| Entry / exit points | `### Architecture` | `### Component map` / `### Key types` / `### Names registry` |
| Touched subsystems + invariant risk | `### Subsystem Impact` | `### Related subsystems` / `### Integration points` |
| Phased skeleton | `### Implementation Points` | `### Roadmap` / `### MVP feature list` / `### Shipping plan` |

Missing intent → STOP. Run `/design-explore {DOC_PATH}` first. Non-literal heading → continue; note mapping in working memory for Phase 3/4 ref prose.

Hold in working memory:

- **Problem statement** + **Approach rationale** — header Scope line.
- **Architecture / Component map / Key types** — entry / exit points → §"Relevant surfaces".
- **Subsystem Impact / Related subsystems** — touched subsystems + invariant risk numbers → step / stage "Relevant surfaces" + guardrails.
- **Implementation Points / Roadmap** — phased checklist is the raw skeleton for steps / stages / phases / tasks.
- **Examples** + **Review Notes** — Decision Log seed for first stage.
- **Non-scope / Post-MVP** list — out-of-scope / scope-boundary doc handoff (Phase 8).
- **Locked decisions** — MVP guardrails to surface in the header block (do-not-reopen list).

### Phase 1 — Slug + overwrite gate

Resolve `{SLUG}`. Target: `ia/projects/{SLUG}-master-plan.md`. Exists → STOP, ask confirm overwrite or new slug. Fail fast — no MCP yet.

### Phase 2 — MCP context (Tool recipe) + surface-path pre-check

Run **Tool recipe** (below) via `domain-context-load` subskill. **Greenfield** (new subsystem): `brownfield_flag = true` — only glossary loaded. **Brownfield**: `brownfield_flag = false` — full recipe. **Tooling-only**: `tooling_only_flag = true`.

Capture for Phases 3–5:

- `glossary_anchors` → canonical names replace ad-hoc synonyms in all authored prose.
- `spec_sections` → §"Relevant surfaces" lines per step / stage.
- `invariants` → header "Read first" line + stage-level guardrails.

**Surface-path pre-check** — run `surface-path-precheck` subskill ([`ia/skills/surface-path-precheck/SKILL.md`](../surface-path-precheck/SKILL.md)): pass paths from Architecture / Component map block. Use returned `line_hint` values in stage Relevant surfaces; mark `(new)` for `exists: false` entries. Skip → ghost line numbers downstream.

### Phase 3 — Scope header

Author header block (verbatim shape, fill placeholders; invariants + surfaces now available from Phase 2):

```markdown
# {Title} — Master Plan ({SCOPE_LABEL})

> **Status:** Draft — Step 1 / Stage 1.1 pending (no BACKLOG rows filed yet)
>
> **Scope:** {one-line scope — pulled from Chosen Approach + Non-scope. Reference scope-boundary doc if provided}.
>
> **Exploration source:** `{DOC_PATH}` (§{sections of expansion that are ground truth}).
>
> **Locked decisions (do not reopen in this plan):** {bullet or inline list pulled from exploration — locked MVP decisions / scope boundary. Omit line entirely if exploration carries no locked list}.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `{DOC_PATH}` — full design + architecture + examples. Design Expansion block is ground truth.
> - {scope boundary doc if set} — scope boundary (what's OUT of MVP / current scope).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — {flagged numbers from Phase 2 MCP, e.g. `#3 (no FindObjectOfType in hot loops), #4 (no new singletons)`}.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.
```

### Phase 4 — Step decomposition

Group Implementation Points into **steps** — each = major shippable milestone. Rules:

- 1 step = ≥1 user-visible capability OR coherent scaffolding layer.
- Hard dep chain → earlier phases = earlier steps.
- Horizontal expansion → same step, additional stages.
- Target 1–4 steps; 5+ = scope creep red flag.

**Depth rule:** All steps fully decomposed (stages → phases → tasks). Apply the same stage decomposition recipe (Phase 5) to every step. Reuse Phase 2 MCP output — no additional tool calls per step. Steps beyond Step 1 use the prior step's Exit criteria as their "prior step outputs" in Relevant surfaces.

Per step, author:

```markdown
### Step {N} — {Name}

**Status:** Draft (tasks _pending_ — not yet filed)

**Backlog state (Step {N}):** {0 filed / N filed / all filed — reflects BACKLOG rows at author time. Stays `0 filed` until `stage-file` runs.}

**Objectives:** {2–4 sentences — what this step lands + why. Ties back to Chosen Approach rationale.}

**Exit criteria:**

- {concrete observable outcome 1 — cites type / method / file path where verifiable}
- {outcome 2}
- ...

**Art:** {None / list of art assets needed — from Design Expansion if surfaced; else `None`}.

**Relevant surfaces (load when step opens):**
- {exploration doc ref + sections}
- {MCP-routed spec section refs (via Phase 2)}
- {invariant numbers from Subsystem Impact}
- {prior step outputs (Steps 2+) — surfaces shipped by Step {N-1}}
- {code paths — entry/exit points from Design Expansion Architecture block; mark `(new)` for non-existent paths per Phase 2 pre-check}
```

### Phase 5 — Stage decomposition

All steps (Steps 1, 2, … N). Subdivide each step into **stages** — each = sub-milestone with own exit criteria. Apply the same recipe per step; reuse Phase 2 MCP output (no additional tool calls). Follow `ia/skills/stage-decompose/SKILL.md` Phase 2 rules for each step's stage/phase/task authoring. Rules:
- Each stage lands on green-bar boundary (compiles, tests pass, no partial state merged).
- Soft deps within step OK; no stage blocks step's close.
- Target 2–4 stages per step.

**Stage-ordering heuristic** (earliest first):

1. **Scaffolding / infrastructure** — bootstrap, persistent bindings, project settings, AudioMixer groups, scene setup. No data model yet.
2. **Data model** — ScriptableObject / blittable struct / serialized fields. Typed but inert.
3. **Runtime logic** — DSP kernel / update loop / compute code. Consumes data model.
4. **Integration + tests** — call sites, EditMode/PlayMode tests, golden fixtures. Lands last in the step.

Follow order unless Implementation Points declare different dep chain (note deviation in Decision Log seed). Rationale: earlier stages inherit zero scaffolding debt; test stage validates everything shipped.

Per stage, author the block shape:

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
| T{N}.{M}.1 | {short name ≤6 words} | 1 | _pending_ | _pending_ | {≤2 sentences — reference concrete deliverable names: types, methods, file paths. Point at existing patterns if applicable (e.g. `GameNotificationManager.cs` as DontDestroyOnLoad ref, `OnValidate` clamps)} |
| T{N}.{M}.2 | {short name} | 1 | _pending_ | _pending_ | {...} |
| T{N}.{M}.3 | {short name} | 2 | _pending_ | _pending_ | {...} |
```

**Task intent concreteness bar:** avoid vague verbs ("add support for X", "handle Y"). Instead cite the thing being shipped — `BlipPatch` SO with `envelope` / `filter` / `oscillator` sub-objects; `OnValidate` clamps on `attackMs` / `decayMs`; `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` → `AudioMixer.SetFloat("SfxVolume", db)` headless binding in `BlipEngine.Awake`. Concrete intent survives the wait between authoring + `stage-file` materialization.

**Task sizing heuristic:** Each task = one coherent subsystem slice a Sonnet spec-implementer can execute in 3–5 phases with ≤2 `spec_section` context reloads. Use this guide when deciding to merge or split:

- **Correct scope:** 2–5 files forming one algorithm layer — e.g., full AHDSR state machine + envelope math together; oscillator bank across all waveforms; one-pole filter + render loop. Tasks at this size keep `spec_section` reloads to ≤2 and produce meaningful per-phase deltas.
- **Too small (merge):** single file, single function, single constant, single struct with no logic. Merge with an adjacent same-domain task in the same phase. Rationale: each BACKLOG task generates 5 orchestration steps (project-new → kickoff → implement → verify-loop → closeout); single-function tasks multiply that overhead without reducing risk.
- **Too large (split):** touches >3 unrelated subsystems or needs >6 phases to implement. Split at the seam between subsystem layers — the natural coupling boundary is the right split point.

Apply this check in Phase 6 (cardinality gate) alongside the ≥2 / ≤6 count rule.

### Phase 6 — Cardinality gate

Run `cardinality-gate-check` subskill ([`ia/skills/cardinality-gate-check/SKILL.md`](../cardinality-gate-check/SKILL.md)): pass phase → tasks map for each stage authored in Phase 5. Cardinality rule (`ia/rules/project-hierarchy.md`): ≥2 tasks/phase (hard), ≤6 soft.

Subskill returns `{phases_lt_2, phases_gt_6, single_file_tasks, oversized_tasks, verdict}`:
- `verdict = pause` → surface violations to user; ask split, merge, or justify in Decision Log. Proceed only after user confirms or fixes. Phrase split/merge question in player/designer-visible outcomes (releasable slices, user-visible checkpoints), not stage numbers or task-count math. Ids / stage numbers go on a trailing `Context:` line. Full rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).
- `verdict = proceed` → continue to Phase 7.

Also covers Phase 5 task sizing: single-file/function/struct tasks → `single_file_tasks`; >3 unrelated subsystems → `oversized_tasks`.

### Phase 7 — Tracking legend

Insert the standard tracking legend once under `## Steps` (copy verbatim from an existing master plan, e.g. `blip-master-plan.md` line 22). Do not paraphrase — downstream skills (`stage-file-plan`, `stage-file-apply`, `/author`, `/implement`, `/closeout`) flip markers based on exact enum values.

```markdown
## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | Skeleton | Planned | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`; `Skeleton` + `Planned` authored by `master-plan-new` / `stage-decompose`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file-plan` + `stage-file-apply` → task rows gain `Issue` id + `Draft` status; `stage-file-apply` also flips Stage header `Draft/Planned → In Progress` (R2) and plan top Status `Draft → In Progress — Step {N} / Stage {N.M}` on first task ever filed (R1); `stage-decompose` → Step header `Skeleton → Draft (tasks _pending_)` (R7); `/author` → `In Review`; `/implement` → `In Progress`; `/closeout` (Stage-scoped) → `Done (archived)` + phase box when last task of phase closes + stage `Final` + step rollup; `master-plan-extend` → plan top Status `Final → In Progress — Step {N_new} / Stage {N_new}.1` when new Steps appended to a Final plan (R6).
```

### Phase 8 — Persist

Write `ia/projects/{SLUG}-master-plan.md`. Order: **header block** → `---` → `## Steps` + tracking legend → Step 1 (full decomp) → stages of Step 1 → Step 2 (full decomp) → stages of Step 2 → ... → Step N (full decomp) → stages of Step N → `---` → `## Orchestration guardrails` → final `---` separator.

No `## Deferred decomposition` section — all steps are fully decomposed at author time.

**Orchestration guardrails section** — Do / Do not lists for agents landing cold. Template:

```markdown
## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` (Stage-scoped pair) runs.
- Run `claude-personal "/stage-file {this-doc} Stage {N}.{M}"` (routes to `stage-file-plan` + `stage-file-apply` pair) to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc + scope-boundary doc.
- Keep this orchestrator synced with umbrella issue (if one exists) — per Stage-scoped `/closeout` (pair) umbrella-sync rule.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items into MVP stages — they belong in the scope-boundary doc.
- Merge partial stage state — every stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
```

Never overwrite an existing master plan file (Phase 1 gate). Never insert BACKLOG rows (that's `stage-file` + `project-new`). Never reference unfiled issue ids in Depends on — tasks stay `_pending_` until stage-file materializes them.

### Phase 8b — Regenerate progress dashboard

Run `progress-regen` subskill ([`ia/skills/progress-regen/SKILL.md`](../progress-regen/SKILL.md)): `npm run progress` from repo root. Non-blocking — failure does NOT block Phase 9; log exit code and continue.

### Phase 9 — Handoff

Single concise message (caveman) naming:

- `{SLUG}-master-plan.md` written — step / stage / phase / task counts (e.g. `3 steps · 9 stages · 22 phases · 48 tasks`). All steps fully decomposed.
- Invariants flagged by number + which stages they gate.
- Cardinality gate: resolved splits / justifications captured.
- Non-scope list outcome: scope-boundary doc referenced in header, OR **recommend stub** if exploration carries explicit post-MVP items but no companion doc exists yet (propose path `docs/{SLUG}-post-mvp-extensions.md` — NOT this skill's job to create; user runs a separate task).
- Next step: `claude-personal "/stage-file {SLUG}-master-plan.md Stage 1.1"` (or named first stage) to file its pending tasks as BACKLOG rows + project-spec stubs.

---

## Tool recipe (territory-ia) — Phase 2 only

**Composite-first call (MCP available):**

1. Call `mcp__territory-ia__orchestrator_snapshot({ slug: "{SLUG}" })` — first MCP call; returns existing orchestrator state, step/stage/task inventory, and locked decisions. Use snapshot to check for existing plan conflicts and surface prior decisions for Phases 3–5.
2. Proceed to `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)) as before. Inputs:
   - `keywords`: English tokens from Chosen Approach + Subsystem Impact + Architecture block component names.
   - `brownfield_flag`: `true` for greenfield — skips `router_for_task` / `spec_sections` / `invariants_summary`. `false` for brownfield.
   - `tooling_only_flag`: `true` for tooling/pipeline-only plans.
   Use returned `glossary_anchors` for canonical names in Phases 3–5; `router_domains` + `spec_sections` for Relevant surfaces; `invariants` for header "Read first" + per-stage guardrails.
3. Run **`list_specs`** / **`spec_outline`** only if a routed domain references a spec whose sections weren't returned by `domain-context-load`. **Brownfield fallback.**
4. **Surface-path pre-check (Phase 2 sub-step):** run `surface-path-precheck` subskill on paths from Architecture / Component map. Use returned `line_hint` in surfaces; mark `(new)` for `exists: false`. Skip → ghost line numbers downstream.

### Bash fallback (MCP unavailable)

1. Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs:
   - `keywords`: English tokens from Chosen Approach + Subsystem Impact + Architecture block component names.
   - `brownfield_flag`: `true` for greenfield (new subsystem, no existing code paths modified) — skips `router_for_task` / `spec_sections` / `invariants_summary`. `false` for brownfield (full recipe).
   - `tooling_only_flag`: `true` for tooling/pipeline-only plans (skips `invariants_summary` regardless of brownfield flag).
   Use returned `glossary_anchors` for canonical names in Phases 3–5; `router_domains` + `spec_sections` for Relevant surfaces; `invariants` for header "Read first" + per-stage guardrails.
2. Run **`list_specs`** / **`spec_outline`** only if a routed domain references a spec whose sections weren't returned by `domain-context-load`. **Brownfield fallback.**
3. **Surface-path pre-check (Phase 2 sub-step — greenfield + brownfield):** run `surface-path-precheck` subskill on paths from Architecture / Component map. Use returned `line_hint` in surfaces; mark `(new)` for `exists: false`. Skip → ghost line numbers downstream.

---

## Guardrails

- IF expansion intent missing from `{DOC_PATH}` (neither literal `## Design Expansion` nor semantic equivalents per Phase 0 table) → STOP, route user to `/design-explore {DOC_PATH}` first.
- IF `ia/projects/{SLUG}-master-plan.md` already exists → STOP, ask user to confirm overwrite OR pick new slug. Orchestrator docs are permanent; never silently overwrite.
- IF any stage phase has <2 tasks after Phase 6 → STOP, ask user to split or justify before persisting.
- IF any stage phase has 7+ tasks after Phase 6 → STOP, suggest split; persist only after user confirms or justifies.
- IF router returns `no_matching_domain` for a subsystem → note the gap in "Relevant surfaces" as `{domain} — no router match; load by path: {file}`, continue.
- IF exploration doc's Non-scope list carries explicit post-MVP items but no companion `docs/{SLUG}-post-mvp-extensions.md` exists → raise recommendation in Phase 9 handoff. Do NOT create the stub — that's a separate task.
- Do NOT insert BACKLOG rows. Do NOT create `ia/projects/{ISSUE_ID}.md` specs. Tasks stay `_pending_` — `stage-file` materializes them later.
- Do NOT delete or rename exploration doc. Do NOT edit its expansion block.
- Do NOT commit — user decides when to commit the new orchestrator.

---

## Seed prompt

```markdown
Run the master-plan-new workflow against {DOC_PATH}.

Follow ia/skills/master-plan-new/SKILL.md end-to-end. Inputs:
  DOC_PATH: {path}
  SLUG: {optional slug override, else inferred from filename stem}
  SCOPE_BOUNDARY_DOC: {optional sibling doc path}

Phase 2 Tool recipe uses territory-ia MCP slices (greenfield skips router / spec_sections / invariants_summary); no full spec reads. Cardinality gate requires ≥2 tasks per phase AND ≤6 tasks per phase — pause for user confirmation on either violation. Steps 2+ stay as skeletons until Step 1 closes (lazy decomposition).
```

---

## Next step

After persist: recommend first stage to file.

`claude-personal "/stage-file {SLUG}-master-plan.md Stage 1.1"` — all steps are already fully decomposed; file stages in step order as each parent step closes.

---

## Changelog

### 2026-04-18 — wiring-review

**source:** wiring-review

**deviation:** `## Next step` section appeared AFTER `## Changelog` (lines 357–361 post-stanza). Changelog must be the terminal section so self-report appenders land at file tail. Moved `## Next step` to before `## Changelog`.

---
