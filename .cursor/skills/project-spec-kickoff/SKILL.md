---
name: project-spec-kickoff
description: >
  Use when reviewing, tightening, or enriching a .cursor/projects/{ISSUE_ID}.md project spec before
  writing code—especially for BUG-/FEAT-/TECH- work, JSON or infra program specs,
  or when aligning vocabulary with the glossary. Triggers include "kickoff spec", "review project spec",
  "enrich TECH-xx.md", "canonical terms audit", "Implementation Plan too vague", "pre-implementation spec pass".
---

# Project spec kickoff and IA alignment

This skill **does not** call MCP tools itself. In an **Agent** chat with **territory-ia** enabled, follow the **Tool recipe** below in order so context is loaded as **slices**, not whole reference specs.

Until richer **MCP** discovery from project-spec prose ships, use the **manual** recipe (no composite MCP tool).

**Related:** **[`project-spec-implement`](../project-spec-implement/SKILL.md)**, **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)** (optional **npm** checks after **MCP** / schema / **IA index** work), **[`project-spec-close`](../project-spec-close/SKILL.md)** (verified close — persist IA, delete spec, **archive** row, **id purge**). Open follow-ups — [`BACKLOG.md`](../../../BACKLOG.md). **Conventions:** [`.cursor/skills/README.md`](../README.md).

**When the issue is verified and you are closing:** use **[`project-spec-close`](../project-spec-close/SKILL.md)** after implementation — not this kickoff skill.

## Seed prompt (parameterize)

Replace `{SPEC_PATH}` with the project spec path from the backlog **Spec:** line (`.cursor/projects/{ISSUE_ID}.md`). Use `{ISSUE_ID}` from the spec header `> **Issue:**` line when present.

```markdown
Review @{SPEC_PATH} and ensure it uses canonical terms from the glossary and reference specs.
Analyze stated goals; avoid negatively affecting current subsystems unless the spec explicitly accepts tradeoffs.
Make ## 7. Implementation Plan more concrete where possible.
For **FEAT-** / **BUG-** specs, ensure ## 7b. Test Contracts maps **§8 Acceptance** to verifiable checks (see `.cursor/templates/project-spec-template.md`).
Follow the MCP tool sequence in this skill's "Tool recipe (territory-ia)" section (do not skip steps unless the spec is tooling-only and cannot touch game subsystems).
If you make material edits, update related Information Architecture: linked project specs, glossary rows, and reference spec sections so implementation stays aligned.
```

## Tool recipe (territory-ia)

Run these steps **in order** unless the project spec is explicitly **pure doc hygiene** with no code or subsystem touch (then skip only the steps noted).

1. **Parse target** — Load `{SPEC_PATH}` (user `@` attach or `read_file`). Extract **`ISSUE_ID`** from the `> **Issue:**` line (e.g. `FEAT-44`, `BUG-48`).

2. **`backlog_issue`** — If `ISSUE_ID` is known, call with `issue_id` to pull **Files**, **Notes**, **Spec**, **Depends on**, **Acceptance**, and **`depends_on_status`** into context. If **`depends_on_status`** includes any entry with **`satisfied`: false** and **`soft_only`** false (hard dependency not met), **stop** and surface it to the user unless they explicitly override in chat.

3. **`invariants_summary`** — Call **once** per review session if the spec implies **code** or **game subsystem** changes. Skip only when the spec is strictly documentation/IA hygiene and cannot affect runtime.

4. **Domain routing** — From **Summary**, **Goals**, backlog **Files**, and **Notes**, list **1–3 domains** (e.g. roads, water, simulation, Save / load, UI). For each domain, call **`router_for_task`** with `domain` set to a string that matches the **agent-router** table vocabulary (e.g. `Road logic, placement, bridges`, `Save / load`, `Water, terrain, cliffs, shores`). If **`router_for_task`** returns **`no_matching_domain`** or weak matches, retry with **`files`** using repo-relative paths from the backlog **Files** line (**glossary** **territory-ia spec-pipeline layer B**).

5. **`spec_section`** — For each routed reference spec, fetch **only** the sections the project spec implies (by **section** id, heading substring, or slug per MCP docs). Use **`max_chars`** to cap size. **Do not** read entire `.cursor/specs/*.md` files unless **`spec_outline`** shows you cannot target sections otherwise.

6. **`glossary_discover`** — Pass **`keywords` as a JSON array** of **English** tokens extracted from ambiguous prose (translate from the user's language first). Run **after** domain hints so keywords are **not** generic (`MCP`, `information`, `agent` alone). Example: `["HeightMap", "schema_version", "Load pipeline", "road preparation"]`.

7. **`glossary_lookup`** — For high-confidence **Term** strings from the glossary table or discover results, narrow with exact **`glossary_lookup`** calls.

8. **`spec_outline`** / **`list_specs`** — Use **only** if you do not know which `spec` key to pass to **`spec_section`**.

### Branching (minimum set)

- **Roads / streets / interstate / bridge / wet run** → ensure **roads-system** and **isometric-geography-system** slices (validation, **road stroke**, path costs) appear in the fetched set via **`router_for_task`** + **`spec_section`**.
- **Water / HeightMap / shore / river / lake / water map** → **water-terrain-system** + relevant **geo** sections.
- **JSON / schema / artifact / DTO / interchange** (especially **Save**-adjacent) → **persistence-system** (**Load pipeline**, **Save data** semantics); do **not** change on-disk **Save data** unless the issue explicitly requires it. Cross-check **JSON interchange program** notes in **BACKLOG** when applicable.

### Impact preflight (optional)

Lightweight check before deep editorial work ([`projects/spec-pipeline-exploration.md`](../../../projects/spec-pipeline-exploration.md) **§2.1**):

1. Classify backlog **Files** (and planned **Implementation Plan** paths) as **read** vs **write**.
2. For each **write** path that may touch runtime **C#** or scenes, plan to call **`invariants_summary`** (if not already done) and cross-check **`.cursor/rules/invariants.mdc`** guardrails.
3. Flag cross-subsystem edits (e.g. **roads** + **HeightMap** / **water**) so **`spec_section`** pulls both domains before implementation.

After MCP slices, perform the **editorial** pass: **Open Questions**, **Implementation Plan** phases, **Decision Log**, and cross-links to sibling `.cursor/projects/*.md`.

## Open Questions policy (project specs)

Under **`## Open Questions (resolve before / during implementation)`** in `.cursor/projects/*.md`:

- Use **canonical game vocabulary** from **glossary** / reference specs only.
- Ask about **game logic** and definitions—not APIs, class names, or implementation mechanics.
- **Tooling-only** issues: state that Open Questions are **N/A** or point to **Acceptance** / **Decision Log** per [.cursor/projects/PROJECT-SPEC-STRUCTURE.md](../../projects/PROJECT-SPEC-STRUCTURE.md).

## Follow-up skills (planned)

Domain guardrail skills (roads, terrain/water, new managers) — see [`BACKLOG.md`](../../../BACKLOG.md).

Use **this** skill first for **spec quality**; use **[`project-spec-implement`](../project-spec-implement/SKILL.md)** to run the **Implementation Plan** when the spec is ready; use any shipped domain skills from **BACKLOG** when **implementing** in those areas.
