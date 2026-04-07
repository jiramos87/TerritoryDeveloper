---
name: project-spec-implement
description: >
  Use when executing a .cursor/projects/{ISSUE_ID}.md Implementation Plan (shipping checklist phases),
  after the spec is ready—not for spec review. Triggers: "implement project spec", "execute project spec",
  "follow Implementation Plan", "ship spec phases", implement BUG-/FEAT-/TECH- project spec.
---

# Project spec implementation (execution)

This skill **does not** call MCP tools itself. In an **Agent** chat with **territory-ia** enabled, follow the **Tool recipe** below so context stays **slices**, not whole reference specs.

Until richer **MCP** discovery from project-spec prose ships, use the **manual** recipe (no composite MCP tool).

**Related:** **[`project-spec-kickoff`](../project-spec-kickoff/SKILL.md)** (review spec **before** code); **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)** (optional **Node** / **CI**-parity checks after **MCP** / schema / **IA index**–touching work); **[`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md)** (optional **Play Mode** logs/screenshots for **§7b** / **§8**); **[`project-spec-close`](../project-spec-close/SKILL.md)** (after verification — closeout / IA persistence / delete spec / **archive** / **id purge**). **Conventions:** [`.cursor/skills/README.md`](../README.md). Trace — [`BACKLOG-ARCHIVE.md`](../../../BACKLOG-ARCHIVE.md).

## Relationship to kickoff

- Use **[`project-spec-kickoff`](../project-spec-kickoff/SKILL.md)** when the spec needs **editorial** work: **Open Questions**, vague **Goals**, or glossary alignment **before** coding.
- Use **this** skill when the goal is to **execute** `## 7. Implementation Plan` in order with minimal diffs.
- After implementation is **verified** and you need to **migrate lessons**, update **glossary** / **reference specs**, **delete** the project spec, **remove** the **BACKLOG** row, **append** **archive**, **purge** ids — use **[`project-spec-close`](../project-spec-close/SKILL.md)**.

Default: spec **Status** is **Final** or **In Review** with game-logic **Open Questions** resolved. If the user insists on coding from **Draft** or unresolved **Open Questions**, state the risk in chat and prefer **kickoff** first.

## Seed prompt (parameterize)

Replace `{SPEC_PATH}` with the project spec path from the backlog **Spec:** line (`.cursor/projects/{ISSUE_ID}.md`). Use `{ISSUE_ID}` from the spec header `> **Issue:**` line when present.

```markdown
Implement @{SPEC_PATH} following its ## 7. Implementation Plan in order.
Use **territory-ia** in the sequence defined in **project-spec-implement**’s "Tool recipe (territory-ia)" (backlog_issue → invariants_summary when code → per-phase router_for_task → spec_section → glossary_*).
Honor **invariants** and **AGENTS.md** **Pre-commit Checklist**. If a phase touches **roads**, **water / HeightMap**, or **new managers**, follow the domain handoff to any shipped domain skills on [`BACKLOG.md`](../../../BACKLOG.md).
Update the project spec **Decision Log** / **Issues Found** when you discover gaps; do not change agreed game behavior without spec owner alignment.
```

## Tool recipe (territory-ia) — implementation session

Run **in order**. Repeat steps **5–12** for each **Implementation Plan** phase (or each coherent batch of checkboxes).

1. **Parse target** — Load `{SPEC_PATH}` (`@` attach or `read_file`). Extract **`ISSUE_ID`** from `> **Issue:**`.

2. **`backlog_issue`** — If `ISSUE_ID` is known, call with `issue_id` to pull **Files**, **Notes**, **Depends on**, **Acceptance**, and **`depends_on_status`**. If any **`depends_on_status`** entry has **`satisfied`: false** and **`soft_only`** false, **stop** and surface unsatisfied hard dependencies unless the user explicitly overrides.

3. **`invariants_summary`** — **Once** per session if **any** phase can touch runtime **C#** or scene behavior. **Skip** only for pure doc/IA deliverables (no game code in any phase).

4. **Phase intent** — State which plan checkboxes are in scope; list files/classes from the plan + backlog **Files**.

5. **Domain routing** — From phase text + **Files**, list **1–3** domains. For each, **`router_for_task`** with `domain` matching **`.cursor/rules/agent-router.mdc`** table labels (e.g. `Road logic, placement, bridges`, `Water, terrain, cliffs, shores`, `Save / load`, `Unity / MonoBehaviour`). If **`router_for_task`** returns **`no_matching_domain`** or weak matches, retry with **`files`** using repo-relative paths from the backlog **Files** line (**glossary** **territory-ia spec-pipeline layer B**).

6. **`spec_section`** — For each routed spec, fetch **only** sections the phase needs; set **`max_chars`**. **Do not** read entire `.cursor/specs/*.md` unless **`spec_outline`** forces it.

7. **`glossary_discover`** — When terms are ambiguous; pass **`keywords` as a JSON array**; **English** only (translate from chat if needed).

8. **`glossary_lookup`** — Narrow with exact term strings from discover results or the glossary table.

9. **`spec_outline`** / **`list_specs`** — **Only** if the `spec` key for **`spec_section`** is unknown.

10. **Implement** — Minimal diff; obey **invariants** and guardrails (e.g. **road preparation family**, **`InvalidateRoadCache()`**, **HeightMap** ↔ **`Cell.height`**, no **`GridManager`** bloat).

11. **Optional deep guardrails** — **`list_rules`** / **`rule_content`** if **`invariants_summary`** is not enough.

12. **Phase exit** — Re-read **§8 Acceptance** (and **§7b Test Contracts** if present) for the completed phase; run applicable **`AGENTS.md`** **Pre-commit Checklist** (Unity build, XML docs, English logs, domain checks). If **§7b** lists **IDE agent bridge** checks and the session has **territory-ia** + **Postgres** + **Unity** on **REPO_ROOT**, optionally run **`unity_bridge_command`** per **[`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md)** and attach **`log_lines`** / **`artifact_paths`** to chat or the issue. If the phase touched **`tools/mcp-ia-server`**, **`docs/schemas`**, **`.cursor/specs/glossary.md`**, **reference spec** bodies that feed **IA indexes**, or committed **`tools/mcp-ia-server/data/*-index.json`**, run **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)** (or **`npm run validate:all`** from repo root) before starting the next phase. Record surprises in the project spec **§9 Issues Found During Development**.

### Phase rollback

If a phase fails verification, revert the phase’s commits (e.g. **`git revert`** / **`git stash`**) or restore files, document the failure in **§9 Issues Found**, then re-run the phase after fixing the root cause ([`projects/spec-pipeline-exploration.md`](../../../projects/spec-pipeline-exploration.md) **§2.4**).

### Editor / agent diagnostics

When a phase involves **sorting**, **grid** sampling, or **Edit Mode** vs **Play Mode**, use **`unity-development-context`** **§10** (**Territory Developer → Reports** → **`tools/reports/`** exports). Attach generated paths in chat; artifacts are **gitignored** by policy.

### IDE agent bridge (optional Play evidence)

For phases that change **in-game** or **HUD** behavior, after **Play Mode** verification you may collect **Console** excerpts or **Game view** PNGs via **`unity_bridge_command`** (**`get_console_logs`**, **`capture_screenshot`**, **`include_ui: true`** for **Overlay** UI). Prerequisites and limits: **[`ide-bridge-evidence`](../ide-bridge-evidence/SKILL.md)**. This is **optional** and **dev-machine-only**; it does not replace **Node** validation.

### Branching (minimum set) — during implementation

Mirror **project-spec-kickoff** so domains get the right slices:

- **Roads / streets / interstate / bridge / wet run** → **roads-system** + **isometric-geography-system** via **`router_for_task`** + **`spec_section`**.
- **Water / HeightMap / shore / river / lake / water map** → **water-terrain-system** + relevant **geo** sections.
- **JSON / schema / DTO / interchange** (**Save**-adjacent) → **persistence-system** (**Load pipeline**, **Save data**); do **not** change on-disk **Save data** unless the issue requires it; cross-check **JSON interchange program** notes when applicable.

## Domain skill handoff

When work enters these areas, open the corresponding skill (**when shipped** on [`BACKLOG.md`](../../../BACKLOG.md)) instead of pasting spec text:

- **Roads** / **wet run** / **bridges**
- **Terrain / water / shore / HeightMap**
- **New MonoBehaviour manager / service**

## Spec maintenance during implementation

- Non-obvious scope or product choices → project spec **§6 Decision Log**.
- Defects or surprises → **§9 Issues Found During Development**.
- Code would **change** agreed game behavior → stop; update spec or ask owner ([**PROJECT-SPEC-STRUCTURE**](../../projects/PROJECT-SPEC-STRUCTURE.md)).

## Completion and backlog

Map work to the project spec **§8 Acceptance** and the backlog **Acceptance** line. **Do not** **archive** the issue (remove from `BACKLOG.md`, append `BACKLOG-ARCHIVE.md`, **id purge**) without **explicit user confirmation** ([`AGENTS.md`](../../../AGENTS.md)).

When the diff is **IA**-heavy (**MCP**, **fixtures**, **glossary** / **reference spec** sources for indexes), run or document **[`project-implementation-validation`](../project-implementation-validation/SKILL.md)** so **CI**-aligned **Node** checks are not skipped.
