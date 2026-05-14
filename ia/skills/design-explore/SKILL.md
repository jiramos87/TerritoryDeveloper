---
name: design-explore
purpose: >-
  Use before a master plan or backlog issue exists: survey approaches in an exploration doc, select
  one, expand with architecture + subsystem impact + implementation points + examples, review with a
  subagent, and persist back to the same doc.
audience: agent
loaded_by: "skill:design-explore"
slices_via: router_for_task, spec_sections, invariants_summary
description: >-
  Use when an exploration doc (under docs/) needs to move from fuzzy survey to a defined, detailed,
  reviewed design ready to seed a master plan or BACKLOG issue. Phases: compare approaches → select →
  expand → architecture → subsystem impact → implementation points → examples → subagent review →
  persist. Triggers: "/design-explore [path]", "expand exploration", "design review [doc]", "turn this
  exploration into a design", "compare and select approach", "take this exploration doc to a master
  plan".
phases: []
triggers:
  - /design-explore [path]
  - expand exploration
  - design review [doc]
  - turn this exploration into a design
  - compare and select approach
  - take this exploration doc to a master plan
argument_hint: >-
  {DOC_PATH} [APPROACH_HINT] [--against REFERENCE_DOC] [--force-model {model}] [--resume {slug}]
  (e.g. docs/foo.md C OR docs/foo.md --against docs/full-game-mvp-exploration.md OR --resume ship-protocol)
model: inherit
reasoning_effort: high
input_token_budget: 160000
pre_split_threshold: 140000
tools_role: planner
tools_extra:
  - Agent
  - mcp__territory-ia__spec_outline
  - mcp__territory-ia__list_specs
  - mcp__territory-ia__cron_arch_changelog_append_enqueue
caveman_exceptions:
  - code
  - commits
  - security/auth
  - verbatim error/tool output
  - structured MCP payloads
  - Mermaid / diagram blocks persisted to the doc
hard_boundaries:
  - IF approach not confirmed after Phase 2 → STOP, ask user. Do NOT guess.
  - IF subagent review returns BLOCKING items → resolve, re-run Phase 8, then persist.
  - IF `{DOC_PATH}` unreadable → stop, report path error.
  - IF touched subsystem spec unavailable via MCP → note gap in Subsystem Impact, continue.
  - "Do NOT overwrite Problem / Approaches surveyed / Recommendation / Open questions — only write the `## Design Expansion` block."
  - Do NOT create master plan, BACKLOG row, or invoke `project-new` — user triggers next step after review.
  - Do NOT commit — user decides when.
  - Do NOT load whole reference specs when `spec_section` / `spec_sections` slices cover it.
  - Phase 3.5 enriched grill — Stage 1 strict band: every task carries all 8 fields. Stages 2+ — fields with `always` mandatory band MUST be present. Partial enrichment on Stage 1 → STOP + re-poll.
  - Phase 9 — emit BOTH `.html` (canonical artifact) and `.md` (derived sidecar). `.html` MUST embed the canonical MD inside `<script id="rawMarkdown" type="text/plain">`. Round-trip `extract-md → diff .md` MUST be byte-clean before handing back. NEVER persist `.html` without the sidecar `.md` (legacy validator chain breaks).
caller_agent: design-explore
---

# Design exploration — expand, review, persist

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

No MCP calls from skill body. Follow **Tool recipe** below (Phase 5 only) — all other phases derive from the exploration doc itself.

**Position in lifecycle:** fires _before_ master plan creation or `project-new`.  
`design-explore` → `ship-plan` → `ship-cycle` → `ship-final`.

