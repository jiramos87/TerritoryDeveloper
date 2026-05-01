---
name: master-plan-new
purpose: >-
  Use after design-explore has persisted `## Design Expansion` in an exploration doc: decompose
  Implementation Points into stage/task (2-level hierarchy) and author
  `ia_master_plans` + `ia_stages` rows as a permanent orchestrator. Canonical shape:
  `docs/MASTER-PLAN-STRUCTURE.md`.
audience: agent
loaded_by: "skill:master-plan-new"
slices_via: router_for_task, spec_sections, invariants_summary
description: >-
  Use when an exploration doc under `docs/` carries a persisted `## Design Expansion` block and the
  work needs a multi-stage plan rather than a single BACKLOG issue. Produces
  `ia_master_plans` row + `ia_stages` rows (orchestrator is permanent — never closeable, never
  deleted by automation) with ALL Stages fully decomposed into Tasks (2-level hierarchy: `Stage >
  Task`). Tasks seeded `_pending_` for later `stage-file`.
  Canonical shape authority: `docs/MASTER-PLAN-STRUCTURE.md` — file shape, Stage block shape, 5-column
  Task table schema, Status enums, flip matrix. Triggers: "/master-plan-new {path}", "turn expanded
  design into master plan", "create orchestrator from exploration", "author master plan from design
  expansion".
phases: []
triggers:
  - /master-plan-new {path}
  - turn expanded design into master plan
  - create orchestrator from exploration
  - author master plan from design expansion
argument_hint: >-
  {DOC_PATH} [SLUG] [SCOPE_BOUNDARY_DOC] (e.g. docs/foo-exploration.md foo
  docs/foo-post-mvp-extensions.md)
model: inherit
reasoning_effort: high
tools_role: custom
tools_extra:
  - Read
  - Bash
  - mcp__territory-ia__master_plan_insert
  - mcp__territory-ia__stage_insert
  - mcp__territory-ia__master_plan_preamble_write
  - mcp__territory-ia__master_plan_description_write
  - mcp__territory-ia__master_plan_change_log_append
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
  - Mermaid / diagram blocks persisted to the doc
  - orchestrator header block prose (human-consumed cold — may run 2–4 sentences per Objectives field)
hard_boundaries:
  - "IF expansion intent missing from `{DOC_PATH}` (neither literal `## Design Expansion` nor semantic equivalents per Phase 0 table) → STOP, route user to `/design-explore {DOC_PATH}` first."
  - IF `master_plan_render({slug: SLUG})` returns a plan payload → STOP, ask user to confirm overwrite OR pick new slug. Orchestrator rows are permanent; never silently overwrite.
  - IF any stage phase has <2 tasks after Phase 6 → STOP, ask user to split or justify before persisting.
  - IF any stage phase has 7+ tasks after Phase 6 → STOP, suggest split; persist only after user confirms or justifies.
  - "IF router returns `no_matching_domain` for a subsystem → note the gap in \"Relevant surfaces\" as `{domain} — no router match; load by path: {file}`, continue."
  - IF exploration doc's Non-scope list carries explicit post-MVP items but no companion `docs/{SLUG}-post-mvp-extensions.md` exists → raise recommendation in Phase 9 handoff. Do NOT create the stub — separate task.
  - Do NOT insert BACKLOG rows. Do NOT create `ia/projects/{ISSUE_ID}.md` specs. Tasks stay `_pending_` — `stage-file` materializes them later.
  - Do NOT delete or rename exploration doc. Do NOT edit its expansion block.
caller_agent: master-plan-new
---

# Master plan — author orchestrator doc from expanded exploration

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md). Exceptions: Objectives 2–4 sentences (human-consumed cold); Exit criteria carry type/method/file precision; Relevant surfaces stay explicit (paths + line refs); Mermaid/ASCII verbatim.

No MCP from skill body. Tool recipe Phase 2 only. All other phases derive from expansion block (literal `## Design Expansion` or semantic equivalents per Phase 0 table).

