# TECH-67 — UI-as-code program (umbrella)

> **Issue:** [TECH-67](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04 (**umbrella** maintenance pass **§ Completed** — [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) Recent archive; **TECH-69** capstone — **§7** Phase **5**)

> **Parent program:** This row is the umbrella. **As-built baseline (shipped):** **`ui-design-system.md`** documents **shipped** **UI** with a committed machine snapshot at **`docs/reports/ui-inventory-as-built-baseline.json`**. Further work (**runtime UI kit**, **Editor** / agent tooling, **TECH-69** capstone) builds on that baseline. **Program charter**, **codebase inventory**, and **roadmap** below replace the retired **`projects/ui-as-code-exploration.md`** workbook.

<!--
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Use glossary terms: ../specs/glossary.md (spec wins if glossary differs).
-->

## 1. Summary

Territory Developer will treat **in-game UI** (Canvas / uGUI / TextMeshPro, **HUD**, **menus**, **panels**, **toolbars**) as a system that **developers and IDE agents** can reason about, generate, and refactor **primarily from the repository** — with **canonical patterns** in **`.cursor/specs/ui-design-system.md`**, optional **runtime C#** building blocks, **Unity Editor** automation, **CLI** / **`batchmode`** hooks where justified, and **Cursor Skills** (and optionally **territory-ia** tools) that encode safe recipes. **UI** is **not** limited to the **city** view: **`MainMenu.unity`**, **`MainScene.unity`** (may rename to **`CityScene`**), and future scenes (e.g. **`RegionScene`**) each carry **Canvas** hierarchies that the program must document and export **per scene** (see **`ui-design-system.md`** — **Machine-readable traceability**; scene allowlist in **`Assets/Scripts/Editor/UiInventoryReportsMenu.cs`**). The goal is to **minimize ad-hoc manual Editor steps** while staying aligned with **Unity**’s standard authoring model (Prefabs, Scenes, Inspector).

**Baseline:** The reference spec describes the **shipped** **UI** (**as-built**), not only aspirational **TBD** rows — **glossary** **UI design system (reference spec)**; refresh **`docs/reports/ui-inventory-as-built-baseline.json`** when **UI** hierarchies change materially.

### 1.1 Information architecture and vocabulary

- **Glossary:** **UI-as-code program (TECH-67)** (this umbrella), **UI design system (reference spec)** (`ui-design-system.md`). Player-facing systems named in **UI** copy still use gameplay terms (**Game notification**, **street**, etc.) per **`ui-design-system.md`** **Overview** → **Domain vocabulary**.
- **Agent routing:** **`.cursor/rules/agent-router.mdc`** — task domain **UI changes** → **`.cursor/specs/ui-design-system.md`**. With **territory-ia** enabled, prefer **`router_for_task`** (`domain`: `UI changes`) then **`spec_section`** on spec key **`ui`** (headings / section ids) instead of loading whole reference specs.
- **New UI backlog rows:** Follow **`ui-design-system.md`** **§5** (*Acceptance criteria per issue*): cite spec **section**, screens affected, **Play Mode** checks (including scroll vs **camera** where relevant — **`ui-design-system.md`** **§3.5**), and **Inspector** regression for **`UIManager`** / controllers.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **As-built spec fidelity:** **`ui-design-system.md`** documents **current** **colors**, **typography**, **spacing**, **layout**, **Canvas** settings, and major **UX** surfaces **across all shipped UI scenes** (**MainMenu**, **city** view, future **Region**, etc.), backed by the **UI** inventory export + committed baseline JSON.
2. **Spec authority (post-baseline):** Extend the same spec with **target** patterns where backlog issues (**TECH-69**, etc.) define future layout; keep **as-built** **vs** **target** explicit.
3. **Code-first workflows:** Land a **Territory Developer UI kit** direction (namespaces, assembly layout, prefab conventions) so new screens can be built from **C#** + **YAML** / **Prefab** diffs in the IDE (**child** issue **TBD**).
4. **Tooling parity:** **Editor** scripts, repo **tools/** helpers, and **Skills** so agents follow the same steps a human would — including validation and **machine-readable** summaries where useful (compare **`unity-development-context.md`** §10 pattern) (**child** issue **TBD**).
5. **Program structure:** Track **child** issues in **BACKLOG** + **`.cursor/projects/{ISSUE_ID}.md`**; keep **§4.4** **Codebase inventory** in this spec updated when **Canvas** hierarchies or key scripts change.

### 2.2 Non-Goals (Out of Scope)

1. Replacing **Unity UI** with a third-party stack or custom renderer.
2. Defining **player-facing game rules** unrelated to presentation (those stay in gameplay specs and **FEAT-**/**BUG-** issues).
3. **CI** **Unity** test runner integration as a **Phase 0** requirement (optional follow-up per child issue).
4. Shipping **MCP** tools before **child** issues scope them (umbrella may list candidates only).

