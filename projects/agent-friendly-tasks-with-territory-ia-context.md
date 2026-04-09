# Agent-friendly tasks with the current territory-ia context system

**Purpose:** Classify **backlog work** and **new task ideas** by how likely an AI agent is to implement them **successfully** using today’s repo IA (specs, rules, `BACKLOG.md`, and **territory-ia** MCP tools)—with **low regression risk** and **minimal need for tight human oversight**.

**Audience:** Developers delegating work to agents; agents planning safe scope.

**Related:** [`docs/agent-tooling-verification-priority-tasks.md`](../docs/agent-tooling-verification-priority-tasks.md) (integrated MCP/script/Unity verification task order), [`docs/mcp-ia-server.md`](../docs/mcp-ia-server.md), [`AGENTS.md`](../AGENTS.md).

---

## 1. What the context system gives today

### 1.1 territory-ia MCP (file-backed)

Ten tools: `backlog_issue`, `list_specs`, `spec_outline`, `spec_section`, `glossary_discover`, `glossary_lookup`, `router_for_task`, `invariants_summary`, `list_rules`, `rule_content`.

**Strengths for implementation:**

- **Cheap, structured retrieval** of spec slices and glossary terms (English-only for glossary tools).
- After **TECH-40** lands: generated **spec** / **glossary** index JSON (**JSON program (TECH-21)** **§ Completed** — [`BACKLOG.md`](../BACKLOG.md)) can narrow which file to open before chaining `spec_section` — same MCP tools, fewer blind full-file reads.
- **`invariants_summary`** — hard constraints agents must not violate (HeightMap sync, road cache, no `FindObjectOfType` in per-frame loops, road preparation family, etc.).
- **`router_for_task`** — points to the right spec file/section family for a domain keyword.
- **`backlog_issue`** — exact issue block (files, notes, dependencies) when the id is known.

**Limits (sources of agent failure if ignored):**

- **No semantic search across all specs** in one call (agents must chain `spec_section` / `router_for_task` manually). Multi-domain bugs (geography + roads + sorting) stay **high coordination cost**.
- **No backlog discovery by keyword** (only `backlog_issue` by id). Agents without the right id may miss dependencies.
- **No Unity/scene/prefab truth** — Inspector wiring, `MainScene.unity`, and asset references are **not** in MCP; Unity validation remains human- or CI-driven.
- **Stale parse cache** — after large doc edits, prefer targeted file read or MCP server restart (per `AGENTS.md`).

### 1.2 In-repo guardrails

`.cursor/rules/invariants.mdc`, `agent-router.mdc`, `coding-conventions.mdc`, and `ARCHITECTURE.md` complement MCP. Agents that **skip** `invariants_summary` before touching roads, water, height, or `GridManager` are much more likely to introduce regressions.

---

## 2. Rubric: “agent-friendly” vs “needs strong human pairing”

| Signal | More agent-friendly | Less agent-friendly |
|--------|---------------------|---------------------|
| **Blast radius** | Single class or 2–3 files, no new public contracts | Many managers, save format, tick order |
| **Spec coverage** | Behavior described in one spec “column” + glossary terms | Requires stitching geo + roads + simulation + persistence |
| **Invariants** | Touches few invariant hotspots (no road prep, no water shore, no sorting formula) | Roads, `HeightMap`/`Cell.height`, shore band, `InvalidateRoadCache`, load restore |
| **Verification** | Compile + obvious runtime check | Requires long AUTO sim, art alignment, profiling |
| **Backlog clarity** | Notes include proposed approach or exact symptom | “Investigate regression suspicion” across AUTO pipeline |

**Default:** Treat anything involving **road preparation family**, **water/shore/cliff**, **sorting order**, **simulation tick order**, or **save/load restore** as **medium–high risk** unless the task is extremely narrow and spec-cited.

---

## 3. Backlog items — suggested agent suitability

Labels:

- **A — Good agent fit:** Mechanical or well-bounded; MCP + backlog text usually suffice; Unity check still recommended.
- **B — Agent with checklist:** Feasible if the agent systematically calls `invariants_summary`, `router_for_task`, and relevant `spec_section` slices; human verifies in Editor.
- **C — Human-led or split:** High coupling, balance, or regression surface; agent may assist (analysis, drafts) but should not own merge without review.