**Canonical master-plan shape:** [`docs/MASTER-PLAN-STRUCTURE.md`](../../../docs/MASTER-PLAN-STRUCTURE.md) — authoritative source for file shape, Stage block subsections, 5-column Task table schema, Status enums, lifecycle flip matrix. This skill authors TO that shape; if this skill drifts, MASTER-PLAN-STRUCTURE.md wins.

**Lifecycle:** AFTER [`design-explore`](../design-explore/SKILL.md), BEFORE [`stage-file`](../stage-file/SKILL.md).
`design-explore` → `master-plan-new` → `stage-file` → `stage-authoring` → `spec-implementer` → `/ship-stage` (inline closeout).

**Related:** [`design-explore`](../design-explore/SKILL.md) · [`master-plan-extend`](../master-plan-extend/SKILL.md) · [`stage-decompose`](../stage-decompose/SKILL.md) · [`stage-file`](../stage-file/SKILL.md) · [`docs/MASTER-PLAN-STRUCTURE.md`](../../../docs/MASTER-PLAN-STRUCTURE.md) · [`ia/rules/project-hierarchy.md`](../../rules/project-hierarchy.md) · [`ia/rules/orchestrator-vs-spec.md`](../../rules/orchestrator-vs-spec.md).

**Shape ref:** [`docs/MASTER-PLAN-STRUCTURE.md`](../../../docs/MASTER-PLAN-STRUCTURE.md) is the sole authority. Render any existing DB-backed orchestrator via `mcp__territory-ia__master_plan_render({slug})` if a working example is needed — no filesystem `.md` exemplars (master plans are DB rows).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `DOC_PATH` | User prompt | Path to exploration doc with expansion block (literal `## Design Expansion` or semantic equivalents per Phase 0) — required |
| `SLUG` | User prompt OR inferred | Kebab-case slug for `ia_master_plans.slug`. Default: exploration doc filename stem stripped of `-exploration` / `-design` suffix |
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
| Staged skeleton | `### Implementation Points` | `### Roadmap` / `### MVP feature list` / `### Shipping plan` |

Missing intent → STOP. Run `/design-explore {DOC_PATH}` first. Non-literal heading → continue; note mapping in working memory for Phase 3/4 ref prose.

Hold in working memory:

- **Problem statement** + **Approach rationale** — header Scope line.
- **Architecture / Component map / Key types** — entry / exit points → §"Relevant surfaces".
- **Subsystem Impact / Related subsystems** — touched subsystems + invariant risk numbers → stage "Relevant surfaces" + guardrails.
- **Implementation Points / Roadmap** — staged checklist is the raw skeleton for Stages + Tasks (2-level: no Step grouping, no Phase layer).
- **Examples** + **Review Notes** — Decision Log seed for first stage.
- **Non-scope / Post-MVP** list — out-of-scope / scope-boundary doc handoff (Phase 8).
- **Locked decisions** — MVP guardrails to surface in the header block (do-not-reopen list).
- **Plan shape** — parse `### Plan Shape` block when present: extract `Shape:` value → `plan_shape ∈ {carcass+section, flat}`. When `plan_shape=carcass+section`: parse `### Carcass Stages` (≤3 rows, format `Carcass {N} — {gate} — {objective}`) + `### Sections` (≥1 rows, format `Section {A|B|C|...} — {name} — {subsystems}`) into working memory as `CARCASS_LIST` + `SECTION_ROSTER`. Fallback: block absent OR partial parse → `plan_shape=flat`; emit warning `SHAPE_PARSE_WARN: treating as flat (Plan Shape block missing or partial)`.

### Phase 1 — Slug + overwrite gate

Resolve `{SLUG}`. Target: DB row `ia_master_plans.slug = {SLUG}`. Probe via `master_plan_render({slug: SLUG})`:

- Returns plan payload → STOP, ask confirm overwrite or new slug.
- Returns `not_found` → continue. Fail fast — no MCP context load yet.

Master plans persist as DB rows (`ia_master_plans` + `ia_master_plan_change_log` + `ia_stages` + `ia_tasks`). No filesystem probe.

### Phase 2 — MCP context (Tool recipe) + surface-path pre-check

Run **Tool recipe** (below) via `domain-context-load` subskill. **Greenfield** (new subsystem): `brownfield_flag = true` — only glossary loaded. **Brownfield**: `brownfield_flag = false` — full recipe. **Tooling-only**: `tooling_only_flag = true`.

