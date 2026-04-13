---
purpose: "Use before a master plan or backlog issue exists: survey approaches in an exploration doc, select one, expand with architecture + subsystem impact + implementation points + examples, review with a subagent, and persist back to the same doc."
audience: agent
loaded_by: skill:design-explore
slices_via: router_for_task, spec_sections, invariants_summary
name: design-explore
description: >
  Use when an exploration doc (under docs/) needs to move from fuzzy survey to a defined,
  detailed, reviewed design ready to seed a master plan or BACKLOG issue. Phases: compare
  approaches → select → expand → architecture → subsystem impact → implementation points →
  examples → subagent review → persist. Triggers: "/design-explore [path]", "expand exploration",
  "design review [doc]", "turn this exploration into a design", "compare and select approach",
  "take this exploration doc to a master plan".
---

# Design exploration — expand, review, persist

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

No MCP calls from skill body. Follow **Tool recipe** below (Phase 5 only) — all other phases derive from the exploration doc itself.

**Position in lifecycle:** fires _before_ master plan creation or `project-new`.  
`design-explore` → master plan (`ia/projects/{slug}-master-plan.md`) → `stage-file` → `project-new` → `project-spec-kickoff` → `project-spec-implement`.

**Related:** [`project-new`](../project-new/SKILL.md) · [`stage-file`](../stage-file/SKILL.md) · [`project-spec-kickoff`](../project-spec-kickoff/SKILL.md) · [`ia/skills/README.md`](../README.md).

---

## Inputs

| Parameter | Source | Notes |
|-----------|--------|-------|
| `DOC_PATH` | User prompt | Path to exploration `.md` — required |
| `APPROACH_HINT` | User prompt | Override recommendation (e.g. `"C"`) — skips Phase 2 gate |

---

## Phase sequence (gated — each phase depends on the previous)

### Phase 0 — Load

Read `{DOC_PATH}`. Extract and hold in working memory:

- **Problem statement** — hard constraints the design must satisfy
- **Approaches list** — id, name, pros/cons/effort for each
- **Existing recommendation** — if present
- **Open questions** — if present

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

Phased checklist ordered by dependency. Format:

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

### Chosen Approach
{approach id + name + one-paragraph rationale referencing Phase 1 criteria}

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

## Tool recipe (territory-ia) — Phase 5 only

Run in order. Skip `invariants_summary` for tooling/pipeline-only designs that touch no runtime C#.

1. **`glossary_discover`** — `keywords` JSON array: English tokens from selected approach components + Phase 3 interface names.
2. **`glossary_lookup`** — high-confidence terms from discover.
3. **`router_for_task`** — 1–3 domains matching agent-router table vocabulary; derive from component responsibilities.
4. **`spec_sections`** — sections implied by touched subsystems; set `max_chars`. No full spec reads.
5. **`invariants_summary`** — if approach touches runtime C# / Unity game subsystems.

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

- **Master plan** — create `ia/projects/{slug}-master-plan.md` for multi-stage work (most expansions)
- **Single issue** — invoke [`project-new`](../project-new/SKILL.md) for designs narrow enough to fit one backlog issue
