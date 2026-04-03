---
name: project-spec-kickoff
description: >
  Use when reviewing, tightening, or enriching a .cursor/projects/{ISSUE_ID}.md project spec before
  writing code—especially for BUG-/FEAT-/TECH- work, JSON or infra program specs (e.g. TECH-40),
  or when aligning vocabulary with the glossary. Triggers include "kickoff spec", "review project spec",
  "enrich TECH-xx.md", "canonical terms audit", "Implementation Plan too vague", "pre-implementation spec pass".
---

# Project spec kickoff and IA alignment

This skill **does not** call MCP tools itself. In an **Agent** chat with **territory-ia** enabled, follow the **Tool recipe** below in order so context is loaded as **slices**, not whole reference specs.

Until **TECH-48** ships richer discovery from project-spec prose, use the **manual** recipe (no composite MCP tool).

**Related:** **TECH-48** (MCP discovery improvements), **TECH-45** / **TECH-46** / **TECH-47** (domain guardrail skills), **TECH-49** — **[`project-spec-implement`](../project-spec-implement/SKILL.md)** (shipped), **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)** (**TECH-52** completed — optional **npm** checks after **MCP** / schema / **IA index** work), **[`project-spec-close`](../project-spec-close/SKILL.md)** (when the issue is done — persist IA, delete spec, **BACKLOG** **Completed**), **TECH-23** (MCP preflight culture). **Conventions:** [`.cursor/skills/README.md`](../README.md).

**When the issue is verified and you are closing:** use **[`project-spec-close`](../project-spec-close/SKILL.md)** after implementation — not this kickoff skill.

## Seed prompt (parameterize)

Replace `{SPEC_PATH}` with the project spec path (e.g. `.cursor/projects/TECH-41.md`). Use `{ISSUE_ID}` from the spec header `> **Issue:**` line when present.

```markdown
Review @{SPEC_PATH} and ensure it uses canonical terms from the glossary and reference specs.
Analyze stated goals; avoid negatively affecting current subsystems unless the spec explicitly accepts tradeoffs.
Make ## 7. Implementation Plan more concrete where possible.
Follow the MCP tool sequence in this skill's "Tool recipe (territory-ia)" section (do not skip steps unless the spec is tooling-only and cannot touch game subsystems).
If you make material edits, update related Information Architecture: linked project specs, glossary rows, and reference spec sections so implementation stays aligned.
```

## Tool recipe (territory-ia)

Run these steps **in order** unless the project spec is explicitly **pure doc hygiene** with no code or subsystem touch (then skip only the steps noted).

1. **Parse target** — Load `{SPEC_PATH}` (user `@` attach or `read_file`). Extract **`ISSUE_ID`** from the `> **Issue:**` line (e.g. `TECH-40`, `BUG-48`).

2. **`backlog_issue`** — If `ISSUE_ID` is known, call with `issue_id` to pull **Files**, **Notes**, **Spec**, **Depends on**, **Acceptance** into context.

3. **`invariants_summary`** — Call **once** per review session if the spec implies **code** or **game subsystem** changes. Skip only when the spec is strictly documentation/IA hygiene and cannot affect runtime.

4. **Domain routing** — From **Summary**, **Goals**, backlog **Files**, and **Notes**, list **1–3 domains** (e.g. roads, water, simulation, Save / load, UI). For each domain, call **`router_for_task`** with `domain` set to a string that matches the **agent-router** table vocabulary (e.g. `Road logic, placement, bridges`, `Save / load`, `Water, terrain, cliffs, shores`).

5. **`spec_section`** — For each routed reference spec, fetch **only** the sections the project spec implies (by **section** id, heading substring, or slug per MCP docs). Use **`max_chars`** to cap size. **Do not** read entire `.cursor/specs/*.md` files unless **`spec_outline`** shows you cannot target sections otherwise.

6. **`glossary_discover`** — Pass **`keywords` as a JSON array** of **English** tokens extracted from ambiguous prose (translate from the user's language first). Run **after** domain hints so keywords are **not** generic (`MCP`, `information`, `agent` alone). Example: `["HeightMap", "schema_version", "Load pipeline", "road preparation"]`.

7. **`glossary_lookup`** — For high-confidence **Term** strings from the glossary table or discover results, narrow with exact **`glossary_lookup`** calls.

8. **`spec_outline`** / **`list_specs`** — Use **only** if you do not know which `spec` key to pass to **`spec_section`**.

### Branching (minimum set)

- **Roads / streets / interstate / bridge / wet run** → ensure **roads-system** and **isometric-geography-system** slices (validation, **road stroke**, path costs) appear in the fetched set via **`router_for_task`** + **`spec_section`**.
- **Water / HeightMap / shore / river / lake / water map** → **water-terrain-system** + relevant **geo** sections.
- **JSON / schema / artifact / DTO / interchange** (especially **Save**-adjacent) → **persistence-system** (**Load pipeline**, **Save data** semantics); do **not** change on-disk **Save data** unless the issue explicitly requires it. Cross-check **TECH-21** program notes in **BACKLOG** when applicable.

After MCP slices, perform the **editorial** pass: **Open Questions**, **Implementation Plan** phases, **Decision Log**, and cross-links to sibling `.cursor/projects/*.md`.

## Open Questions policy (project specs)

Under **`## Open Questions (resolve before / during implementation)`** in `.cursor/projects/*.md`:

- Use **canonical game vocabulary** from **glossary** / reference specs only.
- Ask about **game logic** and definitions—not APIs, class names, or implementation mechanics.
- **Tooling-only** issues: state that Open Questions are **N/A** or point to **Acceptance** / **Decision Log** per [.cursor/projects/PROJECT-SPEC-STRUCTURE.md](../../projects/PROJECT-SPEC-STRUCTURE.md).

## Follow-up skills (planned)

- **TECH-45** — Road modification guardrails.
- **TECH-46** — Terrain / **HeightMap** / water / shore guardrails.
- **TECH-47** — New **MonoBehaviour** manager wiring.

Use **this** skill first for **spec quality**; use **[`project-spec-implement`](../project-spec-implement/SKILL.md)** to run the **Implementation Plan** when the spec is ready; use **TECH-45** / **TECH-46** / **TECH-47** domain skills when **implementing** in those areas.