Capture for Phases 3–4:

- `glossary_anchors` → canonical names replace ad-hoc synonyms in all authored prose.
- `spec_sections` → §"Relevant surfaces" lines per stage.
- `invariants` → header "Read first" line + stage-level guardrails.

**Surface-path pre-check** — run `surface-path-precheck` subskill ([`ia/skills/surface-path-precheck/SKILL.md`](../surface-path-precheck/SKILL.md)): pass paths from Architecture / Component map block. Use returned `line_hint` values in stage Relevant surfaces; mark `(new)` for `exists: false` entries. Skip → ghost line numbers downstream.

### Phase 3 — Scope header + dashboard description

Author header block per MASTER-PLAN-STRUCTURE.md §2 (canonical fields). Fill placeholders; invariants + surfaces now available from Phase 2:

**Also author** a short product-terminology `description` (≤200 chars soft target — advisory, not enforced) summarizing what the plan delivers + main goals in user-facing product wording. Persisted on `ia_master_plans.description` and rendered as the dashboard subtitle directly under the plan title (replaces the verbose preamble panel). Required for new plans. Compose case-by-case from header block (Scope + Locked decisions) — drop ids / file paths / stage numbers; keep the 1–2-sentence shape a designer/PM can read cold.

```markdown
# {Title} — Master Plan ({SCOPE_LABEL})

> **Last updated:** {YYYY-MM-DD}
>
> **Status:** Draft — Stage 1.1 pending (no BACKLOG rows filed yet)
>
> **Scope:** {one-line scope — pulled from Chosen Approach + Non-scope. Reference scope-boundary doc if provided}.
>
> **Exploration source:** `{DOC_PATH}` (§{sections of expansion that are ground truth}).
>
> **Locked decisions (do not reopen in this plan):** {bullet or inline list pulled from exploration — locked MVP decisions / scope boundary. Omit line entirely if exploration carries no locked list}.
>
> **Hierarchy rules:** `docs/MASTER-PLAN-STRUCTURE.md` (canonical file + Stage + Task table shape — authoritative). `ia/rules/project-hierarchy.md` (stage > task — 2-level cardinality). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable). `ia/rules/plan-apply-pair-contract.md` (§Plan section shape for pair seams).
>
> **Read first if landing cold:**
> - `{DOC_PATH}` — full design + architecture + examples. Design Expansion block is ground truth.
> - {scope boundary doc if set} — scope boundary (what's OUT of MVP / current scope).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + Stage / Task cardinality rule (≥2 tasks per Stage).
> - `ia/rules/invariants.md` — {flagged numbers from Phase 2 MCP, e.g. `#3 (no FindObjectOfType in hot loops), #4 (no new singletons)`}.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.
```

### Phase 4 — Stage decomposition

Map Implementation Points directly to **Stages** — each = shippable compilable increment landing on green-bar boundary. 2-level hierarchy: Stages are flat siblings (no Step grouping); each Stage carries its Task table directly (no Phase layer). Reuse Phase 2 MCP output — no additional tool calls per stage.

**Stage numbering:** `Stage N.M`. Use `M` subdivisions when a single milestone splits into serial sub-milestones (e.g. `Stage 1.1 scaffolding`, `Stage 1.2 data model` within MVP cluster `1`). Simple plans may use single-level `Stage 1 / Stage 2 / ...` — equivalent; N.M pattern is convention, not mandatory.

**Stage-ordering heuristic** (earliest first):

1. **Scaffolding / infrastructure** — bootstrap, persistent bindings, project settings, AudioMixer groups, scene setup. No data model yet.
2. **Data model** — ScriptableObject / blittable struct / serialized fields. Typed but inert.
3. **Runtime logic** — DSP kernel / update loop / compute code. Consumes data model.
4. **Integration + tests** — call sites, EditMode/PlayMode tests, golden fixtures. Lands last.

Follow order unless Implementation Points declare different dep chain (note deviation in Decision Log seed). Rationale: earlier stages inherit zero scaffolding debt; test stage validates everything shipped.

**Stage count target:** 2–6 Stages typical; 7+ suggests scope creep (consider splitting into sibling master plans with dependency note).

Per stage, author the canonical block shape (per MASTER-PLAN-STRUCTURE.md §3):

```markdown
### Stage {N}.{M} — {Name}

