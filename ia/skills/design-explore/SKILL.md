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
  {DOC_PATH} [APPROACH_HINT] [--against REFERENCE_DOC] [--force-model {model}] (e.g. docs/foo.md C OR
  docs/foo.md --against docs/full-game-mvp-exploration.md)
model: inherit
reasoning_effort: high
tools_role: planner
tools_extra:
  - Agent
  - mcp__territory-ia__spec_outline
  - mcp__territory-ia__list_specs
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
caller_agent: design-explore
---

# Design exploration — expand, review, persist

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

No MCP calls from skill body. Follow **Tool recipe** below (Phase 5 only) — all other phases derive from the exploration doc itself.

**Position in lifecycle:** fires _before_ master plan creation or `project-new`.  
`design-explore` → `master-plan-new` → `stage-file` → `stage-authoring` → `project-spec-implement`.

**Related:** [`project-new`](../project-new/SKILL.md) · [`master-plan-new`](../master-plan-new/SKILL.md) · [`master-plan-extend`](../master-plan-extend/SKILL.md) · [`stage-file`](../stage-file/SKILL.md) · [`ia/skills/README.md`](../README.md).

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

### Phase 1 — Compare

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

**Polling shape (4 sequential AskUserQuestion turns — one question per turn per `agent-human-polling.md`; each poll carries plain-language preface + `Recommended:` line per the same rule):**

1. **Decision slug** — kebab-case, prefixed `DEC-A{N}` where `{N}` = next free in `arch_decisions`. Designer/player wording in question stem ("How should we name this design choice?"); slug rendered in option label.
2. **Rationale** — ≤250 chars (DEC-A17 row budget). Single short paragraph explaining trade-off rationale.
3. **Alternatives considered** — ≤3 entries, semicolon-separated. Names of approaches NOT chosen + why.
4. **Affected `arch_surfaces[]`** — list of slugs from `arch_surfaces` (e.g. `layers/full-dependency-map`, `interchange/agent-ia`). Implementer derives candidate list from Phase 3 component → spec_path mapping; user confirms / trims via multi-select.

**MCP writes (after polling) — shape-branched:**

Read `plan_shape` from working memory (set in Phase 0 plan-shape gate).

**When `plan_shape=carcass+section`** — seed 3 plan-scoped decision rows (slug-shape contract: `plan-{slug}-{suffix}` where `{slug}` = exploration doc slug; `{suffix}` ∈ `boundaries` / `end-state-contract` / `shared-seams`). Write in order:

1. `arch_decision_write({ slug: "plan-{slug}-boundaries", plan_slug: "{slug}", title: "Plan {slug} — Boundaries", summary: "{one-line from working memory}", status: "locked" })`
2. `arch_decision_write({ slug: "plan-{slug}-end-state-contract", plan_slug: "{slug}", title: "Plan {slug} — End-State Contract", summary: "{one-line from working memory}", status: "locked" })`
3. `arch_decision_write({ slug: "plan-{slug}-shared-seams", plan_slug: "{slug}", title: "Plan {slug} — Shared Seams", summary: "{one-line from working memory}", status: "locked" })`

Then:

4. `arch_changelog_append({ kind: 'design_explore_decision', decision_slug: "plan-{slug}-boundaries", body, commit_sha: null })`
5. `arch_drift_scan({ open_plans_only: true })`

**When `plan_shape=flat`** — legacy single DEC-A15 path (unchanged):

1. `arch_decision_write({ slug: "architecture-lock-{slug}", title, rationale, alternatives, surface_slugs[], status: 'active' })`
2. `arch_changelog_append({ kind: 'design_explore_decision', decision_slug, body, commit_sha: null })`
3. `arch_drift_scan({ open_plans_only: true })`

**Drift report render target:** append to exploration doc under sibling section `### Architecture Decision` (peer of `### Architecture` block authored in Phase 4 → §Persist). Block contains: decision row summary + rendered drift report (per-plan breakdown).

**Stop condition:** if any MCP write fails → stop, surface error, do NOT continue to Phase 3 / 4. User must reconcile DB state before re-running.

### Phase 3 — Expand

Detail the selected approach:

- **Components** — list each with a one-line responsibility
- **Data flow** — inputs → processing steps → outputs (linear or branching)
- **Interfaces / contracts** — key function signatures, file formats, event boundaries, CLI flags
- **Non-scope** — explicit list of what this approach does NOT do

### Phase 4 — Architecture

Emit both:

1. **Mermaid diagram** (`flowchart LR` or `graph TD`) — component relationships, data flow, external boundaries
2. **Entry / exit points** — where callers invoke this system and what they receive back

If Mermaid would exceed 20 nodes, produce ASCII block + a simplified Mermaid showing only top-level components.

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

### Phase 9 — Persist

Detect whether `## Design Expansion` section already exists in `{DOC_PATH}`:
- **Exists** → update content in place between that header and the next `---` separator.
- **Not present** → append after a `---` separator following the last existing section.

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

## Tool recipe (territory-ia) — Phase 5 only

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

---

## Next step

After persist: if expansion validates, propose one of:

- **Standard mode** — `claude-personal "/master-plan-new {DOC_PATH}"` (multi-stage) or `claude-personal "/project-new ..."` (single issue)
- **Gap-analysis mode** — `claude-personal "/master-plan-extend {SLUG} {DOC_PATH}"` to absorb the filled gaps into the existing plan

---

## Relentless human polling (companion to §Plan Digest)

Pick-prevention is layered across design-explore → stage-authoring. This skill is the upstream-most layer: poll the human question-by-question (one open question per turn, never a batch) until every decision is locked in the design doc BEFORE the master plan is compiled. Result: by the time `stage-authoring` runs, zero picks remain; by the time §Plan Digest lint-scans for picks (`plan_digest_scan_for_picks`), leaks are exceptional. Leak = abort chain + route back to `/design-explore` (not a silent deferral).

See `ia/rules/plan-digest-contract.md` rubric point 1 (zero open picks) and `ia/skills/stage-authoring/SKILL.md` lint gate.