**Related:** [`project-new`](../project-new/SKILL.md) · [`ship-plan`](../ship-plan/SKILL.md) · [`ship-cycle`](../ship-cycle/SKILL.md) · [`ia/skills/README.md`](../README.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `DOC_PATH` | User prompt | Path to exploration `.md` — required |
| `APPROACH_HINT` | User prompt | Override recommendation (e.g. `"C"`) — skips Phase 2 gate |
| `AGAINST_DOC` | User prompt | Path to reference orchestrator / umbrella doc — activates **gap-analysis mode** when `DOC_PATH` is a locked design with no Approaches list |

---

## Phase sequence (gated — each phase depends on the previous)

### Phase 0 — Load

Read `{DOC_PATH}`. Extract and hold in working memory:

- **Problem statement** — hard constraints the design must satisfy
- **Approaches list** — id, name, pros/cons/effort for each
- **Existing recommendation** — if present
- **Open questions** — if present

**Locked-doc detection (end of Phase 0):**

| Doc state | `AGAINST_DOC` set? | Action |
|---|---|---|
| Has Approaches list | Either | Standard mode — continue to Phase 1 |
| Locked design (no Approaches list) | Yes | **Gap-analysis mode** — skip to [§ Gap-analysis mode](#gap-analysis-mode) |
| Locked design (no Approaches list) | No | STOP — present user three options: (A) add an Approaches section and re-run, (B) pass `--against {UMBRELLA_DOC}` to run gap analysis, (C) skip to `/master-plan-extend` if no alignment gaps expected |
| `DOC_PATH` unreadable | Either | STOP — report path error |

**Plan-shape gate (end of Phase 0):**

After locked-doc detection, poll the user for plan shape. Skip if working memory already carries a `plan_shape` value (re-run or gap-analysis resume).

Plain-language preface + `Recommended:` line mandatory per [`agent-human-polling`](../../rules/agent-human-polling.md). Poll runs ONE time; downstream phases (2.5 / 6 / 9) read `plan_shape` — never re-poll.

```
Plan shape — release slices that ship in parallel (carcass+section), or linear ladder of stages (flat)?

A. Carcass + sections — parallel sections that run simultaneously inside milestone gates
B. Flat — linear ladder of stages, one at a time

Recommended: flat (safe default; choose carcass+section only when parallel work streams are confirmed)
```

Capture result as working-memory token: `plan_shape ∈ {carcass+section, flat}`.

**Non-interactive default:** `flat` (headless runs / skipped poll).

### Phase 0.5 — Interview (user gate)

**Skip this phase entirely** if the doc already has a `## Design Expansion` block with a completed Select section. Proceed directly to Phase 1 (compare) or Phase 3 (expand) as appropriate.

If the doc is a stub (no Design Expansion), run a short interview to surface hidden constraints and disambiguate open questions before building the criteria matrix.

**Language rules (strict — game design vocabulary only):**
- Questions MUST use player/designer language: player experience, game rules, economic mechanics, UI interactions, design goals, balance choices.
- NEVER mention class names, method signatures, file paths, C# types, or Unity-specific internals in questions. Those are implementation details the agent resolves independently.
- Good: "When the player's police budget runs out this month, should they be blocked from building new police stations entirely, or just warned?"
- Bad: "Should `BudgetAllocationService.TryDraw()` check the treasury floor before drawing from the envelope?"
- Full rule + exceptions: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md). Fetch via `rule_content agent-human-polling` when drafting interview questions.

**Interview rules (strict):**
- Ask **ONE question per turn. Stop. Wait for the user's answer** before asking the next.
- Do NOT present a numbered list. Do NOT say "here are my questions".
- **Plain-language preface (mandatory)** — 1–2 sentences before option list explaining WHAT is being decided + WHY it matters in product/dev-flow terms. No jargon-only polls.
- **Recommendation (mandatory)** — every poll ends with a `Recommended:` line picking one option + 1-line rationale (speed / token cost / robustness / blast radius / unblock value). User can override; never absent. Anchor: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).
- Pull from: (1) open questions already listed in the doc, (2) up to 3 inferred questions about scope boundaries, blocking constraints, or priority trade-offs not covered by existing answers.
- Max 5 questions total — but extend if user explicitly requests more product-scope coverage. Stop early if earlier answers already cover remaining questions.
- After the last answer: emit a one-paragraph summary of what you learned, then proceed to Phase 1 without another confirmation prompt.

Start with the single most important unknown — typically a scope boundary, blocking constraint the approaches don't address, or a stakeholder priority the doc leaves ambiguous.

### Phase 1 — Compare + Exit Gate

**Polling template lookup (Phase 1 + Phase 2):**

When `core_prototype.verb` is set in working memory, load the matching template before building polls:

```
ia/templates/polling/{verb}.json
```

Supported verbs: `trim`, `add`, `replace`, `refactor`, `integrate`. If the verb is missing or the file is absent, fall back to LLM-authored polls (no error — template is advisory).

Template shape: `{ verb, question, plain_language_preface, options[{id,label,tradeoff}], recommended, recommended_rationale, slot_keys[] }`. Fill `{{slot}}` placeholders from working memory before rendering. Emit the `plain_language_preface` + `Recommended:` line verbatim (with slots filled) — do NOT paraphrase them.

**Phase 1 exit hard rule (zero unresolved decisions):** Phase 1 MUST NOT advance to Phase 2 while any decision remains unresolved. The exit gate enforces this via a `phase-1-done` token (see below).

**Relentless polling loop (cross-link: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md)):**

Before building the criteria matrix, run an `AskUserQuestion` loop that resolves all unresolved decisions. Loop rules:
1. Each round: list ALL remaining unresolved decisions as a numbered preamble before posing any question.
2. Ask 1–4 questions per round (1-4 per `agent-human-polling.md` band cap); pause after each round for the user response.
3. Re-enter the loop while ≥1 decision remains unresolved. Do NOT advance to Phase 2 in this state.
4. The loop body MUST enumerate outstanding decisions before each poll round — never skip the preamble.

**Exit token contract:**
- Phase 1 exits ONLY when BOTH conditions are true:
  a. Zero unresolved decisions remain.
  b. The human types the literal token `phase-1-done` OR picks the "close phase 1" option in a poll.
- If zero decisions remain but the token has not been received: re-run the loop with a single confirmation poll listing 0 outstanding decisions.
- If the token is received but decisions remain: ignore the token, re-run the loop listing the outstanding decisions.

Build a criteria matrix from the problem constraints. Score each approach.

Criteria (adapt to domain; always include):
- **Constraint fit** — how well it satisfies the hard constraints
- **Effort** — low / medium / high
- **Output control** — determinism, palette/projection/format compliance
- **Maintainability** — how much ongoing authoring/QA is required
- **Dependencies / risk** — external tools, training data, third-party stability