**Status:** Draft

**Notes:** tasks _pending_ — not yet filed

**Backlog state (Stage {N}.{M}):** 0 filed

**Objectives:** {2–4 sentences — what this stage lands + why. Ties back to Chosen Approach rationale. Human-consumed cold; full English OK per caveman exception for Objectives prose}.

**Exit criteria:**

- {concrete observable outcome 1 — cites type / method / file path where verifiable}
- {outcome 2}
- {glossary row additions, if canonical terms introduced}

**Art:** {None / list of art assets needed from Design Expansion; else `None`}.

**Relevant surfaces (load when stage opens):**

- {exploration doc ref + sections}
- {MCP-routed spec section refs (via Phase 2)}
- {invariant numbers from Subsystem Impact}
- {prior stage outputs — surfaces shipped by Stage {N}.{M-1} or earlier}
- {code paths — entry / exit points from Design Expansion Architecture block; mark `(new)` for non-existent paths per Phase 2 pre-check}

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T{N}.{M}.1 | {short name ≤6 words} | _pending_ | _pending_ | {≤2 sentences — concrete deliverable: types, methods, file paths. Reference existing patterns (e.g. `GameNotificationManager.cs` DontDestroyOnLoad, `OnValidate` clamps). Avoid vague verbs like "add support for X"} |
| T{N}.{M}.2 | {short name} | _pending_ | _pending_ | {...} |
| T{N}.{M}.3 | {short name} | _pending_ | _pending_ | {...} |

#### §Stage File Plan

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

_pending — populated by `/plan-review` when fixes are needed._
```

**Carcass + section title annotation (carcass-shape plans only):**

Carcass-shape master plans (any Stage with `carcass_role='carcass'`) annotate Stage titles to make role + section affiliation visible at-a-glance in the rendered preamble + `## Stages` ToC. Convention:

- Carcass stages: `### Stage {N}.{M} — {Name} (carcass)` — append literal ` (carcass)`.
- Section-affiliated stages: `### Stage {N}.{M} — {Name} (section-{X})` — `{X}` = capital letter A/B/C/... matching `section_id` order from Phase 4 stage decomposition (first declared section = `section-A`, second = `section-B`, etc.).
- Plain (no carcass + no section) stages: no suffix — `### Stage {N}.{M} — {Name}`.

Annotation is title-only — the `carcass_role` + `section_id` DB columns remain the source of truth; downstream `arch_drift_scan(scope='intra-plan')` + `/ship-stage` Pass A claim mutex read DB, not title. Goal of the suffix: human-readable orientation when landing cold on a long preamble.

Skip annotation for legacy linear plans (no carcass stages). Mixing carcass + linear stages in one plan is forbidden per `docs/MASTER-PLAN-STRUCTURE.md` — carcass-shape applies whole-plan.

**Task table schema (5 columns, per MASTER-PLAN-STRUCTURE.md §3):**

- `Task` = hierarchical id `T{N}.{M}.{K}` (e.g. `T1.3.2`).
- `Name` = short ≤6-word handle (doubles as BACKLOG row title + spec file name).
- `Issue` = `_pending_` until `stage-file` fills with `**{PREFIX}-NNN**`.
- `Status` = `_pending_ → Draft → In Review → In Progress → Done (archived)`.
- `Intent` = ≤2 sentences naming concrete deliverable (types / methods / file paths).

**No `Phase` column.** Subgrouping happens via N.M Stage numbering or Stage-internal ordering.

**Task intent concreteness bar:** avoid vague verbs ("add support for X", "handle Y"). Instead cite the thing being shipped — `BlipPatch` SO with `envelope` / `filter` / `oscillator` sub-objects; `OnValidate` clamps on `attackMs` / `decayMs`; `PlayerPrefs.GetFloat("BlipSfxVolumeDb", 0f)` → `AudioMixer.SetFloat("SfxVolume", db)` headless binding in `BlipEngine.Awake`. Concrete intent survives the wait between authoring + `stage-file` materialization.