### 2.3 Code guardrails (when child issues touch runtime **UI** **C#**)

Umbrella issues stay mostly documentation, but **Phase 3** and future children may edit **`UIManager`**, controllers, or scenes. Align with **`.cursor/rules/invariants.mdc`** and project patterns:

- **No `FindObjectOfType` in `Update` or per-frame loops** — cache in `Awake` / `Start` (see **BUG-14** class).
- **No new singletons** — use **Inspector** + **`[SerializeField] private`** + **`FindObjectOfType` fallback in `Awake`** when adding manager references; **`GameNotificationManager`** is an existing project singleton — do not add parallel globals for **UI**.
- **New `MonoBehaviour` managers** remain scene components (**never `new`**).

**Editor** automation for exports belongs under **`Assets/Scripts/Editor/`** and must not ship in player builds — mirror **`unity-development-context.md`** **§10** (agent diagnostics pattern).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want **documented UI patterns** so that refactors stay consistent across **HUD** and **menus**. | **`ui-design-system.md`** sections referenced by **BACKLOG** / **Skills** cover **as-built** and **target** where applicable. |
| 2 | IDE agent | I want **`spec_section`** to return **real** layout and typography, not **TBD**. | **`ui-design-system.md`** **as-built** baseline + **`docs/reports/ui-inventory-as-built-baseline.json`** current. |
| 3 | Maintainer | I want **program notes** and **child** specs so phases have clear boundaries. | **TECH-67** **§4.4–§4.9** + open **BACKLOG** children (**TECH-69**, etc.). |
| 4 | Developer | I want **Territory Developer** to help me add a new panel or button. | **Skill** + optional **MCP** — **child** issue **TBD** |

## 4. Current State

### 4.1 Domain behavior

**`ui-design-system.md`** carries **as-built** **§1–§4** and major **§2–§3** (baseline shipped); **target** rows and **§1.4**/**§1.5** may stay light until **TECH-69** lands **theme** / prefab **v0**. The game has substantial **uGUI** across **`MainMenu`**, **`MainScene`** / future **`CityScene`**, etc. **City** **toolbar** (**`ControlPanel`**) is **as-built** in **`MainScene.unity`** (**§3.3**). **Program closeout:** [TECH-69](TECH-69.md) implements critique **P1–P9** + tooling (**Phase 5** above).

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Reference UI spec | `.cursor/specs/ui-design-system.md` |
| **As-built** baseline | **`ui-design-system.md`** + **`docs/reports/ui-inventory-as-built-baseline.json`** |
| Toolbar (**ControlPanel**) | **`ui-design-system.md`** **§3.3**; inventory **§4.4** |
| Prefab / scene introspection | **TECH-33** |
| Editor diagnostics pattern | `.cursor/specs/unity-development-context.md` §10 |
| **Multi-scene UI** | **`UiInventoryReportsMenu`** allowlist + **`scenes[]`** JSON — extend when **`RegionScene`** / **`CityScene`** rename lands |
| **BACKLOG `Spec sections` mirror** | **`ui-design-system.md`** — **§1** **Foundations**, **§3** patterns (**toolbar** / layout), **§4** **Canvas** / **Canvas Scaler**; **`unity-development-context.md`** **§10** when **Editor** **Reports** / exports overlap |
| **As-built critique (planning)** | [`docs/ui-as-built-critique-TECH-67.md`](../docs/ui-as-built-critique-TECH-67.md) — gaps vs goals; **execution** → [TECH-69](TECH-69.md) (**capstone**) |
| **Program capstone** | [TECH-69](TECH-69.md) — **UI improvements using UI-as-code** (**P1–P9** + **Editor** / **Skill** / optional **MCP** per **§5.2**/**§5.4** of that spec) |

