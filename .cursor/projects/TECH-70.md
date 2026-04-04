# TECH-70 — UI-as-code umbrella maintenance & multi-scene UI traceability

> **Issue:** [TECH-70](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04 (**TECH-70** **§7** implementation pass)

> **Parent program:** [TECH-67](TECH-67.md) (**UI-as-code program** umbrella — **glossary** **UI-as-code program (TECH-67)**). **Out of scope:** [TECH-69](TECH-69.md) capstone (**theme** **`ScriptableObject`**, prefab library **v0**, **MainMenu** **Canvas** serialization, **`UIManager`** facades, critique **P1–P9**, **Editor** validate/scaffold in **TECH-69** **§5.2** / **Phase H**, **territory-ia** **MCP** tools for **theme** tokens).

<!--
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Use glossary terms: ../specs/glossary.md (spec wins if glossary differs).
-->

## 1. Summary

This issue tracks **TECH-67** work that is **not** owned by **TECH-69**: keeping the **umbrella** spec (**§4.4** **Codebase inventory**, **§4.6** **Backlog bridge**, **§4.9** open implementation threads), **`ui-design-system.md`** **as-built** prose, and **machine-readable** **UI** inventory **traceability** (**glossary** **UI design system (reference spec)**; **`ui-design-system.md`** — **Machine-readable traceability**) aligned with **Unity** scenes as they evolve. Scope includes **multi-scene** growth (**`RegionScene`**, **`CityScene`** rename) for **`UiInventoryReportsMenu`** (`Assets/Scripts/Editor/UiInventoryReportsMenu.cs`) and the reference spec, plus **regeneration** of [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) when **HUD** / **toolbar** / **popup** hierarchies change materially ([`docs/reports/README.md`](../../docs/reports/README.md)).

**IA routing:** **`.cursor/rules/agent-router.mdc`** — task domain **UI changes** → **`.cursor/specs/ui-design-system.md`**. **Editor** export behavior → **`unity-development-context.md`** **§10** (**Export UI Inventory (JSON)**).

### 1.1 Persistence and guardrails

- **Not Save data:** **UI** inventory JSON and spec edits are **not** **Load pipeline** artifacts — do not change on-disk **Save data** or **interchange** semantics (**`persistence-system.md`**). See **TECH-67** **§5.2** **Persistence boundary**.
- **Runtime C#:** Prefer **no** runtime **`Assets/Scripts/`** changes. If **`UiInventoryReportsMenu`** allowlist edits are required, keep scripts under **`Assets/Scripts/Editor/`** only; obey **`.cursor/rules/invariants.mdc`** (**no** `FindObjectOfType` in **`Update`**, **no** new singletons) if any non-Editor code is touched incidentally.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Umbrella IA:** Keep [`.cursor/projects/TECH-67.md`](TECH-67.md) **§4.4**, **§4.6**, and **§4.9** accurate vs repo **scenes**, **`UIManager.cs`**, and representative controllers listed there.
2. **Spec ↔ scene drift:** After material **UI** edits (or this audit), align **`ui-design-system.md`** **§1–§4** / **§2–§3** **as-built** tables and paths with shipped hierarchy; keep **`docs/reports/README.md`** refresh rules honest.
3. **Committed baseline:** Update [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) when allowlisted **UI** changes — source: **Territory Developer → Reports → Export UI Inventory (JSON)** (**Edit Mode**) or **`editor_export_ui_inventory.document`** (**Postgres**) per **§10** table (**unity-development-context**).
4. **Multi-scene export:** When **`RegionScene`** exists and/or **`MainScene`** renames to **`CityScene`**, update **`SceneAllowlist`** in **`UiInventoryReportsMenu`**, **`ui-design-system.md`** **Overview** / affected sections, **TECH-67** **§4.4** paths, and **BACKLOG** **Files** lines that cite scene assets — single cohesive PR preferred.
5. **Backlog hygiene:** After **`Spec:`** or `.cursor/projects/*.md` link edits in the **UI-as-code** program: `npm run validate:dead-project-specs` (repo root).
6. **Future UI issues:** When this pass touches **`ui-design-system.md`**, confirm **§5** (*Acceptance criteria per issue*) remains the template for new **UI** **BACKLOG** rows (**screens affected**, **Play Mode** checks, **Inspector** regression).

### 2.2 Non-Goals (Out of Scope)

1. **TECH-69** deliverables: **theme** **`ScriptableObject`**, prefab **v0**, **MainMenu** serialization strategy, **`UIManager`** facades, **modal** contract execution, **TMP** vs legacy **Text** **migration** execution, **Editor** menus in **TECH-69** **§5.2** / **Phase H**, new **MCP** tools for **theme** tokens.
2. **Player-facing** simulation or economy rules (**FEAT-**/**BUG-**) except **UI** copy paths explicitly scoped elsewhere.
3. **Full WCAG** / accessibility audit.
4. **Headless** **`batchmode`** **UI** tree validation (**TECH-67** **§5.5** seed) — optional follow-up **TECH-** row unless trivially added here without scope creep.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Maintainer | I want the **umbrella** inventory to match **scene** roots and key scripts after **UI** refactors. | **TECH-67** **§4.4** matches **`MainScene.unity`** / **`MainMenu.unity`** (or renamed paths) + listed managers/controllers. |
| 2 | IDE agent | I want **`spec_section`** **ui** slices and baseline **JSON** to agree on **ControlPanel** / **HUD** paths. | **`scenes[]`** **`path`** nodes (relative to **Canvas** root) match **`ui-design-system.md`** cited paths for sampled widgets after refresh. |
| 3 | Developer | I want a third **UI** scene in the export when **Region** **UI** lands. | Allowlist + spec subsection + baseline **`scenes[]`** entry when **`RegionScene`** asset exists. |

## 4. Current State

### 4.1 Domain behavior

**As-built** **UI** is documented in **`ui-design-system.md`** with a committed snapshot (**Machine-readable traceability**). **TECH-67** **Phase 1** (baseline) and **Phase 2** (**toolbar** in scene) are **shipped**. **TECH-69** owns the **capstone** improvement track; **TECH-70** owns **umbrella** upkeep and **traceability** only.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Umbrella | [`.cursor/projects/TECH-67.md`](TECH-67.md) **§4.4**, **§4.6**, **§4.9**, **§7** Phase **0** |
| Reference **UI** spec | `.cursor/specs/ui-design-system.md` — **Overview**, **§1–§4**, **§2–§3**, **§5**, **Machine-readable traceability** |
| **Editor** **Reports** | **`unity-development-context.md`** **§10**; [`UiInventoryReportsMenu.cs`](../../Assets/Scripts/Editor/UiInventoryReportsMenu.cs); **BUG-53** if menus fail |
| Baseline JSON | [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json); [`docs/reports/README.md`](../../docs/reports/README.md) |
| **Postgres** (optional) | [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) — **Editor export registry**, **`editor_export_ui_inventory`** |
| Capstone (excluded) | [`.cursor/projects/TECH-69.md`](TECH-69.md) |
| Sibling tooling | **TECH-33** — prefab/scene introspection (complements, does not replace **UI** inventory sampling) |

### 4.3 Implementation investigation notes

- **Audit §4.4:** Diff **TECH-67** **§4.4** **Primary entry points** / controller bullets against `Assets/Scripts/` paths; spot-check **`MainScene.unity`** / **`MainMenu.unity`** for **`UI/City/Canvas`**, **`ControlPanel`**, **`DataPanelButtons`** roots (YAML or **Unity** **Editor**).
- **JSON ↔ spec:** For **MainScene**, compare baseline **`canvases[0].nodes`** sample paths (e.g. **HUD** money text) to **§1.2** typography table and **§3.3** **toolbar** prose. **MainMenu:** **Edit Mode** export may show **`canvases: []`** when **Canvas** is runtime-only — **§3.0** remains authority (**`MainMenuController.BuildUI()`**).
- **CityScene rename:** One PR: **`UiInventoryReportsMenu.SceneAllowlist`**, **`ui-design-system.md`** **Overview** / **Related files**, **TECH-67** **§4.4**, **BACKLOG** **Files** rows under **UI-as-code** program, baseline JSON **`scene_asset_path`** / **`scene_name`** fields.
- **RegionScene:** Additive **`SceneAllowlist`** entry; new **`ui-design-system.md`** subsection (e.g. under **§3** patterns or **§6** revision note) when **Canvas** roots are known.

### 4.4 **TECH-67** **§4.9** resolution recipe (no silent drift)

Each thread must end in **one** of: (a) a **Decision Log** row here or in **TECH-67** **§6**, (b) a short **stub** in **`ui-design-system.md`** pointing to **TECH-69**, or (c) explicit **defer** to **TECH-69** with phase pointer.

| **§4.9** thread | Default under **TECH-70** | **TECH-69** anchor (if deferring) |
|-----------------|---------------------------|-------------------------------------|
| **TextMeshPro** vs legacy **Text** | **Defer** product policy + migration to **TECH-69** **Phase D** — add **Decision Log** row; optional one-line stub in **`ui-design-system.md`** **§1.2** (“policy **TBD** — **capstone**”) | **Phase D** — Typography |
| Minimum **Canvas Scaler** reference resolutions | **Defer** resolution matrix work to **TECH-69** **Phase E**; ensure **§4.3** table in **`ui-design-system.md`** still lists current **as-built** refs (**800×600** city, **1280×720** menu) | **Phase E** — **Canvas Scaler** |
| Optional **territory-ia** **UI** graph tool | **Defer**; document “**`router_for_task`** + **`spec_section`** **`ui`** sufficient until **BACKLOG** scopes **MCP**” in **Decision Log** | **Phase H** — optional **MCP** |

## 5. Proposed Design

### 5.1 Target behavior (product)

No change to **player-visible** game rules. **Documentation**, **Editor** export coverage, and **IA** links stay **per scene** and **glossary**-aligned.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- **Markdown-first:** **`TECH-67.md`**, **`ui-design-system.md`**, **`BACKLOG.md`** as needed.
- **Allowlist:** `static readonly string[] SceneAllowlist` in **`UiInventoryReportsMenu`** — repo-relative **`Assets/Scenes/*.unity`** strings; extend only when scenes exist to avoid export warnings.
- **Baseline refresh:** **Unity** **Edit Mode** → **Territory Developer → Reports → Export UI Inventory (JSON)** → copy committed snapshot to **`docs/reports/ui-inventory-as-built-baseline.json`** (or promote from **Postgres** **`document`** per [`docs/reports/README.md`](../../docs/reports/README.md)).
- **IA indexes:** If **`ui-design-system.md`** or **glossary** bodies change: `npm run generate:ia-indexes` from repo root, then `npm run generate:ia-indexes -- --check`.

### 5.3 Method / algorithm notes (optional)

Follow **TECH-67** **§5.4**: **git** tag/branch before large **`.unity`** rewrites; diff **`scenes[]`** vs spec tables; **Play Mode** spot-check **menu → city** when scaler/anchor rows change.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Split **umbrella** maintenance from **TECH-69** capstone | **TECH-69** holds **P1–P9** + kit/tooling; **TECH-70** holds **Phase 0** + **traceability** | Fold into **TECH-69** (rejected — scope creep) |
| 2026-04-04 | **§4.9** threads default to **defer** + **Decision Log** unless product wants early **stub** prose | Avoid duplicating **TECH-69** typography/scaler/**MCP** design in **TECH-70** | Pre-decide **TMP** policy here (rejected — capstone owns product choice) |
| 2026-04-04 | **Baseline JSON** aligned to **Postgres** export | **`editor_export_ui_inventory`** row **id=2** (**git** `392be1bb336ed02ee42e7d20b5bcd8239b16ccb4`, **exported_at_utc** `2026-04-04T21:41:06.4878600Z`); committed snapshot updated (**186** **MainScene** nodes — same tree as prior baseline, new timestamp) | — |
| 2026-04-04 | **`RegionScene.unity`** | **Missing** under **`Assets/Scenes/`**; **`SceneAllowlist`** unchanged — extend when the asset lands (**TECH-67** **§5.4** example **3**) | Add allowlist stub now (rejected — export warnings) |
| 2026-04-04 | **`MainScene`** → **`CityScene`** rename | Not performed; **BACKLOG** / allowlist / baseline still cite **`MainScene.unity`** | Rename without coordinated spec + **BACKLOG** edits (rejected) |

## 7. Implementation Plan

### Phase 1 — Umbrella inventory & bridge

- [x] Read **TECH-67** **§4.4**; verify **`UIManager.cs`**, **`CursorManager.cs`**, **`GameNotificationManager.cs`**, and **§4.4** controller table paths exist under `Assets/Scripts/`.
- [x] Spot-check **`Assets/Scenes/MainScene.unity`** for **`UI/City/Canvas`**, **`ControlPanel`**, **`DataPanelButtons`** (and **`MainMenu.unity`** for serialized **Canvas** if any).
- [x] Patch **TECH-67** **§4.4** for any renamed **GameObject** paths, missing controllers, or stale file paths — **no path drift**; added **`SampleScene.unity`** note (not on export allowlist).
- [x] Re-read **TECH-67** **§4.6**; confirm rows for **as-built**, **TECH-69**, **TECH-33**, **toolbar**, **TECH-70** match [`BACKLOG.md`](../../BACKLOG.md) **UI-as-code program** section.

### Phase 2 — Reference spec & **§4.9** threads

- [x] Apply **§4.4** resolution recipe: for each **TECH-67** **§4.9** bullet, add **§6** row and/or **`ui-design-system.md`** stub, or confirm **defer** text is already satisfied by this **kickoff** table.
- [x] Scan **`AGENTS.md`** and **`.cursor/rules/agent-router.mdc`** — if **UI** default route unchanged, note “no edit” in **§9** or **Decision Log**; if changed, patch per **TECH-67** **§8** bullet **3**.

### Phase 3 — Baseline JSON & **IA** indexes

- [x] **Human / Unity gate:** **Export UI Inventory** captured in **Postgres** (**row id=2**); baseline file **`exported_at_utc`** updated to match.
- [x] **Baseline JSON:** [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json) refreshed (timestamp **2026-04-04T21:41:06Z**; **186** nodes sampled).
- [x] **`ui-design-system.md`** body changed → `npm run generate:ia-indexes` && `npm run generate:ia-indexes -- --check` (**Phase 5**).

### Phase 4 — Multi-scene allowlist (conditional)

- [x] **`Glob`** / list `Assets/Scenes/` — **`RegionScene.unity`** **missing**; **defer** recorded in **§6**; allowlist unchanged.
- [x] If **`RegionScene`** **exists:** append path to **`SceneAllowlist`**; add **`ui-design-system.md`** subsection for **Region** / **map** **UI**; refresh baseline JSON. *(Skipped — asset not present.)*
- [x] If **`MainScene`** **renamed** to **`CityScene`:** update allowlist string, spec **Overview** / **Related files**, **TECH-67** **§4.4**, relevant **BACKLOG** **Files**, baseline JSON; run **`validate:dead-project-specs`** after **BACKLOG** edits. *(Skipped — rename not in this pass.)*

### Phase 5 — Verification & handoff

- [x] `npm run validate:dead-project-specs` (repo root).
- [x] Reference spec body changed → **`npm run generate:ia-indexes`** + **`--check`** (per **§7b**); ran **`npm run validate:all`** for **CI** parity (**dead-project-specs**, **`test:ia`**, **fixtures**, index **check**).
- [x] **`project-implementation-validation`** parity — **`validate:all`** completed (no **`glossary.md`** / **MCP** code edits this pass).

## 7b. Test Contracts

| **§8** acceptance bullet | Check type | Command or artifact | Notes |
|--------------------------|------------|---------------------|-------|
| **§8.1** **TECH-67** **§4.4** / **§4.6** / **§4.9** reviewed | Manual / diff | PR diff vs **TECH-67.md** | Includes **§4.9** **Decision Log** or stub/defer |
| **§8.2** **`ui-design-system.md`** **as-built** matches scenes | Manual | Spot-check + JSON sample paths | **MainMenu** **Edit Mode** empty **Canvas** caveat |
| **§8.3** Baseline JSON updated when hierarchy changed | Manual | `docs/reports/ui-inventory-as-built-baseline.json` in PR | **Unity** export or **Postgres** promotion |
| **§8.4** **RegionScene** / **CityScene** done or deferred | Manual | **§6** **Decision Log** row | Trigger documented |
| **§8.5** `validate:dead-project-specs` | Node | `npm run validate:dead-project-specs` | After **BACKLOG** / **`Spec:`** edits |
| **§8** (index hygiene) | Node | `npm run generate:ia-indexes -- --check` | When **§8.2** touched **glossary** or spec bodies feeding indexes |
| Full **IA** chain (if **glossary** / **MCP**-fed specs touched) | Node | `npm run validate:all` | [`.cursor/skills/project-implementation-validation/SKILL.md`](../skills/project-implementation-validation/SKILL.md) |

## 8. Acceptance Criteria

- [x] **TECH-67** **§4.4** / **§4.6** / **§4.9** reviewed and updated for known drift, or **§6** **Decision Log** records intentional **defer** / **stub** per **§4.4** recipe.
- [x] **`ui-design-system.md`** **as-built** sections match shipped scenes after any refresh performed under this issue — **prose** aligned (**§1.2** stub); **Unity** hierarchy unchanged this pass.
- [x] Committed **`ui-inventory-as-built-baseline.json`** updated when allowlisted **UI** hierarchies change during this work **or** **§6** records why refresh was skipped (no drift).
- [x] **RegionScene** / **CityScene** work completed **or** explicitly deferred in **§6** with trigger.
- [x] `npm run validate:dead-project-specs` passes for this issue’s **BACKLOG** / link edits (no **`Spec:`** path edits this pass).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | **`AGENTS.md`** / **`agent-router.mdc`** **UI** route | N/A | **No edit** — **UI changes** → **`ui-design-system.md`** unchanged |
| 2 | Baseline **JSON** refresh | **Postgres** export **id=2** (human); payload matched prior committed baseline except **`exported_at_utc`** | **§6** + **`ui-inventory-as-built-baseline.json`** updated; re-run export after next material **UI** hierarchy edit |

## 10. Lessons Learned

- _Fill at closure; migrate to **`ui-design-system.md`**, **TECH-67**, or **glossary** as needed._

## Open Questions (resolve before / during implementation)

None — **documentation**, **Editor** allowlist, and **IA** hygiene only; **player-facing** **game logic** belongs in **FEAT-**/**BUG-** specs (**`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`**).