### 3.1 High priority (`BACKLOG.md`)

| ID | Suitability | Rationale |
|----|-------------|-----------|
| ~~**BUG-12**~~ | **Done** | Fixed: happiness display now reads `cityStats.happiness`. See [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md). |
| **BUG-14** | **A** | Directly matches invariant “no FindObjectOfType in Update”; cache in `Awake`/`Start`. Two files, mechanical. |
| **BUG-28** | **C** | Sorting between slope and interstate — `GridManager` sorting region + terrain/road interaction; easy to break visuals. |
| **BUG-31** | **C** | `RoadPrefabResolver` / border entry-exit; road validation and prefab rules. |
| **BUG-20** | **B–C** | Load/visual restore for multi-cell utilities; persistence and sorting interplay; verify after related completed bugs. |
| **TECH-01** | **C** | GridManager decomposition forbidden to grow `GridManager`; requires architecture discipline and Unity regression passes. |

### 3.2 Medium priority (sample)

| ID | Suitability | Rationale |
|----|-------------|-----------|
| **BUG-49** | **C** | Preview vs full stroke; must stay aligned with road validation / `TryPrepareRoadPlacementPlan` family. |
| **BUG-48** | **B–C** | Minimap refresh strategy; performance trade-offs; may need profiling script (see [`docs/agent-tooling-verification-priority-tasks.md`](../docs/agent-tooling-verification-priority-tasks.md)). |
| **BUG-52** | **C** | AUTO zoning gaps, tick order, road cache invalidation; regression suspicion after BUG-37. |
| **BUG-16**, **BUG-17** | **B** | Init order / null camera; localized but timing bugs need Play mode verification. |
| **FEAT-03** | **B** | Forest hold-to-place; input + `ForestManager` pattern; less core than roads/water. |
| **FEAT-22–23**, **FEAT-36**, **FEAT-43**, **FEAT-35** | **C** | Economy loops, AUTO expansion, growth rings, area bulldoze — design and multi-system effects. |
| **TECH-15**, **TECH-16** | **C** | Performance without mandatory harness in repo; agent risks guessing optimizations. |

### 3.3 Code health

| ID | Suitability | Rationale |
|----|-------------|-----------|
| **TECH-13** | **B–C** | Removal of obsolete urbanization proposal code; must not break saves/scenes; audit references. |
| **TECH-02** | **B** | Repetitive encapsulation; **high prefab/scene breakage risk** — agent can draft, human should open Unity. |
| **TECH-03** | **B** | Constants extraction; tedious but can touch many files; avoid behavior change. |
| **TECH-04** | **B–C** | Enforces `GetCell`; subtle if any path assumed direct array semantics. |
| **TECH-05** | **B** | Pattern duplication; wide sweep, moderate review cost. |
| **Toolbar / ControlPanel** | **C** | Scene hierarchy + layout; MCP has UI spec slices (**`ui-design-system.md`** **§3.3**) but Unity is source of truth. |
| **TECH-14** | **A** | Stub cleanup **if** reference search confirms no scene/scripts depend on them. |

### 3.4 Low priority / new systems

**FEAT-09–16**, **FEAT-18–19**, **FEAT-39–41**, **AUDIO-01**, **ART-01–04** → **C** (new systems, assets, or large design surface). Agents may **prototype** or **document** subtasks, but production merge without human/Unity review is risky.

**TECH-18**–**TECH-20**, **JSON program (TECH-21)** **§ Completed** (**TECH-40**–**TECH-41** **§ Completed**, **TECH-44a** **§ Completed** — [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md), **glossary**), **TECH-44** program (**TECH-44b**/**c**; **TECH-53**/**TECH-54** backlog-only), **TECH-43** → **B–C** depending on milestone; **unity-development-context.md** (**TECH-20** + **TECH-25** completed) remains a strong **B** target for *small, slice-sized* doc follow-ups if new Unity conventions appear.

---

## 4. New task ideas optimized for the *current* MCP (not future TECH-18/19)

These are **incremental** work items that improve agent success **without** waiting for Postgres or `search_specs`.