**Task sizing heuristic:** Each task = one coherent subsystem slice a Sonnet spec-implementer can execute with ≤2 `spec_section` context reloads. Use this guide when deciding to merge or split:

- **Correct scope:** 2–5 files forming one algorithm layer — e.g., full AHDSR state machine + envelope math together; oscillator bank across all waveforms; one-pole filter + render loop. Tasks at this size keep `spec_section` reloads to ≤2 and produce meaningful per-phase deltas.
- **Too small (merge):** single file, single function, single constant, single struct with no logic. Merge with an adjacent same-domain task in the same Stage. Rationale: each BACKLOG task generates per-Task orchestration steps inside `/ship-stage` (implement → verify-loop → code-review) plus the upstream `/stage-file` filing + `/stage-authoring`; single-function tasks multiply that overhead without reducing risk.
- **Too large (split):** touches >3 unrelated subsystems or needs >6 phases of implementation to execute. Split at the seam between subsystem layers — the natural coupling boundary is the right split point.

Apply this check in Phase 5 (cardinality gate) alongside the ≥2 / ≤6 count rule.

### Phase 5 — Cardinality gate

Run `cardinality-gate-check` subskill ([`ia/skills/cardinality-gate-check/SKILL.md`](../cardinality-gate-check/SKILL.md)): pass Stage → Task count map. Cardinality rule (`ia/rules/project-hierarchy.md`): ≥2 Tasks/Stage (hard), ≤6 soft.

Subskill returns `{stages_lt_2, stages_gt_6, single_file_tasks, oversized_tasks, verdict}`:

- `verdict = pause` → surface violations to user; ask split, merge, or justify in Decision Log. Proceed only after user confirms or fixes. Phrase split/merge question in player/designer-visible outcomes (releasable slices, user-visible checkpoints), not stage numbers or task-count math. Ids / stage numbers go on a trailing `Context:` line. Full rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).
- `verdict = proceed` → continue to Phase 6.

Also covers Phase 4 task sizing: single-file/function/struct tasks → `single_file_tasks`; >3 unrelated subsystems → `oversized_tasks`.

### Phase 6 — Tracking legend

Insert the canonical tracking legend once under `## Stages` (copy verbatim from MASTER-PLAN-STRUCTURE.md §3). Do not paraphrase — downstream skills (`stage-file`, `stage-authoring`, `spec-implementer`, `/ship-stage` inline closeout) flip markers based on exact enum values.

```markdown
## Stages

> **Tracking legend:** Stage `Status:` uses enum `Draft | In Review | In Progress | Final` (per `docs/MASTER-PLAN-STRUCTURE.md` §6.2). Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `stage-authoring` → `In Review`; `spec-implementer` → `In Progress`; `/ship-stage` inline closeout → `Done (archived)` + Stage `Final` rollup.
```

### Phase 7 — Persist (DB-only)

Compose markdown in working memory using the canonical order per MASTER-PLAN-STRUCTURE.md §4:

1. **Header block** (Phase 3 output)
2. `---`
3. `## Stages` + tracking legend (Phase 6)
4. `### Stage 1.1` (full Task table + 2 subsections §Stage File Plan / §Plan Fix)
5. `### Stage 1.2` (same shape) · ... · `### Stage N.M`
6. `---`
7. `## Orchestration guardrails`
8. Final `---` separator

**Stage block path-verify gate (pre-persist).** For each Stage block in working memory, extract every repo-relative file path token from the `**Relevant surfaces (load when stage opens):**` bullets (skip URLs, MCP tool names, glossary refs, paths already annotated `(new)`). Call `mcp__territory-ia__plan_digest_verify_paths({ paths: [...extracted] })` per Stage. Per returned `{path, exists}` row with `exists: false` AND no `(new)` annotation → emit warning `SURFACE_PATH_MISS Stage {N}.{M}: '{path}' cited in Relevant surfaces but not on disk + missing (new) marker.` Surface to user; offer fix-in-place (annotate `(new)` or correct typo) before persistence. Warn-only — author overrides allowed when the path lands inside a downstream Task. Defense-in-depth: stage-file Phase 2.2b re-checks at file time, but author-time fix is cheaper than file-time re-author.

