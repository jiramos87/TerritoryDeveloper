---
purpose: "Project spec for TECH-64 — Play Mode test harness + agent **/create-play-mode-test** workflow."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-64 — Play Mode test harness + agent **`/create-play-mode-test`** workflow

> **Issue:** [TECH-64](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04 ([`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) TECH-39 **deferred** **parity** — **UrbanCentroidService** **/** **AUTO** **capture** **scoped** **here**)

**Planned delivery split (this spec tracks the umbrella until children exist):**

1. **Infrastructure** — Unity **Play Mode** tests (**UTF**), **game**-specific bootstrap (scene / prefabs), stable **JSON** (or **Markdown**) **artifact** contract under **`tools/reports/`** (gitignored) for agent consumption — complements **unity-development-context** §10 (**Editor** diagnostics).
2. **MCP** — **`territory-ia`** tools and/or **`tools/`** **Node** helpers so an **IDE agent** can scaffold **ad-hoc** **Play Mode** checks, register a run, and read structured results without hand-copying **Console** text.
3. **Cursor Skill** — **`/create-play-mode-test`** (proposed slash command): recipe that chains **MCP** + scripts + **BACKLOG**/**spec** context to produce a minimal runnable **Play Mode** test and interpret output for **debug** with the agent in the IDE.

**Skill name (approved for final phase):** **`/create-play-mode-test`** — folder target **`ia/skills/create-play-mode-test/`** (kebab-case, consistent with **`project-new`**, **`project-spec-kickoff`**, etc.).

## 1. Summary

Ship a **Territory Developer**–specific **Play Mode** testing path in the **Unity Test Runner** plus an **agent-invocable** pipeline (scripts + **MCP** + **Skill**) so developers and **IDE agents** can add **ad-hoc** runtime checks, execute them in **Play Mode**, and return **glossary-aligned**, **machine-readable** snapshots for debugging (**grid**, **simulation tick**, **AUTO**, etc.). **Edit Mode** tests (e.g. **TECH-38** **`ComputeLibParityTests`**) remain the default for **pure** math; **Play Mode** covers **MonoBehaviour** lifecycle, scene state, and multi-frame **simulation** behavior.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Play Mode** **UTF** assembly(ies) and at least one **reference** test proving **game** context loads and writes a bounded **JSON** (or agreed **Interchange JSON** subset) to **`tools/reports/`** or integrates with **Editor export registry** patterns ([`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) §Editor export registry).
2. **Scaffolding** (templates / snippets) for **ad-hoc** tests: naming, folder layout, **`[UnityTest]`** patterns, teardown, and **invariants** (**HeightMap** ↔ **Cell.height**, **road** **cache**, **no** **`FindObjectOfType`** in **`Update`** in *new* test code per **invariants**).
3. **Agent-oriented** automation: **Phase 2** **MCP** tools (or documented **`npm run …`** steps) to create or locate a test stub and **Phase 3** **Skill** **`/create-play-mode-test`** with an ordered **Tool recipe** (aligned with **TECH-63** **§7b** culture).
4. Document overlap with **unity-development-context** §10 (**Territory Developer → Reports**) — **Play Mode** exports today are manual menus; this issue adds **repeatable** **UTF**-driven capture for **agents**.

### 2.2 Non-Goals

1. Replacing **Edit Mode** **golden** tests or **`tools/compute-lib`** parity ([`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) / unity-development-context §11 — open [`BACKLOG.md`](../../BACKLOG.md) **§ Compute-lib program**).
2. Player-facing test UI or shipping **Play Mode** tests in standalone builds (tests stay **Editor** / **test assemblies** only).
3. Full **CI** headless **Unity** runner in **Phase 1** (optional follow-up; note **license** / **runner** cost in **Decision Log** when scoped).
4. Defining new **Save data** **DTO** shapes unless a **FEAT-**/**BUG-** explicitly requires it (**persistence-system**).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I run a **Play Mode** test from **Test Runner** and get a **JSON** file the agent can `@`-attach. | Documented path + sample test merged. |
| 2 | IDE agent | I invoke **`/create-play-mode-test`** with a short **debug intent** and get a stub test + steps to run and read results. | **Skill** + **MCP** (or script) recipe **§7** **Phase 3** complete. |
| 3 | Maintainer | I can tell **infrastructure** vs **MCP** vs **Skill** PRs apart. | **Decision Log** + **Implementation Plan** phases match the three-part split; optional child **BACKLOG** rows when split. |

## 4. Current State

### 4.1 Domain behavior

- **Edit Mode** tests exist (**TECH-38** **`TerritoryDeveloper.EditModeTests`**) for **pure** **compute**; they do not exercise **Play Mode** **simulation** or scene-bound **managers**.
- **Editor** **Reports** menus (**unity-development-context** §10) export **Agent context**, **Sorting debug**, **Cell chunk**, **World snapshot** — **manual** triggers, not **UTF** **Play Mode** tests.
- **territory-ia** **MCP** does not yet scaffold **Play Mode** tests or ingest **UTF** result files as first-class tools.

### 4.2 Systems map

| Area | Pointers |
|------|----------|
| Unity **UTF** | `Packages/manifest.json` **com.unity.test-framework** |
| **Edit Mode** precedent | `Assets/Tests/EditMode/`, **`TerritoryDeveloper.Game`** asmdef |
| **Editor** diagnostics | `Assets/Scripts/Editor/AgentDiagnosticsReportsMenu.cs`, `InterchangeJsonReportsMenu.cs` |
| **IA / MCP** | `tools/mcp-ia-server/`, [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) |
| **Skills** | [`ia/skills/README.md`](../skills/README.md), **TECH-63** **§ Completed** patterns |

### 4.3 Implementation investigation notes

- **Ad-hoc** = generated or copied-from-template **per debug session**, not necessarily committed long-term; policy (commit vs **gitignored** scratch) — **Decision Log**.
- **Play Mode** **UTF** may need a **bootstrap** scene or **SceneManager** load of **`MainScene`** (or agreed test scene) — verify **GridManager** **`isInitialized`** timing vs **TECH-28** / **unity-development-context** §10 (**Editor** diagnostics) lessons.

## 5. Proposed Design

### 5.1 Target behavior (product)

**Tooling only** — no change to player **game rules**. Outputs are for **developers** and **agents** debugging **Territory Developer** behavior.

### 5.2 Architecture / implementation (phased)

| Phase | Focus | Outcomes (high level) |
|-------|--------|------------------------|
| **A — Infrastructure** | **Play Mode** asmdef(s), scene load, shared **test** **utilities**, first **JSON** **artifact** writer to **`tools/reports/play-mode-test-*.json`** (name **TBD**), English **XML** **`summary`** on public test helpers |
| **B — MCP + scripts** | **`territory-ia`** tool(s) and/or **`tools/`** **Node** CLI: template listing, optional patch application, “last run” JSON reader; **`npm run verify`** / **`test:ia`** when **MCP** code changes |
| **C — Skill** | **`ia/skills/create-play-mode-test/SKILL.md`**: **`/create-play-mode-test`** recipe (**`backlog_issue`** → **`invariants_summary`** when touching runtime → **`spec_section`** **unity** §10 → scaffold steps → **Play Mode** run instructions → attach **artifact**); register in [`ia/skills/README.md`](../skills/README.md) |

### 5.3 Method / algorithm notes

- Reuse **glossary** terms in **artifact** field names where possible (**HeightMap**, **Cell**, **simulation tick**, **AUTO**, **road stroke**, etc.).
- **Snake_case** for any new **MCP** tool names (**terminology-consistency**).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-04 | Umbrella issue **TECH-64** with **three** delivery parts (infra → MCP → Skill) | Matches user intent; allows parallel planning before child **BACKLOG** rows | Single mega-issue without phases |
| 2026-04-04 | Final **Skill** trigger **`/create-play-mode-test`** | Clear, action-oriented, consistent with **`/project-new`** style | **`/play-mode-test`**, **`/utf-play-debug`** |
| 2026-04-04 | **Computational** **MCP** **suite** **closed** — **UrbanCentroid** **/** **AUTO** **parity** **lives** **under** **TECH-64** | **Play** **Mode** **/** **bounded** **JSON** **is** **the** **right** **substrate** **for** **`UrbanCentroidService`** **/** **AUTO** **observable** **diffs** | **No** **TECH-39** **project** **spec** — **trace** [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) TECH-39 |

## 7. Implementation Plan

### Phase A — Infrastructure (**Play Mode** **UTF**)

- [ ] Add **`TerritoryDeveloper.PlayModeTests`** (or agreed name) **asmdef** referencing **`TerritoryDeveloper.Game`** + **TestAssemblies**; **Editor** platform rules per Unity docs.
- [ ] One **reference** **`[UnityTest]`** that enters **Play Mode**, loads **game** context (**scene** policy in **Decision Log**), waits for **`GridManager.isInitialized`** (or documents skip), writes bounded **JSON** under **`tools/reports/`** (gitignored pattern documented).
- [ ] Cross-link **unity-development-context** §10 in code comments or **`docs/`** pointer.
- [ ] ([`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) TECH-39 **deferred** **parity**) Optional follow-on test or menu-scripted capture: record minimal `UrbanCentroidService` (centroid, radius, optional ring sample cells) + one AUTO observable (e.g. road / zone counts TBD) after N sim ticks; store under `tools/reports/` for baseline diffs (document schema in Decision Log) — feeds **[TECH-32](../../BACKLOG.md)** research without changing gameplay defaults.

### Phase B — **MCP** + **Node** helpers

- [ ] Design 1–3 **MCP** tools (names **`snake_case`**) or document **`npm run …`** substitutes: e.g. list templates, emit stub file, read last **artifact**.
- [ ] Update [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) + **`tools/mcp-ia-server/README.md`**; **`npm run verify`** green.

### Phase C — **Skill** **`/create-play-mode-test`**

- [ ] Add **`ia/skills/create-play-mode-test/SKILL.md`** with **Tool recipe** (territory-ia order aligned with **project-spec-implement** / **TECH-63**).
- [ ] Update [`ia/skills/README.md`](../skills/README.md) index.
- [ ] Optional: root **`package.json`** script **`play-mode-test:…`** if **Node** glue is shared.

### Phase D — Split (optional)

- [ ] If scope grows, file **TECH-64a** / **TECH-64b** / **TECH-64c** and move **Implementation Plan** checklists; keep **TECH-64** as umbrella or close after children exist. (Do not reuse **[TECH-65](../../BACKLOG.md)** for UTF splits — that id is pathfinding / MCP v2 on the compute-lib program.)

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| **Play Mode** reference test | **Unity** **Test Runner** (Play Mode) | Manual + CI **TBD** | **Phase A** |
| **MCP** / parser changes | **Node** | **`npm run verify`** under **`tools/mcp-ia-server/`** | **Phase B** |
| **IA** index sources | **Node** | **`npm run validate:all`** | If **glossary** / **spec** / **MCP** docs change |

## 8. Acceptance Criteria

- [ ] **Phase A:** **Play Mode** test assembly + at least one committed test + documented **JSON** output path (includes optional centroid / AUTO capture when scoped — [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) TECH-39 deferred parity).
- [ ] **Phase B:** **MCP** and/or **Node** path documented; **`verify`** green when server code ships.
- [ ] **Phase C:** **`/create-play-mode-test`** **Skill** merged and listed in **Skills** **README**.
- [ ] **Invariants** and **unity-development-context** §10 **GridManager** access rules respected in **new** test / **Editor** code.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

**None — tooling only.** Policy choices (commit **ad-hoc** tests vs scratch-only, **CI** **Unity** runner, exact **artifact** **schema_version**) belong in **Implementation Plan**, **Decision Log**, or **§7b** — not **game logic**. If a test would assert on **Save data** format, open a **BUG-**/**FEAT-** and **persistence-system** alignment.