Emit comparison as a Markdown table. Hold in working memory; not yet persisted.

### Phase 2 — Select (user gate)

If existing recommendation is unambiguous **and** `APPROACH_HINT` is not set:
proceed with it, state choice explicitly.

Otherwise: present comparison table + leading candidate → **pause, ask user to confirm or override before continuing**.

**Polling wording** (strict): question stem + each option label describe player/designer-visible outcome, not approach codenames or stage numbers. Ids and doc paths go on a trailing `Context:` line, not inside the question. **Plain-language preface + `Recommended:` line are mandatory** (1–2 sentence framing before options + recommended pick with rationale after). Full rule: [`ia/rules/agent-human-polling.md`](../../rules/agent-human-polling.md).

### Phase 2.5 — Architecture Decision (DEC-A15 lock)

Per DEC-A15 (`arch-authoring-via-design-explore`): if the selected approach touches `arch_surfaces` (architectural inventory), lock a `arch_decisions` row + audit trail before architecture diagram render in Phase 4.

**Skip-clause:** phase no-ops when Phase 5 subsystem-impact returns zero `arch_surfaces[]` hits. Heuristic: any selected component whose spec home falls under `ia/specs/architecture/**` OR matches an existing `arch_surfaces.spec_path` row triggers the phase. Code-only / UI-only / tooling-only explorations skip this phase silently.

**Polling shape — single combined form (TECH-15913):**

Load `ia/templates/polling/arch-decision.json`. Fill `{{topic}}` slot with the decision topic (derived from Phase 3 component name or the selected approach name).

Emit **one `AskUserQuestion`** carrying all 4 axes as a single form:

```
[plain_language_preface with {{topic}} filled]

Please fill in all four fields:

1. Problem statement (≤250 chars): What design question are we answering?
2. Chosen approach (≤250 chars): Which option + one-line rationale.
3. Alternatives considered (≤400 chars): ≤3 options NOT chosen + why. Semicolon-separated.
4. Consequences (≤300 chars): What changes downstream?

Return as a JSON block with keys: problem, chosen, alternatives, consequences.
```

Parse the user's JSON response. If any field is `"?"` or empty, re-poll for that field only (one follow-up per missing field; max 2 follow-ups total).

After all 4 axes are resolved, derive the decision slug from the `problem` field: kebab-case summary prefixed `DEC-A{N}` where `{N}` = next free in `arch_decisions`.

> **Legacy (pre-TECH-15913):** 4 sequential AskUserQuestion turns (one per axis). Retained for reference; single-form replaces it.

**Affected `arch_surfaces[]`** — separate single poll (not part of form-fill) after axes are locked: list candidate slugs from Phase 3 component → spec_path mapping; user confirms / trims.

**MCP writes (after polling) — shape-branched:**

Read `plan_shape` from working memory (set in Phase 0 plan-shape gate).

**When `plan_shape=carcass+section`** — seed 3 plan-scoped decision rows (slug-shape contract: `plan-{slug}-{suffix}` where `{slug}` = exploration doc slug; `{suffix}` ∈ `boundaries` / `end-state-contract` / `shared-seams`). Write in order:

1. `arch_decision_write({ slug: "plan-{slug}-boundaries", plan_slug: "{slug}", title: "Plan {slug} — Boundaries", summary: "{one-line from working memory}", status: "locked" })`
2. `arch_decision_write({ slug: "plan-{slug}-end-state-contract", plan_slug: "{slug}", title: "Plan {slug} — End-State Contract", summary: "{one-line from working memory}", status: "locked" })`
3. `arch_decision_write({ slug: "plan-{slug}-shared-seams", plan_slug: "{slug}", title: "Plan {slug} — Shared Seams", summary: "{one-line from working memory}", status: "locked" })`

Then:

4. `cron_arch_changelog_append_enqueue({ kind: 'design_explore_decision', decision_slug: "plan-{slug}-boundaries", body, commit_sha: null })` — fire-and-forget; returns `{job_id, status:'queued'}` < 100 ms.
5. `arch_drift_scan({ open_plans_only: true })`

**When `plan_shape=flat`** — legacy single DEC-A15 path (unchanged):

1. `arch_decision_write({ slug: "architecture-lock-{slug}", title, rationale, alternatives, surface_slugs[], status: 'active' })`
2. `cron_arch_changelog_append_enqueue({ kind: 'design_explore_decision', decision_slug, body, commit_sha: null })` — fire-and-forget; returns `{job_id, status:'queued'}` < 100 ms.
3. `arch_drift_scan({ open_plans_only: true })`

**Drift report render target:** append to exploration doc under sibling section `### Architecture Decision` (peer of `### Architecture` block authored in Phase 4 → §Persist). Block contains: decision row summary + rendered drift report (per-plan breakdown).

**Stop condition:** if any MCP write fails → stop, surface error, do NOT continue to Phase 3 / 4. User must reconcile DB state before re-running.

### Phase 3 — Expand

Detail the selected approach:

- **Components** — list each with a one-line responsibility
- **Data flow** — inputs → processing steps → outputs (linear or branching)
- **Interfaces / contracts** — key function signatures, file formats, event boundaries, CLI flags
- **Non-scope** — explicit list of what this approach does NOT do