**Persist via DB MCP** (no filesystem write):

1. `master_plan_insert({slug: SLUG, title: "{plan title}", preamble: "{everything from Header block through tracking legend}", description: "{Phase 3 short product description, ≤200 chars}"})` — creates the `ia_master_plans` row + preamble + description (dashboard subtitle). Description required for new plans.
2. For each Stage block authored — pass `carcass_role` + `section_id` based on working-memory shape:
   - **`plan_shape=carcass+section`:** stages annotated `(carcass)` → `stage_insert({slug, stage_id, title, body, objective, exit_criteria, carcass_role: "carcass", section_id: null})`; stages annotated `(section-A/B/C/...)` → `stage_insert({..., carcass_role: "section", section_id: "{kebab-name}"})` where `{kebab-name}` = kebab-case of section name from `SECTION_ROSTER` (e.g. `Section A — Data model` → `data-model`); un-annotated stages → `carcass_role: null, section_id: null`.
   - **`plan_shape=flat`:** `stage_insert({slug: SLUG, stage_id: "{N}.{M}", title: "{name}", body: "{full Stage block markdown}", objective: "{Objectives prose}", exit_criteria: "{Exit criteria bullets joined}"})` — no `carcass_role` / `section_id` params (current behavior preserved).
3. **Task pre-seed (mandatory).** For every Task row inside every authored Stage's Task table, call `task_insert({prefix: "TECH" (or domain-inferred FEAT/BUG/ART/AUDIO), title: row.Name, slug: SLUG, stage_id: "{N}.{M}", status: "pending", notes: row.Intent})`. Creates the `ia_tasks` rows that downstream `/stage-file` mode-detect (`tools/scripts/recipe-engine/stage-file/mode-detect.sh`) requires — `mode=file` only fires when `ia_tasks WHERE status='pending'` count ≥ 1. Skipping this step strands the Stage: `stage_render` shows the Task table from Stage body markdown, but `ia_tasks` is empty, so `/stage-file` halts with `mode-detect: no-op (pending=0) — nothing to file`. Idempotent on `(slug, stage_id, title)` — re-runs no-op. Use `TECH` for tooling/IA/MCP/migration/test/CI surfaces; pick `FEAT` / `BUG` / `ART` / `AUDIO` only when domain match is unambiguous from the Task intent.
4. `master_plan_change_log_append({slug: SLUG, kind: "plan_authored", body: "Authored {N} stages, {T} pending task rows pre-seeded from {DOC_PATH}"})` — audit row.

No `## Deferred decomposition` section — all Stages fully decomposed at author time (refer `master-plan-extend` for new-Stage authoring post-ship).

**Orchestration guardrails section** — Do / Do not lists for agents landing cold. Template:

```markdown
## Orchestration guardrails

**Do:**

- Open one Stage at a time. Next Stage opens only after current Stage's `/ship-stage` (inline closeout) lands.
- Run `claude-personal "/stage-file {SLUG} Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + task spec stubs (DB-backed).
- Update Stage `Status` as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc + scope-boundary doc.
- Keep this orchestrator synced with umbrella issue (if one exists) — per `/ship-stage` inline closeout umbrella-sync rule.
- Extend via `/master-plan-extend {SLUG} {source-doc}` when a new exploration or extensions doc introduces new Stages — do NOT hand-insert Stage blocks.

**Do not:**