### 4.3 Implementation investigation notes (optional)

- **Unity** APIs: `GameObject` + `RectTransform` construction in **Editor** vs runtime factories; **Prefab** variant strategy; **Canvas Scaler** reference resolutions.
- **Agent** ergonomics: whether **YAML** scene diffs or **Editor** menu commands reduce merge pain compared to manual hierarchy edits.
- **Overlap** with **TECH-59** (**EditorPrefs** staging) only if UI tooling needs **registry**-style workflows.

### 4.4 Codebase inventory and constraints (uGUI)

*Migrated from the retired **`projects/ui-as-code-exploration.md`** workbook. **Scene** object names and **Inspector** wiring can drift — verify in **Unity** when updating **as-built** docs or refactors. Update this subsection when hierarchies or roles change.*

**Stack:** **Unity UI (uGUI)** — **Canvas**, **Graphic** (**Image**, **Text** / **TMP**, etc.), **EventSystem**. Primary orchestrator: **`UIManager.cs`** (`Territory.UI`) with many serialized references to texts, images, **popups**, and tool state. **`CursorManager`**, **`GameNotificationManager`**, and **UnitControllers** handle focused interactions.

**Architectural placement** (see also **`ARCHITECTURE.md`**): **UI layer** — **`UIManager`**, **`CursorManager`**, **`GameNotificationManager`**, controllers. **Input** — **`GridManager`** and others gate world input when the pointer is over UI (**`IsPointerOverGameObject`**); scroll vs camera is a recurring UX area (**BACKLOG**).

**Primary entry points**

| File | Role |
|------|------|
| `Assets/Scripts/Managers/GameManagers/UIManager.cs` | Main **HUD**, **popups** (**PopupType**: load game, details, building selector, stats, tax), **toolbar** / zone and tool selection, demand visualization |
| `Assets/Scripts/Managers/GameManagers/CursorManager.cs` | Cursor state with tools and UI |
| `Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs` | **Game notification** path (**singleton**, `DontDestroyOnLoad`) |

**Controllers (representative)** — **`GameControllers/`**: **`CameraController.cs`** (zoom vs UI scroll), **`CityStatsUIController.cs`**, **`MiniMapController.cs`**. **`UnitControllers/`**: **`BuildingSelectorMenuController`**, **`DetailsPopupController`**, **`DataPopupController`**, **`GrowthBudgetSlidersController`**, **`SpeedButtonsController`**, **`*SelectorButton.cs`**, **`MiniMapLayerButton`**, **`ShowStatsButton`**, **`ShowTaxes`**, **`SimulateGrowthToggle`**, etc. Other managers feed **HUD** data (**`StatisticsManager`**, **`EconomyManager`**, **`TimeManager`**) without owning every widget.

**City scene and `ControlPanel`:** Primary layout **`Assets/Scenes/MainScene.unity`** (or future **`CityScene.unity`**). **City** **Canvas** root in scene: **`UI/City/Canvas`** (**Screen Space Overlay**; **Canvas Scaler** reference **800×600** in **UI** inventory export). **`ControlPanel`**: **left**-docked **vertical** construction **toolbar** (category rows, **horizontal** tool groups per row); wired via **`UIManager`** and **`UnitControllers/*SelectorButton.cs`**. **Normative layout:** **`ui-design-system.md`** **§3.3**. **`SampleScene.unity`** also lives under **`Assets/Scenes/`** (default **Unity** template); it is **not** on **`UiInventoryReportsMenu`** **`SceneAllowlist`** — **as-built** docs and the committed baseline cover **MainScene** + **MainMenu** only.

**Main menu scene:** **`Assets/Scenes/MainMenu.unity`** — scene YAML may contain **no** serialized **Canvas**; **`MainMenuController`** either wires **Inspector** **Button**s or builds **`Canvas`** + **`CanvasScaler`** at runtime (**1280×720**, **Scale With Screen Size**). **Edit Mode** **UI** inventory export therefore often shows **`canvases: []`** for **MainMenu**; use **`ui-design-system.md`** **§3.0** + code for **as-built** menu **UI**.

