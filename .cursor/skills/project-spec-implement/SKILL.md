---
name: project-spec-implement
description: >
  Use when executing a .cursor/projects/{ISSUE_ID}.md Implementation Plan (shipping checklist phases),
  after the spec is ready—not for spec review. Triggers: "implement project spec", "execute TECH-xx spec",
  "follow Implementation Plan", "ship spec phases", implement BUG-/FEAT-/TECH- project spec.
---

# Project spec implementation (execution)

This skill **does not** call MCP tools itself. In an **Agent** chat with **territory-ia** enabled, follow the **Tool recipe** below so context stays **slices**, not whole reference specs.

Until **TECH-48** ships richer discovery from project-spec prose, use the **manual** recipe (no composite MCP tool).

**Related:** **TECH-49** — completed (`BACKLOG.md` § Completed); **TECH-44** / **project-spec-kickoff** (review spec **before** code); **TECH-48** (future MCP discovery); **TECH-23** (MCP preflight culture); **TECH-45** / **TECH-46** / **TECH-47** (domain guardrail skills when shipped). **Conventions:** [`.cursor/skills/README.md`](../README.md).

## Relationship to kickoff

- Use **[`project-spec-kickoff`](../project-spec-kickoff/SKILL.md)** when the spec needs **editorial** work: **Open Questions**, vague **Goals**, or glossary alignment **before** coding.
- Use **this** skill when the goal is to **execute** `## 7. Implementation Plan` in order with minimal diffs.

Default: spec **Status** is **Final** or **In Review** with game-logic **Open Questions** resolved. If the user insists on coding from **Draft** or unresolved **Open Questions**, state the risk in chat and prefer **kickoff** first.

## Seed prompt (parameterize)

Replace `{SPEC_PATH}` with the project spec path (e.g. `.cursor/projects/TECH-40.md`). Use `{ISSUE_ID}` from the spec header `> **Issue:**` line when present.

```markdown
Implement @{SPEC_PATH} following its ## 7. Implementation Plan in order.
Use **territory-ia** in the sequence defined in **project-spec-implement**’s "Tool recipe (territory-ia)" (backlog_issue → invariants_summary when code → per-phase router_for_task → spec_section → glossary_*).
Honor **invariants** and **AGENTS.md** **Pre-commit Checklist**. If a phase touches **roads**, **water / HeightMap**, or **new managers**, follow the domain handoff to **TECH-45** / **TECH-46** / **TECH-47** skills when available.
Update the project spec **Decision Log** / **Issues Found** when you discover gaps; do not change agreed game behavior without spec owner alignment.
```

## Tool recipe (territory-ia) — implementation session

Run **in order**. Repeat steps **5–11** for each **Implementation Plan** phase (or each coherent batch of checkboxes).

1. **Parse target** — Load `{SPEC_PATH}` (`@` attach or `read_file`). Extract **`ISSUE_ID`** from `> **Issue:**`.

2. **`backlog_issue`** — If `ISSUE_ID` is known, call with `issue_id` to pull **Files**, **Notes**, **Depends on**, **Acceptance**.

3. **`invariants_summary`** — **Once** per session if **any** phase can touch runtime **C#** or scene behavior. **Skip** only for pure doc/IA deliverables (no game code in any phase).

4. **Phase intent** — State which plan checkboxes are in scope; list files/classes from the plan + backlog **Files**.

5. **Domain routing** — From phase text + **Files**, list **1–3** domains. For each, **`router_for_task`** with `domain` matching **`.cursor/rules/agent-router.mdc`** table labels (e.g. `Road logic, placement, bridges`, `Water, terrain, cliffs, shores`, `Save / load`, `Unity / MonoBehaviour`).

6. **`spec_section`** — For each routed spec, fetch **only** sections the phase needs; set **`max_chars`**. **Do not** read entire `.cursor/specs/*.md` unless **`spec_outline`** forces it.

7. **`glossary_discover`** — When terms are ambiguous; pass **`keywords` as a JSON array**; **English** only (translate from chat if needed).

8. **`glossary_lookup`** — Narrow with exact term strings from discover results or the glossary table.

9. **`spec_outline`** / **`list_specs`** — **Only** if the `spec` key for **`spec_section`** is unknown.

10. **Implement** — Minimal diff; obey **invariants** and guardrails (e.g. **road preparation family**, **`InvalidateRoadCache()`**, **HeightMap** ↔ **`Cell.height`**, no **`GridManager`** bloat).

11. **Optional deep guardrails** — **`list_rules`** / **`rule_content`** if **`invariants_summary`** is not enough.

12. **Phase exit** — Re-read touched **Acceptance** bullets; run applicable **`AGENTS.md`** **Pre-commit Checklist** (Unity build, XML docs, English logs, domain checks).

### Editor / agent diagnostics

When a phase involves **sorting**, **grid** sampling, or **Edit Mode** vs **Play Mode**, use **`unity-development-context`** **§10** (**Territory Developer → Reports** → **`tools/reports/`** exports). Attach generated paths in chat; artifacts are **gitignored** by policy.

### Branching (minimum set) — during implementation

Mirror **project-spec-kickoff** so domains get the right slices:

- **Roads / streets / interstate / bridge / wet run** → **roads-system** + **isometric-geography-system** via **`router_for_task`** + **`spec_section`**.
- **Water / HeightMap / shore / river / lake / water map** → **water-terrain-system** + relevant **geo** sections.
- **JSON / schema / DTO / interchange** (**Save**-adjacent) → **persistence-system** (**Load pipeline**, **Save data**); do **not** change on-disk **Save data** unless the issue requires it; cross-check **TECH-21** program notes when applicable.

## Domain skill handoff

When work enters these areas, open the corresponding skill (**when shipped**) instead of pasting spec text:

- **Roads** / **wet run** / **bridges** → **TECH-45**
- **Terrain / water / shore / HeightMap** → **TECH-46**
- **New MonoBehaviour manager / service** → **TECH-47**

## Spec maintenance during implementation

- Non-obvious scope or product choices → project spec **§6 Decision Log**.
- Defects or surprises → **§9 Issues Found During Development**.
- Code would **change** agreed game behavior → stop; update spec or ask owner ([**PROJECT-SPEC-STRUCTURE**](../../projects/PROJECT-SPEC-STRUCTURE.md)).

## Completion and backlog

Map work to the project spec **§8 Acceptance** and the backlog **Acceptance** line. **Do not** move the issue to **Completed** in `BACKLOG.md` without **explicit user confirmation** ([`AGENTS.md`](../../../AGENTS.md)).