- Close this orchestrator — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal Stage landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items into MVP stages — they belong in the scope-boundary doc.
- Merge partial Stage state — every Stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Hand-insert new Stages past the last persisted `### Stage N.M` block — run `/master-plan-extend` so MCP context + cardinality gate + progress regen fire.
```

Never overwrite an existing master plan DB row (Phase 1 gate). Never insert BACKLOG rows (that's `stage-file`). Never reference unfiled issue ids in Depends on — tasks stay `_pending_` until stage-file materializes them.

### Phase 7b — Regenerate progress dashboard

Run `progress-regen` subskill ([`ia/skills/progress-regen/SKILL.md`](../progress-regen/SKILL.md)): `npm run progress` from repo root. Non-blocking — failure does NOT block Phase 8; log exit code and continue.

### Phase 8 — Handoff

Single concise message (caveman) naming:

- `{SLUG}` master plan written — Stage / Task counts (e.g. `4 stages · 14 tasks`). All Stages fully decomposed.
- Invariants flagged by number + which stages they gate.
- Cardinality gate: resolved splits / justifications captured.
- Non-scope list outcome: scope-boundary doc referenced in header, OR **recommend stub** if exploration carries explicit post-MVP items but no companion doc exists yet (propose path `docs/{SLUG}-post-mvp-extensions.md` — NOT this skill's job to create; user runs a separate task).
- **Next step (parallel-eligible commands for the *immediate* next move only — not the full wave plan).** Compute the parallel-eligible set per the algorithm below + emit one `/stage-file` command per eligible stage in a fan-out block. Do NOT enumerate downstream waves (sections after carcass, or stages past the immediate next gate) — user will re-invoke after each gate clears.

**Parallel-eligibility algorithm (immediate-next only):**

1. Read all stages just persisted from working memory (`stage_id`, `carcass_role`, `section_id`, `depends_on` if any).
2. Find the lowest `stage_id` group with no unsatisfied dependencies — the "current front".
3. Apply shape rules:
   - **Carcass+section plan** (any `carcass_role='carcass'` stage exists) → eligible set = ALL `carcass_role='carcass'` stages (no inter-sibling deps per D16/D18). Section stages are NOT eligible until carcass closes — omit from this handoff.
   - **Legacy linear plan** (all `carcass_role` NULL) → eligible set = single first Stage (typically `Stage 1.1`). Subsequent stages gate on prior completion — omit.
4. If eligible set has 1 stage → emit single `Next` line.
5. If eligible set has ≥2 stages → emit fan-out block:

```markdown
**Next — parallel-eligible (open one terminal per line):**

```
Session α: claude-personal "/stage-file {SLUG} Stage {N.M-α}"
Session β: claude-personal "/stage-file {SLUG} Stage {N.M-β}"
Session γ: claude-personal "/stage-file {SLUG} Stage {N.M-γ}"
```

Sessions touch disjoint surfaces — no contention. Re-invoke handoff logic after this front closes for the next gate.
```

For carcass+section plans, append a one-liner naming the gate that unlocks the section wave: `Gate after carcass: ia_master_plan_health.carcass_done = true (check via mcp__territory-ia__master_plan_health).`

---

## Tool recipe (territory-ia) — Phase 2 only

**Composite-first call (MCP available):**

1. Call `mcp__territory-ia__orchestrator_snapshot({ slug: "{SLUG}" })` — first MCP call; returns existing orchestrator state, Stage/Task inventory, and locked decisions. Use snapshot to check for existing plan conflicts and surface prior decisions for Phases 3–4.
2. Proceed to `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs:
   - `keywords`: English tokens from Chosen Approach + Subsystem Impact + Architecture block component names.
   - `brownfield_flag`: `true` for greenfield — skips `router_for_task` / `spec_sections` / `invariants_summary`. `false` for brownfield.
   - `tooling_only_flag`: `true` for tooling/pipeline-only plans.
   Use returned `glossary_anchors` for canonical names in Phases 3–4; `router_domains` + `spec_sections` for Relevant surfaces; `invariants` for header "Read first" + per-stage guardrails.
3. Run **`list_specs`** / **`spec_outline`** only if a routed domain references a spec whose sections weren't returned by `domain-context-load`. **Brownfield fallback.**
4. **Surface-path pre-check (Phase 2 sub-step):** run `surface-path-precheck` subskill on paths from Architecture / Component map. Use returned `line_hint` in surfaces; mark `(new)` for `exists: false`. Skip → ghost line numbers downstream.

### Bash fallback (MCP unavailable)

1. Run `domain-context-load` subskill as above.
2. Run **`list_specs`** / **`spec_outline`** only if a routed domain references a spec whose sections weren't returned. **Brownfield fallback.**
3. **Surface-path pre-check** on Architecture / Component map paths.