### Phase 3.5 — Enriched field grill (8 fields per task / stage)

Schema contract: [`ia/rules/design-explore-output-schema.md`](../../rules/design-explore-output-schema.md). Fetch via `rule_content design-explore-output-schema` before starting the grill. Carries the 8 field shapes, mandatory bands, MD persistence shape, downstream ingestion points.

**Causal chain.** The 8 fields cross from this skill into `ship-plan` Phase B (digest authoring) into `ship-cycle` Pass A (spec-implementer) into `verify-loop` Pass B. Without enrichment authoring here, downstream digests stay thin → spec-implementer + verifier work from prose only. With it, body_md carries machine-typed mockups + edge cases + failure modes + path previews.

**Phase 3.5 fires** for every stage + task produced in Phase 3, regardless of plan_shape. Order: stage-level fields first (`edge_cases[]`, `shared_seams[]`), then per-task fields (6 × N tasks).

**Polling shape — one question per turn.** Plain-language preface + Recommended line mandatory per [`agent-human-polling`](../../rules/agent-human-polling.md). Defer the heaviest fields (`visual_mockup_svg`, `before_after_code`) to last.

**Grill poll order (8 polls, expand per task / stage):**

1. **Per stage — `edge_cases[]`.** "What edge cases must the red-stage proof cover before this stage closes?" Agent enumerates ≥3 `{input, state, expected}` triples per stage. Always mandatory.
2. **Per stage — `shared_seams[]`.** "What interface contracts cross from this stage forward or backward into other stages?" Skip when stage is self-contained (validator emits INFO). Bullet list of `{name, producer_stage, consumer_stages, contract}`.
3. **Per task — `glossary_anchors[]`.** Driven by Tool recipe (no human poll required); ≥1 anchor per task. Always mandatory.
4. **Per task — `touched_paths_with_preview`.** Driven by Tool recipe `csharp_class_summary` calls. One entry per item in `touched_paths`. Always mandatory.
5. **Per task — `failure_modes[]`.** "Name the concrete ways this task can ship broken." Bullet list of "Fails if X" statements; ≥1 entry. Always mandatory.
6. **Per task — `decision_dependencies`.** Driven by `arch_decision_list` (Tool recipe); per-task-type band — fires when touched_paths overlap `ia/specs/architecture/**` OR `digest_outline` references DEC-A*. Optional otherwise.
7. **Per task — `visual_mockup_svg`.** "What does this task SHOW the user when shipped?" Inline SVG ≤200 LOC, ds-* palette. Always mandatory on Stage 1 tracer-slice tasks; per-task-type elsewhere (kind=code AND touched_paths include UI surface); optional otherwise.
8. **Per task — `before_after_code`.** Driven by Tool recipe `csharp_class_summary`. Pair `{path, before, after}` ≤30 LOC per side. Per-task-type band — kind=code AND largest touched-path LOC ≥50; optional otherwise.

**Hard rule — Stage 1 strict band.** Every task of the first stage carries all 8 fields. Stage 1 is the load-bearing prototype demonstration (prototype-first methodology); partial enrichment defeats the purpose. Validator (`validate-design-explore-yaml.mjs`) enforces.

**Persist shape (in working memory until Phase 9):**

- YAML frontmatter `enriched:` sub-block per task (machine-typed values).
- Parallel MD body subsections (`#### Task {ID} — Enriched` + sub-headings).

Both surfaces emitted at Phase 9 persist time. Schema contract specifies dual surface; renderer (Phase 9) reads frontmatter for widget binding, ship-plan reads body subsections for digest injection.

### Phase 4 — Architecture + Red-Stage Proofs + YAML Emitter

Emit both:

1. **Mermaid diagram** (`flowchart LR` or `graph TD`) — component relationships, data flow, external boundaries
2. **Entry / exit points** — where callers invoke this system and what they receive back

If Mermaid would exceed 20 nodes, produce ASCII block + a simplified Mermaid showing only top-level components.

**Mandatory per-stage red-stage proof block:**

For each Stage in the exploration, emit a pseudo-code proof block (5–15 lines, Python-flavoured). Block must:
- Name the assertion being validated (e.g. `assert stage_N_visibility_delta_visible()`)
- Name the expected failure mode if the Stage ships broken
- Reference glossary terms only (no class names, file paths, or Unity-specific internals)
- Appear under `### Red-Stage Proof — Stage {N}` heading in the exploration doc

**Per-task red-stage proof (opt-in):**

Emit a task-level proof block only when the human explicitly signals during grilling (default = no task-level block). Use heading `### Red-Stage Proof — {TASK_KEY}`.

**Lean YAML frontmatter emitter:**

Emit a YAML frontmatter block at the VERY TOP of the exploration doc (`docs/explorations/{slug}.md` or versioned `{slug}-v{N+1}.md` on `--resume`). Bounded by `---` fences. Required keys:

```yaml
---
slug: {slug}
parent_plan_slug: null  # or the parent slug on version bumps
target_version: 1       # existing_max_version + 1 on --resume
stages:
  - stage_id: "1"
    title: "{Stage 1 title}"
    status: pending
tasks:
  - prefix: TECH
    depends_on: []
    digest_outline: "{one-line goal}"
    touched_paths: []
    kind: implementation
---
```