**Technical constraints:** **Canvas Scaler** — document in spec **§4.3**. **EventSystem** — UI must consume pointer events so world tools (e.g. camera zoom) do not fire through panels. **Performance** — no **`FindObjectOfType`** in **`Update`** (**.cursor/rules/invariants.mdc**). **Coupling** — **`UIManager`** is large; prefer small controllers or shared helpers (**AGENTS.md**).

**Known pain points:** Scroll wheel over UI lists also moving **camera** (**BUG-19** class); **`FindObjectOfType`** in hot paths (**BUG-14**); happiness / stats display inconsistencies (**BACKLOG**).

### 4.5 Program charter (condensed)

| Theme | Intent |
|-------|--------|
| **Consistency** | Shared visual language across **HUD**, **toolbars**, **popups**, **menus** |
| **Velocity** | Reuse documented patterns, not one-off styling |
| **Maintainability** | Clear prefab/script ownership; less duplicated layout |
| **Quality** | Predictable interaction patterns within **Unity UI** constraints |
| **Agent / IDE** | Spec + repo structure so **Cursor** agents need no parallel “context” docs |

**Non-goals (initial):** **UI Toolkit** wholesale replacement in one step; brand assets outside the client; full **WCAG** audit unless a **BACKLOG** row scopes it.

**Program success checklist** (revisit when closing **TECH-67** or a major child): **as-built** baseline maintained (**`ui-design-system.md`** + baseline JSON); minimal **component set** prefabs or documented variants (**`ui-design-system.md`** **§4.1**); new UI **BACKLOG** rows cite spec sections (**reference spec §5**); **Decision Log** rows here stay current.

### 4.6 Backlog bridge (executable work)

| Workstream | Backlog | Reference spec | Notes |
|------------|---------|----------------|-------|
| **As-built** **UI** documentation | *(baseline shipped — **`ui-design-system.md`*)* | **`ui-design-system.md`** **§1–§4**, **§2–§3** | **Multi-scene** export; **glossary** **UI design system (reference spec)**; **`docs/reports/ui-inventory-as-built-baseline.json`** |
| **UI** improvements + **UI-as-code** capstone | **TECH-69** | **`ui-design-system.md`** (post-refactor **as-built**); [`docs/ui-as-built-critique-TECH-67.md`](../docs/ui-as-built-critique-TECH-67.md) | **End of program** — **theme**, prefabs **v0**, **MainMenu** serialize, **typography** policy, **`UIManager`** facades, modal/input, tooling (**§5.2** in **TECH-69**) |
| **Toolbar / ControlPanel** | *(shipped in scene — **§3.3**)* | **§3.3**, **§1.3**, **§4.3** | Refresh **as-built** JSON when hierarchy changes; coordinate **Canvas Scaler** / renames with **TECH-69** **Phase E**/**A** |
| **Prefab** / scene introspection | **TECH-33** | [`.cursor/projects/TECH-33.md`](TECH-33.md) | Complements **UI** inventory export; does **not** replace **Graphic** / **RectTransform** sampling |
| **Umbrella** maintenance & multi-scene **traceability** | *(ongoing — **§7** Phase **0**)* | [`.cursor/projects/TECH-67.md`](TECH-67.md) **§7** Phase **0**; [`docs/reports/README.md`](../../docs/reports/README.md); `Assets/Scripts/Editor/UiInventoryReportsMenu.cs` | Baseline JSON refresh; **`RegionScene`** / **`CityScene`** allowlist when scenes land or rename; **`validate:dead-project-specs`** after **`Spec:`** edits — **excludes** **TECH-69** capstone |

### 4.7 Related issues

| Issue | Relationship |
|-------|----------------|
| **TECH-69** | **Capstone** — critique **P1–P9** + **Editor**/**Skill** tooling (**program** closeout) |
| **TECH-33** | **Prefab** / scene introspection — automation |
| **BUG-53** | **Editor** **Reports** — adjacent to **IDE-first** tooling; expected menus per **`unity-development-context.md`** **§10** |
| **BUG-19** | Scroll / **camera** vs UI — **`ui-design-system.md`** **§3.5** (world vs UI input); **§3.2** only if the failure mode is inside a scrollable **popup** |
| **TECH-59** | **EditorPrefs** / staging registry — optional if **UI** tooling adopts registry-style workflows (**§4.3** overlap) |