---

## Guardrails

- IF expansion intent missing from `{DOC_PATH}` (neither literal `## Design Expansion` nor semantic equivalents per Phase 0 table) → STOP, route user to `/design-explore {DOC_PATH}` first.
- IF `master_plan_render({slug: SLUG})` returns a plan payload → STOP, ask user to confirm overwrite OR pick new slug. Orchestrator rows are permanent; never silently overwrite.
- IF any Stage has <2 Tasks after Phase 5 → STOP, ask user to split or justify before persisting (Decision Log waiver).
- IF any Stage has 7+ Tasks after Phase 5 → STOP, suggest split; persist only after user confirms or justifies.
- IF router returns `no_matching_domain` for a subsystem → note the gap in "Relevant surfaces" as `{domain} — no router match; load by path: {file}`, continue.
- IF exploration doc's Non-scope list carries explicit post-MVP items but no companion `docs/{SLUG}-post-mvp-extensions.md` exists → raise recommendation in Phase 8 handoff. Do NOT create the stub.
- IF authored output carries `### Step N` heading, `**Phases:**` checkbox block, `Phase` column in Task table, or `#### Stage N.M` H4 heading → STOP. Canonical shape is H3 Stages with 5-column Task table (see MASTER-PLAN-STRUCTURE.md §1).
- IF `description` arg empty / missing on `master_plan_insert` → STOP. Description (≤200 char soft target, product-terminology overview) is required for new plans — it backs the dashboard subtitle.
- IF Phase 7 step 3 (task pre-seed via `task_insert`) skipped → downstream `/stage-file` halts with `mode-detect: no-op (pending=0)` because `ia_tasks` carries zero rows for the stage. Hard contract: every Task table row authored in Phase 4 → one `task_insert` row in `ia_tasks` with `status='pending'`. The Task table inside Stage body markdown is presentational; `ia_tasks` is the queryable source of truth.
- Do NOT insert BACKLOG rows. Do NOT create `ia/projects/{ISSUE_ID}.md` specs. Tasks stay `_pending_` (= `ia_tasks.status='pending'` rows pre-seeded by Phase 7 step 3) — `stage-file` materializes them into BACKLOG + spec stubs later.
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

Canonical master-plan shape: docs/MASTER-PLAN-STRUCTURE.md (file shape, Stage block, 5-col Task table, Status enums). 2-level hierarchy Stage > Task (no Steps, no Phases). Phase 2 Tool recipe uses territory-ia MCP slices (greenfield skips router / spec_sections / invariants_summary); no full spec reads. Cardinality gate requires ≥2 tasks per Stage AND ≤6 tasks per Stage — pause for user confirmation on either violation. All Stages fully decomposed at author time (no skeleton/lazy materialization — use master-plan-extend for post-ship extensions).
```

---

## Next step

After persist: recommend first stage to file.

`claude-personal "/stage-file {SLUG} Stage 1.1"` — all Stages already fully decomposed; file in order.


---

## Changelog

| Date | Change | Trigger |
|------|--------|---------|
| 2026-04-29 | Phase A recipe extension — `tools/recipes/master-plan-new-phase-a.yaml` handles deterministic DB mutations for carcass-aware plans: `master_plan_insert` + `arch_decision_write` ×N (foreach) + HEAD SHA resolve + `master_plan_lock_arch`. Skill Phase sequence unchanged for legacy linear path. | `docs/parallel-carcass-exploration.md` §7 PR 3.3 |
| 2026-04-29 | Phase 7 contract fix — added mandatory task pre-seed step (was: stage_insert → change_log; now: stage_insert → `task_insert` per Task row → change_log). Closes structural gap where Stage body markdown showed a Task table but `ia_tasks` had zero pending rows, causing `/stage-file` mode-detect to halt with `pending=0`. Idempotent on (slug, stage_id, title). | parallel-carcass-rollout Stage 1.1 stage-file escalation; recipe contract gap diagnosis |

### 2026-04-29 — skill-train run

**source:** train-proposed

**proposal:** `ia/skills/master-plan-new/proposed/2026-04-29-train.md`

**friction_count:** 0

**threshold:** 2

---