**TECH-23**–**TECH-27** are open under [**Code Health (technical debt)** in `BACKLOG.md`](../BACKLOG.md#code-health-technical-debt). Mapping:

| Idea (below) | Issue |
|--------------|--------|
| Invariant preflight | [**TECH-23**](../BACKLOG.md#code-health-technical-debt) |
| MCP parser tests/fixtures | [**TECH-24**](../BACKLOG.md#code-health-technical-debt) |
| `unity-development-context.md` slice milestones | [**TECH-25**](../BACKLOG.md#completed-last-30-days) (**completed** 2026-04-02; umbrella **TECH-20**) |
| Scripted mechanical checks | [**TECH-26**](../BACKLOG.md#code-health-technical-debt) |
| Backlog glossary alignment | [**TECH-27**](../BACKLOG.md#code-health-technical-debt) |

1. **“Invariant preflight” issue template** — For any BUG/FEAT, require first comment: paste output plan referencing `invariants_summary` + `router_for_task` + at least one `spec_section` call. Reduces forgotten road/height rules. **→ [TECH-23](../BACKLOG.md#code-health-technical-debt)**
2. **Narrow MCP-only regressions** — Extend `tools/mcp-ia-server` tests/fixtures when parsers change (already pattern from FEAT-45); no Unity. **→ [TECH-24](../BACKLOG.md#code-health-technical-debt)**
3. **`unity-development-context.md` slices** — Optional future polish (lifecycle, SerializeField policy, `FindObjectOfType` policy, execution order) in small PRs; baseline milestones shipped with [**TECH-25** completed](../BACKLOG.md#completed-last-30-days). New slices stay **A**–**B** if they only touch this spec and `coding-conventions.mdc` vocabulary.
4. **Scripted checks** (see [`docs/agent-tooling-verification-priority-tasks.md`](../docs/agent-tooling-verification-priority-tasks.md) task 1) — e.g. `FindObjectOfType` inside `Update` scanner, optional `gridArray` gate: **A/B** for agents in Node/shell; lowers human vigilance for mechanical violations. **→ [TECH-26](../BACKLOG.md#code-health-technical-debt)**
5. **Backlog cross-links** — Ensure “Depends on” and Spec fields use glossary terms; human or agent **A** task, improves `backlog_issue` usefulness. **→ [TECH-27](../BACKLOG.md#code-health-technical-debt)**

---

## 5. Practical workflow for a low-friction agent run

1. **`backlog_issue`** for the target id (or read `BACKLOG.md` if MCP off).
2. **`invariants_summary`** always before code edits.
3. **`router_for_task`** with a short domain string; then **`spec_outline`** / **`spec_section`** for the listed spec keys (avoid full-file reads when slices exist).
4. **`glossary_discover`** (English keywords) when naming or writing notes so terminology matches [`glossary.md`](../.cursor/specs/glossary.md).
5. After roads/water changes: explicitly verify **`InvalidateRoadCache()`**, **`RefreshShoreTerrainAfterWaterUpdate`** (when applicable), and HeightMap/cell sync per invariants.
6. **Human/Unity:** Play mode + scene/prefab diff for anything touching Inspector, layout, or initialization order.

---

## 6. Summary

- **Best backlog candidates for a mostly-autonomous agent today:** **BUG-14**, **TECH-14** (with reference audit), and **documentation/tooling** follow-ups [**TECH-23**](../BACKLOG.md#code-health-technical-debt)–[**TECH-27**](../BACKLOG.md#code-health-technical-debt) (see [§4](#4-new-task-ideas-optimized-for-the-current-mcp-not-future-tech-1819)). **`unity-development-context.md`** baseline polish: [**TECH-25** completed](../BACKLOG.md#completed-last-30-days).
- **Reasonable with discipline and Editor verification:** **BUG-17**, **FEAT-03**, parts of **TECH-02/03/05**, **TECH-13** (audited).
- **Poor fit for “hands-off” agent implementation:** **BUG-28**, **BUG-31**, **BUG-49**, **BUG-52**, **TECH-01**, **FEAT-22–23**, **FEAT-35–36**, **FEAT-43**, performance (**TECH-15/16**), and **new gameplay systems**—unless the human splits the work into verified milestones with spec anchors.

---

*Document type: planning / agent workflow. Created 2026-04-02. Update as TECH-18/19 land and MCP gains search and dependency tools.*