### 4.8 Program risks (exploration)

| Risk | Mitigation |
|------|------------|
| **`UIManager`** keeps growing | Extract controllers / helpers; **TECH-69** + follow-up **TECH-** rows |
| Scene-only truth | Document **Canvas** paths in **`ui-design-system.md`** + **UI** inventory JSON (committed baseline under **`docs/reports/`**) |
| Spec drift | **§5.4** mitigations; re-run **UI** inventory export |

### 4.9 Open threads (implementation — not gameplay)

- **TextMeshPro** vs legacy **Text** — state policy in **`ui-design-system.md`** when decided.
- Minimum **Canvas Scaler** reference resolutions for acceptance testing.
- Optional **territory-ia** **UI** graph tool — **`router_for_task`** + **`spec_section`** on **`ui-design-system.md`** may suffice until justified.

**Status (§4.9 threads):** **TextMeshPro** vs **Text** → deferred to **TECH-69** **Phase D** (**§6** **Decision Log**). **Canvas Scaler** acceptance matrix → deferred to **TECH-69** **Phase E**; current **as-built** reference resolutions remain in **`ui-design-system.md`** **§4.3** (**800×600** city, **1280×720** menu). Optional **UI** graph **MCP** → deferred; **`router_for_task`** + **`spec_section`** **`ui`** remain the default until a **BACKLOG** row registers a dedicated tool.

## 5. Proposed Design

### 5.1 Target behavior (product)