All keys required. Each task entry carries: `prefix`, `depends_on`, `digest_outline`, `touched_paths`, `kind`.

**`--resume {slug}` mode:**

When invoked as `design-explore --resume {slug}`:
1. Read existing master plan from DB via `master_plan_render` + `master_plan_lineage` MCP tools (cross-link: TECH-14103 supplies `master_plan_lineage`).
2. Classify stages: re-grill ONLY stages where `backfilled = true` OR pre-scan band = `partial`; skip `present_complete` stages (never re-grill — preserves human-authored content).
3. Versioned filename: if `target_version > 1`, write to `{slug}-v{N+1}.md`; v=1 stays at `{slug}.md`. Previous file remains immutable (M#11 collision guard).
4. Exit Phase 4 with a lean YAML pointing at `target_version = existing_max_version + 1` so downstream `ship-plan --version-bump` can pick it up.

### Phase 5 — Subsystem impact

Run **Tool recipe** (below). For each touched subsystem:

- Nature of dependency (reads / writes / publishes events / new interface required)
- Invariant risk (`ia/rules/invariants.md` — flag by number if at risk)
- Breaking vs. additive-only change
- Recommended mitigation if breaking

### Phase 6 — Implementation points

Phased checklist ordered by dependency.

**When `plan_shape=carcass+section`** — group items under `#### Carcass` (shared across sections) + `#### Section {A|B|C|...}` (section-scoped) subheadings:

```
#### Carcass
Phase A — {Shared deliverable}
  - [ ] Task 1
  ...
  Risk: {flag or "none"}

#### Section A
Phase B — {Section-scoped deliverable}
  - [ ] Task 1
  ...
  Risk: {flag or "none"}
```

**When `plan_shape=flat`** — flat format (unchanged):

```
Phase A — {Deliverable}
  - [ ] Task 1
  - [ ] Task 2
  ...
  Risk: {flag or "none"}

Phase B — ...
```

Close with: **Deferred / out of scope** list.

### Phase 7 — Examples

Concrete I/O for the most non-obvious part of the design:

- ≥1 input sample (YAML / JSON / CLI invocation / code)
- ≥1 expected output sample (file content, struct, rendered result, command output)
- ≥1 edge case with expected behavior

### Phase 8 — Subagent review

**Skip-gate (TECH-15912 — both gates required):**

Before spawning the subagent, run the skip-gate check:

1. **YAML format gate** — run `validate:design-explore-yaml` against the exploration doc:
   ```
   node tools/scripts/validate-design-explore-yaml.mjs {DOC_PATH}
   ```
   Exit code 0 = YAML clean. Exit code 1 = schema violation → do NOT skip.

2. **MCP warning gate** — count MCP tool warnings emitted during Phases 0–7. Zero warnings = gate passes.

**When both gates pass:** skip subagent invocation. Record outcome inline as:
```
> Phase 8 skipped — YAML format gate: clean, MCP warnings: 0. Subagent review not required.
```
Proceed directly to Phase 9.

**When either gate fails:** fire subagent as normal (see prompt below). Log which gate failed.

Spawn a `Plan` subagent via the Agent tool. Prompt template:

```
Review this design expansion for [{DOC_PATH} topic].

[Paste Phases 3–7 output here]

Identify and return as a bulleted critique grouped by:
  BLOCKING   — must resolve before shipping
  NON-BLOCKING — should address but won't stop progress
  SUGGESTIONS — optional improvements

For each item: location (which phase), problem, recommended fix.
```

- Resolve all **BLOCKING** items before persisting.
- Include **NON-BLOCKING** + **SUGGESTIONS** verbatim in `## Review Notes`.

### Phase 9 — Persist (HTML-first + MD derived artifact)

**Output shape — `.html` canonical + `.md` derived sidecar.**

Phase 9 emits TWO files atomically:

- `docs/explorations/{slug}.html` — canonical artifact (embeds raw MD inside `<script id="rawMarkdown">` block + renders 11 Thariq widgets + embeds enriched fields from frontmatter)
- `docs/explorations/{slug}.md` — derived sidecar (byte-equal to the MD embedded inside the HTML; ship-plan Phase A.0 prefers HTML extract path but falls back to .md when only the sidecar is present)

The `.md` exists so legacy validators (`validate-design-explore-yaml.mjs`, `validate:frontmatter`, `generate:ia-indexes`) keep operating on a single text source without traversing HTML. Hand-edits to `.md` are absorbed at next design-explore re-run via the renderer reading `.md` as input.

**Steps (in order):**

1. Author canonical MD in working memory: frontmatter (lean YAML + `enriched:` blocks per task / stage) + body (Design Expansion + parallel `#### Task {ID} — Enriched` subsections per task).
2. Detect `## Design Expansion` existence inside current `{DOC_PATH}` (when caller passed an existing `.md`):
   - **Exists** → update content in place between that header and the next `---` separator.
   - **Not present** → append after a `---` separator following the last existing section.
3. Write `.md` sidecar to disk: `docs/explorations/{slug}.md` (or versioned `{slug}-v{N+1}.md` on `--resume`).
4. Run renderer: `npm run design-explore:render-html {slug}`. Renderer reads `.md`, parses frontmatter via `js-yaml`, fills the `ia/templates/design-explore.html.template` slots (`{{TITLE}}`, `{{META_CHIPS_JSON}}`, `{{FRONTMATTER_RAW}}`, `{{RAW_MD}}`, `{{STAGES_JSON}}`, `{{DECISIONS_JSON}}`, `{{REFERENCES_JSON}}`, `{{CUSTOM_BLOCKS}}`), writes `docs/explorations/{slug}.html`.
5. Validate round-trip: `npm run design-explore:extract-md {slug} > /tmp/extracted.md` then `diff /tmp/extracted.md docs/explorations/{slug}.md`. Zero diff (modulo trailing newline) confirms canonical embed is byte-clean.
6. Hand back ABSOLUTE paths + `open` cmd so user's IDE can reach both files:

   ```
   .md  → file:///Users/.../docs/explorations/{slug}.md
   .html → file:///Users/.../docs/explorations/{slug}.html
   ```

**Mandatory subsections (per `docs/prototype-first-methodology-design.md §6 D10` — prototype-first methodology):**

`## Design Expansion` MUST carry both `### Core Prototype` and `### Iteration Roadmap`. Skill MUST FAIL persist (abort + structured error, no partial write) when either is missing or empty.

- **`### Core Prototype` (mandatory).** Maps 1:1 to Stage 1.0 §Tracer Slice in the downstream master plan (`docs/MASTER-PLAN-STRUCTURE.md §3.5`). Carries 5 named fields, all non-empty:
  - `verb:` — what the player/agent can do at end of Stage 1.0 (one verb-phrase, free-form).
  - `hardcoded_scope:` — list of hardcoded data / scenes / configs.
  - `stubbed_systems:` — list of stub methods returning constants.
  - `throwaway:` — visible-layer items acceptable for Stage 2+ rewrite.
  - `forward_living:` — structural-layer items locked forward.
- **`### Iteration Roadmap` (mandatory).** Maps 1:1 to Stages 2+ §Visibility Delta lines (`docs/MASTER-PLAN-STRUCTURE.md §3.6`). 3-column table, ≥1 row, no plumbing-only rows:

  | Stage | Scope | Visibility delta |
  |---|---|---|
  | 2.x | {what this iteration adds} | {one sentence — what player/agent sees/feels new} |

**§Red-Stage Proof seed sources (informational — emission owned by ship-plan Phase 4, not design-explore):** The downstream master plan Stage blocks derive §Red-Stage Proof block fields from three sources produced here: §Implementation Points → per-Stage proof skeleton (one proof block per Stage); §Tracer Slice (`verb` field) → Stage 1.0 `red_test_anchor` (`target_kind=tracer_verb`); §Iteration Roadmap §Visibility Delta rows → Stages 2+ `red_test_anchor` (`target_kind=visibility_delta`). Exploration outputs these sources; ship-plan Phase 4 binds them into each Stage block at plan authoring time.
- See [ia/rules/tdd-red-green-methodology.md](../../rules/tdd-red-green-methodology.md) — anchor grammar + `target_kind` / `proof_status` enum tables.

**Persist failure mode:** Missing or empty `### Core Prototype` OR missing/empty `### Iteration Roadmap` → skill aborts persist with structured error `design_explore_persist_contract_violation` naming the missing/empty subsection. No partial write to `{DOC_PATH}`.

**Optional layout fields (HTML render only).** When the exploration carries visual goals, cross-stage patterns, per-stage checkpoint screenshots, per-stage iteration logs, or per-stage handoff scope summaries, emit them as optional frontmatter keys: `visual_goals[]`, `patterns_observed[]`, `stages[].checkpoint_screenshots[]`, `stages[].iteration_log[]`, `stages[].scope_summary`, and `handoff_template` (top-level override). Field shapes documented in [`ia/rules/design-explore-output-schema.md`](../../rules/design-explore-output-schema.md) §Optional layout fields. No validator gates — renderer treats missing fields as empty arrays; HTML sections hide when their backing array is empty. Author hand-fills `visual_goals` at Phase 3 when the exploration carries a UI target; `iteration_log` + `checkpoint_screenshots` populate organically post-`ship-cycle` (later automated via DB sidecar).

**Canonical reference fixture:** [`tools/recipes/__fixtures__/design-explore-persist-contract-v2.fixture.json`](../../../tools/recipes/__fixtures__/design-explore-persist-contract-v2.fixture.json) — authoring shape canonical for §Core Prototype 5-field block + §Iteration Roadmap 3-column table.

Sections to write (in order):

```markdown
---

## Design Expansion

### Plan Shape
- Shape: {carcass+section|flat}
- Rationale: {one-liner — why this shape fits the problem}

### Carcass Stages
{Emit only when plan_shape=carcass+section. ≤3 entries (hard cap). Format per row:
`Carcass {N} — {milestone gate name} — {one-line objective}`}

### Sections
{Emit only when plan_shape=carcass+section. ≥1 entries. Format per row:
`Section {A|B|C|...} — {section name} — {touched subsystems / glossary anchors}`}

### Core Prototype
{MANDATORY per D10 — feeds Stage 1.0 §Tracer Slice. 5 named fields, all non-empty:
- `verb:` {one player/agent verb-phrase}
- `hardcoded_scope:` {list of hardcoded data/scenes/configs}
- `stubbed_systems:` {list of stub methods returning constants}
- `throwaway:` {visible-layer items rewriteable in Stage 2+}
- `forward_living:` {structural-layer items locked forward}}

### Iteration Roadmap
{MANDATORY per D10 — feeds Stages 2+ §Visibility Delta lines. 3-column table, ≥1 row, no plumbing-only rows. Each row → one Stage. `Visibility delta` column MUST be unique within table.}

| Stage | Scope | Visibility delta |
|---|---|---|
| 2.x | {what this iteration adds} | {one sentence — what player/agent sees/feels new} |

### Chosen Approach
{approach id + name + one-paragraph rationale referencing Phase 1 criteria}

### Architecture Decision
{Phase 2.5 output — DEC-A{N} row summary (slug + rationale + alternatives + surface_slugs) + rendered drift report from `arch_drift_scan`. Skip block when phase 2.5 skip-clause fires (no arch_surfaces hits).}

### Architecture
{Phase 4 diagram(s) + entry/exit point description}

### Subsystem Impact
{Phase 5 table or bullets per subsystem}

### Implementation Points
{Phase 6 phased checklist}

### Examples
{Phase 7 I/O blocks}

### Review Notes
{Phase 8 non-blocking items + suggestions; "None" if all clear}

### Expansion metadata
- Date: {ISO_DATE}
- Model: {MODEL_ID}
- Approach selected: {APPROACH_ID}
- Blocking items resolved: {N}
```

Never overwrite the original **Problem**, **Approaches surveyed**, **Recommendation**, or **Open questions** sections.

---

## Gap-analysis mode

Activated when Phase 0 detects a locked design AND `AGAINST_DOC` is set. Replaces Phases 1–2 with gap-specific equivalents; Phases 3–9 run shared.

### Phase 0b — Load reference doc

Read `{AGAINST_DOC}`. Extract every cross-reference to the system described in `DOC_PATH`:

- Step/bucket exit gates that name the system
- Tier entry conditions gated on the system's deliverables
- Interface contracts the system must produce (YAML schemas, descriptor formats, archetype counts)
- Locked decisions in the umbrella that constrain the system's design

Assign each extracted requirement an id (`R1`, `R2`, …). Hold as **requirements list**.

### Phase 1g — Gap inventory (replaces Phase 1)

Compare each requirement against the current design in `DOC_PATH`. Build gap table:

| Req | Source (section + quote) | Current coverage | Gap severity |
|---|---|---|---|
| R1 | … | Present / Partial / Absent | Blocking / Additive / Deferred |

Emit as Markdown table. Hold in working memory; not yet persisted.

### Phase 2g — Confirm gate (replaces Phase 2)

Present gap table + short summary of confirmed new requirements. **Pause — ask user to confirm gaps, trim scope, or mark any as already-resolved before expanding.** Do NOT continue until confirmed.

After confirmation: treat confirmed gaps as the design scope for Phases 3–8.

### Phases 3–8 — Standard (gap-scoped)

Run Phases 3–8 as defined in standard mode, but scoped to the confirmed gaps:

- **Phase 3 Expand** — components = what's missing; interfaces = new contracts required; non-scope = what's already covered in existing design.
- **Phase 4 Architecture** — only if gaps introduce new components or change data flow. Skip if gaps are schema corrections or count changes.
- **Phase 5 Subsystem impact** — same tool recipe.
- **Phase 6 Implementation points** — one phase block per confirmed gap.
- **Phase 7 Examples** — schema examples for new contracts; step tables for structural gaps.
- **Phase 8 Subagent review** — same prompt template.

### Phase 9g — Persist (gap-analysis variant)

Derive a context title from `AGAINST_DOC` filename slug (e.g. `full-game-mvp-exploration.md` → `## Design Expansion — MVP Alignment`). When `AGAINST_DOC` is a master-plan slug instead of a doc path, use the slug directly (e.g. `full-game-mvp` → `## Design Expansion — MVP Alignment`).

- If an existing `## Design Expansion` block is present → append the new named section **after** it, separated by `---`. Do NOT overwrite the existing block.
- If no `## Design Expansion` block exists → append after `---` following the last existing section.
- Emit `### Plan Shape` block (same format as Phase 9 standard) at top of the appended Design Expansion section.

Never overwrite Problem / Approaches surveyed / Recommendation / Open questions / any prior Design Expansion block.

---

## Tool recipe (territory-ia) — Phase 3.5 + Phase 5

### Phase 3.5 recipe (enriched field grill driver)

Per design-explore run, batch the following MCP calls once at the start of Phase 3.5 + use returned data per-task during the grill polls:

1. **`glossary_lookup` (batched per task).** Per task in working memory, batch one `glossary_lookup({ keywords: [...digest_outline tokens, ...interface names, ...touched_paths basenames] })` call. Returned anchors fill `enriched.glossary_anchors[]` verbatim. No grill poll required — anchors are derived.
2. **`arch_decision_list` (once per run).** Single call: `arch_decision_list({ plan_slug: "{slug}" })` — returns DEC-A* rows scoped to this plan + global. Surface matching rows per task during the `decision_dependencies` grill poll.
3. **`csharp_class_summary` (batched per `.cs` path).** Per task, for each `.cs` path in `touched_paths`, call `csharp_class_summary({ path })`. Returns `{ loc, summary }`. Fills `enriched.touched_paths_with_preview[].loc` + `.summary` + `.kind` (`new` when LOC unresolvable; `extend` when LOC > 0 + path exists in `touched_paths`).
4. **Reuse for `before_after_code`.** Same `csharp_class_summary` output drives `enriched.before_after_code.before` — agent extracts ≤30 LOC excerpt of the target method/class from summary's body slice; drafts `.after` shape during the grill turn.

### Phase 5 recipe (subsystem impact)

**Composite-first call (MCP available):**

1. Call `mcp__territory-ia__glossary_discover({ query: "{approach domain keywords}", keywords: ["{component_name_1}", "{component_name_2}", ...] })` — first MCP call; no issue id yet at design-explore time. Returns glossary anchors + related terms for Phase 5 subsystem impact.
2. Proceed to `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)) with returned anchors as seed. Inputs: `keywords` = English tokens from selected approach components + Phase 3 interface names; `brownfield_flag = false` for designs touching existing subsystems; `tooling_only_flag = true` for tooling/pipeline-only designs. Use returned `glossary_anchors`, `router_domains`, `spec_sections`, `invariants` for Phase 5 subsystem impact table.

### Bash fallback (MCP unavailable)

1. Run `domain-context-load` subskill ([`ia/skills/domain-context-load/SKILL.md`](../domain-context-load/SKILL.md)). Inputs: `keywords` = English tokens from selected approach components + Phase 3 interface names; `brownfield_flag = false` for designs touching existing subsystems; `tooling_only_flag = true` for tooling/pipeline-only designs. Use returned `glossary_anchors`, `router_domains`, `spec_sections`, `invariants` for Phase 5 subsystem impact table.

---

## Guardrails

- IF approach not confirmed after Phase 2 → STOP; ask user before continuing
- IF subagent review returns BLOCKING items → resolve, then re-run Phase 8 before persisting
- IF `{DOC_PATH}` unreadable → stop, report path error; do not proceed
- IF touched subsystem spec unavailable via MCP → note gap in Subsystem Impact, continue
- Never commit changes — user decides when to commit the enriched doc

### DB read batching guardrail

Before issuing the first DB read, list every question needed for this phase. Batch into one `db_read_batch` MCP call OR one typed MCP slice (`catalog_panel_get`, `catalog_archetype_get`, `master_plan_state`, `task_bundle_batch`, `spec_section`). Sequential reads only when query N depends on result of N-1.

---

## Next step

After persist: if expansion validates, propose one of:

- **Standard mode** — `claude-personal "/ship-plan {DOC_PATH}"` (multi-stage) or `claude-personal "/project-new ..."` (single issue)
- **Gap-analysis mode** — `claude-personal "/ship-plan --version-bump {SLUG} {DOC_PATH}"` to absorb the filled gaps into the existing plan

---

## Relentless human polling (companion to §Plan Digest)

Pick-prevention is layered across design-explore → ship-plan. This skill is the upstream-most layer: poll the human question-by-question (one open question per turn, never a batch) until every decision is locked in the design doc BEFORE the master plan is compiled. Result: by the time `ship-plan` runs, zero picks remain; by the time §Plan Digest lint-scans for picks (`plan_digest_scan_for_picks`), leaks are exceptional. Leak = abort chain + route back to `/design-explore` (not a silent deferral).

See `ia/rules/plan-digest-contract.md` rubric point 1 (zero open picks) and `ia/skills/ship-plan/SKILL.md` lint gate.

---

## Changelog

- 2026-05-13 — feat: HTML-first uplift (design-explore-html-effectiveness-uplift plan). Phase 3.5 added — 8-field enriched grill (visual_mockup_svg, before_after_code, edge_cases, glossary_anchors, failure_modes, decision_dependencies, shared_seams, touched_paths_with_preview) per [`ia/rules/design-explore-output-schema.md`](../../rules/design-explore-output-schema.md). Tool recipe extended with Phase 3.5 sub-block (batch glossary_lookup + arch_decision_list + csharp_class_summary). Phase 9 rewritten — emits BOTH `{slug}.html` (canonical, embeds raw MD inside `<script id="rawMarkdown">`) + `{slug}.md` (derived sidecar). Renderer = `npm run design-explore:render-html {slug}` (script: `tools/scripts/render-design-explore-html.mjs`); template = `ia/templates/design-explore.html.template`. Round-trip extractor = `npm run design-explore:extract-md {slug}` (script: `tools/scripts/extract-exploration-md.mjs`). Hard boundaries extended — Stage 1 strict band; HTML + MD both written. Source: HTML-first uplift causal chain — enriched body_md flows into ship-plan Phase B prompt → ship-cycle Pass A spec-implementer transparently (ship-cycle preflight reads full body_md via `task_spec_body` MCP).