**Player-visible** layout and styling are **documented** (**as-built**) **per scene context** (menu vs city vs future region), then **evolved** per **BACKLOG** issues; the reference spec remains the **single** **UI** normative doc for presentation patterns. Specific **HUD** logic changes remain in **FEAT-**/**BUG-** issues.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

**Phased** delivery via **child** issues (**§7**). With **as-built** baseline shipped:

- A small **runtime** library for reusable **UI** primitives (names / folders **TBD** — future child).
- **Editor** folder scripts for **validate** / **scaffold** / **report** operations (future child).
- Optional **Node** + **`batchmode`** glue when headless validation is valuable.

**Persistence boundary:** **UI** documentation and **Editor** export JSON are **not** **Save data**. Do not conflate **`ui-inventory`**, **Agent context**, or **Postgres** **Editor export registry** rows with the **Load pipeline** (**`persistence-system.md`**) unless a **child** issue explicitly scopes interchange for **UI** prefabs (unlikely default).

### 5.3 Method / algorithm notes (optional)

_Order:_ **as-built** spec + baseline JSON (shipped) → **toolbar** in scene + spec **§3.3** → UI kit → Editor/agent tooling.

### 5.4 Risk mitigation — spec vs **Unity** scene, and future “build UI from repo”

**Concern:** **`ui-design-system.md`** could drift from **`Assets/Scenes/*.unity`** (e.g. **`MainMenu`**, **`MainScene`** / **`CityScene`**) and **prefabs**, or a future **codegen** / **Editor** scaffold could **replace** hierarchies in a way that **loses** the **shipped** look-and-feel before new tooling is proven.

**Facts:**

- The **as-built** program does **not** construct the whole **UI** from the markdown spec alone; it **documents** and **exports** **as-built** state (**`ui-design-system.md`** + **UI** inventory **JSON**). **Unity** assets remain authoritative until a **child** issue explicitly introduces procedural construction.
- Confidence in the spec does **not** require “only when we rebuild from the doc.” **Structural** checks (**UI** inventory JSON **`scenes[]`** ↔ spec tables), **git** diffs on all **UI-bearing** `.unity` files / prefabs, and **Play Mode** spot-checks **per scene flow** (menu → load city, etc.) are enough for baseline validation.

**Mitigations (program-level — apply in child specs as needed):**

| Mitigation | Intent |
|------------|--------|
| **Scene / prefab stays primary** until a deliberate migration | Avoid treating the markdown file as the only source of truth before tooling exists. |
| **Re-run UI inventory export** after spec or scene edits | Detect drift between **Editor** snapshot and **`ui-design-system.md`** without codegen. |
| **Git tag or branch + commit** before large hierarchy rewrites | Recover **`MainScene.unity`**, **`MainMenu.unity`**, or future **`CityScene`** / **`RegionScene`** if a **codegen** or reparent experiment fails. |
| **Incremental migration** (one surface at a time: toolbar, then one **popup**) | Avoid a single **big-bang** replace of the whole **Canvas**. |
| **Parallel hierarchy / prefab variants** | Keep old **ControlPanel** (or **HUD** subtree) until the new tree passes **Play Mode** + checklist; delete only after sign-off. |
| Keep **as-built** vs **target** explicit in **`ui-design-system.md`** | **Codegen** can target **§3.3** **Target** while **as-built** rows record what existed pre-migration (history in **git** + spec **§6**). |

**Examples (concrete):**

1. **Export diff after documenting —** Implementer fills **§1.2** from **`UIManager`** + **city** scene and **MainMenu** hierarchy. They run **`Territory Developer → Reports → Export UI Inventory (JSON)`**. If **`scenes[].scene_name == "MainScene"`** (or **`CityScene`** after rename) and node **`Canvas/HUD/MoneyText`** shows `font_size: 20` but the spec table still says `18`, fix the table **or** fix the scene — no procedural **UI** build required. **MainMenu** entries appear under their own **`scenes[]`** block with **`scene_asset_path`** **`Assets/Scenes/MainMenu.unity`**.

2. **Toolbar reparent without losing state —** Before large **`ControlPanel`** hierarchy edits, commit (or tag) current **`MainScene.unity`** (or **`CityScene.unity`**). **MainMenu** should still appear in the **multi-scene** export. Land **`ui-design-system.md`** **§3.3** **as-built** rows; keep a duplicate **`ControlPanel_Legacy`** disabled under the **city** **`Canvas`** until **Play Mode** verifies the new layout; then remove legacy in a follow-up commit.

3. **Future `RegionScene` —** Add **`Assets/Scenes/RegionScene.unity`** to the **`UiInventoryReportsMenu`** allowlist; extend **`ui-design-system.md`** with a **Region** / **map** **UI** subsection; re-run export so **`scenes[]`** gains a third entry without breaking existing paths.

### 5.5 Future work seeds (child issues — not committed scope)

| Track | Idea | Notes |
|-------|------|-------|
| **Runtime UI kit** | Small **C#** library for panels, buttons, styles | **Phase 3** |
| **Editor** automation | Menus for **validate** / **scaffold** / **report** | **`unity-development-context.md`** §10 pattern |
| **`batchmode`** | Headless **UI** tree validation | Optional when export shape stabilizes |
| **Skills / MCP** | **Cursor Skills** + optional **territory-ia** tools | Document in **`docs/mcp-ia-server.md`** when registered |

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Capture exploration in **`projects/ui-as-code-exploration.md`** (not a **reference spec**) | Align stakeholders before bulk **`ui-design-system.md`** edits | Jump straight to spec edits only |
| 2026-04-04 | First program slice scoped **as-built** **`ui-design-system.md`** | Agents and developers need **truth** before **target** refactors and tooling | Start with **UI kit** code without baseline |
| 2026-04-05 | Umbrella **§5.4** — mitigations for spec ↔ scene drift and future **UI** construction | Reduce risk of losing **shipped** **UI** when moving toward repo-driven tooling | Assume spec-only truth before **Editor** export + **git** safety net (rejected) |
| 2026-04-06 | **Multi-scene** **UI** scope (**MainMenu** + **city** + future **Region** / **`CityScene`** rename) | **UI** inventory JSON and spec must not assume **MainScene** only | Single-scene **UI** program (rejected) |
| 2026-04-06 | Retire **`projects/ui-as-code-exploration.md`**; persist charter + inventory in **TECH-67** **§4.4–§4.9** | Single **project spec** for **IA** / **BACKLOG** links; avoid duplicate “workbook” | Keep a separate **`projects/`** file (rejected) |
| 2026-04-06 | **project-spec-kickoff** pass — **§1.1** IA, **§2.3** runtime guardrails, concrete **§7**/**§7b** | Align umbrella with **BACKLOG** **Spec sections**, **invariants**, and **as-built** phased plan | Keep **§7** as one-line phases only (rejected) |
| 2026-04-06 | Child project spec held per-phase **deliverables**/**verification**; **TECH-67** **Phase 1** referenced that spec | Avoid duplicating full checklist in umbrella | Copy entire child **§7** into **TECH-67** (rejected) |
| 2026-04-04 | **TECH-69** = **program capstone** (**UI improvements using UI-as-code**) | Single **BACKLOG** row holds **P1–P9** + tooling design; run **after** **Phase 1–4** unless **Decision Log** collapses order | Many micro-**TECH-** rows only (rejected for this program pass) |
| 2026-04-04 | **§4.9** — **TextMeshPro** vs legacy **Text** | Product policy + migration → **TECH-69** **Phase D**; **`ui-design-system.md`** **§1.2** notes **TBD** until capstone | Decide typography stack in umbrella maintenance only (rejected — duplicates **TECH-69**) |
| 2026-04-04 | **§4.9** — **Canvas Scaler** reference-resolution matrix | Full acceptance matrix → **TECH-69** **Phase E**; **§4.3** table stays the **as-built** source | Expand matrix under umbrella only (rejected — capstone owns depth) |
| 2026-04-04 | **§4.9** — optional **territory-ia** **UI** graph **MCP** | Defer; **`router_for_task`** + **`spec_section`** **`ui`** sufficient until **BACKLOG** scopes **MCP** | Ship graph tool in umbrella maintenance only (rejected — **TECH-69** **Phase H**) |

## 7. Implementation Plan

### Phase 0 — Program notes (this spec)

**Phase 0** tracks **umbrella** inventory, committed **UI** inventory baseline, and **Editor** allowlist (former short-lived project spec **§ Completed** — trace in [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) Recent archive).

- [ ] Keep **§4.4** **Codebase inventory** and **§4.6–§4.9** current as discoveries land (after **Unity** hierarchy or **Inspector** wiring changes, cross-check **§4.4** vs scene roots).
- [ ] After edits to **BACKLOG** **`Spec:`** lines or cross-links among **TECH-67** / siblings: `npm run validate:dead-project-specs` (repo root).

### Phase 1 — As-built reference spec *(shipped)*

**Delivered:** allowlisted scenes + **Editor** **UI** inventory export (bounded **JSON** with **`scenes[]`**); **`ui-design-system.md`** **§1–§4**, **§2–§3** **as-built**; **§3.3** **Current** vs **Target** for **ControlPanel**; committed baseline [`docs/reports/ui-inventory-as-built-baseline.json`](../../docs/reports/ui-inventory-as-built-baseline.json); **`npm run generate:ia-indexes -- --check`** after spec body edits (**glossary** / **IA** index consumers).

**Ongoing:** When **UI** hierarchies change, refresh **`ui-design-system.md`**, **§4.4** here, and the committed baseline JSON per [`docs/reports/README.md`](../../docs/reports/README.md).

### Phase 2 — Toolbar layout *(shipped in scene)*

**Delivered:** **`ControlPanel`** **left**-docked **vertical** **toolbar** authored in **`MainScene.unity`**; **`ui-design-system.md`** **§3.3** / **§1.3** describe **as-built** layout (open row retired — [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md)).

**Ongoing:** **Play Mode** verify **§3.5**-class issues (scroll vs **camera**) on **HUD** / **toolbar** / **popups**; refresh committed **UI** inventory JSON when the hierarchy changes materially.

### Phase 3 — Runtime UI kit (incremental or folded into **TECH-69**)

- [ ] **Option A:** File a dedicated **BACKLOG** row ( **`Spec:`** `.cursor/projects/{ISSUE_ID}.md` ) for early **theme**/**prefab** spikes if the team wants delivery **before** the capstone.
- [ ] **Option B (default):** Implement **runtime** **theme** **`ScriptableObject`**, prefab **v0**, and **`UIManager`** facades under [TECH-69](TECH-69.md) **Phase C**/**F** — **§2.3** guardrails; XML docs on new public **API** per **`.cursor/rules/coding-conventions.mdc`**.

### Phase 4 — Editor / agent tooling (incremental or folded into **TECH-69**)

- [ ] **Editor** menus (**validate** / **scaffold**) — **`unity-development-context.md`** **§10**; optional **`tools/`** **Node** diff vs **UI** inventory (**TECH-69** **§5.2**).
- [ ] **Cursor Skill(s)** + optional **territory-ia** **MCP**: register per **`docs/mcp-ia-server.md`** if shipped; **`npm run verify`** under **`tools/mcp-ia-server/`** when **MCP** code changes — **Phase H** in [TECH-69](TECH-69.md).

### Phase 5 — Program capstone (**TECH-69**)

- [ ] Execute [`.cursor/projects/TECH-69.md`](TECH-69.md) **§7** through **§8** — **UI improvements using UI-as-code** (**P1–P9** from [`docs/ui-as-built-critique-TECH-67.md`](../docs/ui-as-built-critique-TECH-67.md), refined in **TECH-69** **§5.3**).
- [ ] **Gate:** **TECH-69** **§8** satisfied; **critique** doc **§8** tracking updated; umbrella **§10** **Lessons** filled on program closeout.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| **TECH-67** **§8** bullet 1 (**as-built** baseline) | Node / manual | **`ui-design-system.md`** + baseline JSON current | `npm run generate:ia-indexes -- --check` after **`ui-design-system.md`** / **glossary** body edits |
| **TECH-67** **§8** bullet 2 (extra **child** row or **Decision Log** deferral) | Manual | **BACKLOG.md** + **§6** **Decision Log** | Record intentional deferral if no new row |
| **TECH-67** **§8** bullet 3 (**AGENTS.md** / **agent-router**) | Manual | Diff vs **`.cursor/rules/agent-router.mdc`** | Only when default **UI** route changes |
| **BACKLOG** / **Spec** links valid | Node | `npm run validate:dead-project-specs` (repo root) | After **`Spec:`** or `.cursor/projects/` path edits |
| **IA** index after **`ui-design-system.md`** edits | Node | `npm run generate:ia-indexes -- --check` | Per **project-implementation-validation** |
| **IA** / **MCP** package (if touched in a **child** issue) | Node | `npm run verify` under `tools/mcp-ia-server/` | Per **project-implementation-validation** skill |
| Runtime **UI** kit / **TECH-69** capstone | Manual / UTF | **TECH-69** **§7b** + **Play Mode** smoke | **Phase 5** exit |

## 8. Acceptance Criteria

- [x] **`ui-design-system.md`** **as-built** baseline shipped — see **glossary** **UI design system (reference spec)** and **`docs/reports/ui-inventory-as-built-baseline.json`**.
- [ ] [TECH-69](TECH-69.md) **§8** satisfied — **capstone** (**UI improvements using UI-as-code**) delivers **P1–P9** + **§5.2** tooling (or **Decision Log** / **BACKLOG** records deferrals).
- [ ] Interim **child** rows (**Phase 3–4** **Option A**) filed only if the team splits work **before** **TECH-69**; otherwise **TECH-69** absorbs kit + **Editor**/**Skill** scope per **§7** above.
- [ ] **`AGENTS.md`** or **`.cursor/rules/agent-router.mdc`** updated only if the default **UI** task route changes (optional).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- _Fill at closure; migrate to **`ui-design-system.md`**, **`AGENTS.md`**, or **Skills**._

## Open Questions (resolve before / during implementation)

None — program charter, documentation, and developer/agent workflow only. **Player-facing** **UI** semantics and gameplay tie-ins belong in **child** **FEAT-**/**BUG-** specs under **`## Open Questions`** using **glossary** terms. **Implementation** threads (TMP vs **Text**, scaler resolutions, optional **MCP**) live in **§4.9**, not here — **Open Questions** in project specs target **game logic** definitions only (**`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`**).
