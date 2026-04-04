# UI-as-code program — exploration workbook

> **Umbrella backlog:** [TECH-67](../BACKLOG.md) — **Project spec:** [`.cursor/projects/TECH-67.md`](../.cursor/projects/TECH-67.md)  
> **Purpose:** Single place for **program charter**, **codebase inventory**, and **exploration** before enriching **`.cursor/specs/ui-design-system.md`** and filing **child** issues. **Normative** foundations, components, and patterns belong in **`ui-design-system.md`** (reference spec); this file is **not** a reference spec.

## Document map (where things live now)

| Need | Location |
|------|----------|
| **Foundations, components, patterns, Unity mapping, per-issue acceptance hints** | [`.cursor/specs/ui-design-system.md`](../.cursor/specs/ui-design-system.md) — use for **BACKLOG** **Spec sections** and **territory-ia** `spec_section` / **agent-router** “UI changes” |
| **Program intent, IA inventory, constraints, pain points, phased exploration** | This workbook (`projects/ui-as-code-exploration.md`) |
| **Executable issues** | [`BACKLOG.md`](../BACKLOG.md) — **TECH-67** (umbrella), **TECH-68** (**as-built** **`ui-design-system.md`**), **TECH-07** (**ControlPanel** layout) |
| **Editor diagnostics / Reports / batchmode patterns** | [`.cursor/specs/unity-development-context.md`](../.cursor/specs/unity-development-context.md) §10+ |
| **Layering and init order** | [`ARCHITECTURE.md`](../ARCHITECTURE.md) |

**Superseded (removed 2026-04-04):** `docs/ui-design-system-project.md` (charter index) and `docs/ui-design-system-context.md` (discovery inventory). Their useful content is **rescued** in **§ Charter** and **§ Codebase inventory** below.

---

## 0. How to use this workbook

| Step | Owner | Status |
|------|-------|--------|
| Keep **§ Charter** decision log and **§ Codebase inventory** in sync when UI architecture shifts | TBD | ☐ |
| Promote stable rules from exploration into **`ui-design-system.md`** (reference spec) | TBD | ☐ |
| Review with team / agent charter pass | TBD | ☐ |
| **TECH-68** **as-built** spec pass (see **`.cursor/projects/TECH-68.md`**) | TBD | ☐ |
| Further **child** rows (**runtime kit**, **Editor** tooling) | TBD | ☐ |
| Archive obsolete rows here or move to **TECH-67** **Decision Log** | TBD | ☐ |

---

## Charter and program decisions

*Rescued and condensed from the former `docs/ui-design-system-project.md` (pre–spec-pipeline / pre–Skills). That file was an index and decision log; it did not replace **BACKLOG** issue ids.*

### Role of this program

Introduce a coherent **UI / UX** approach for Territory Developer: shared visual language, reusable components, traceable **BACKLOG** links to spec sections, and (under **TECH-67**) **IDE- and agent-first** workflows aligned with **Unity** **uGUI** / **Canvas** / **Prefab** practice.

### Goals

- **Consistency** — Color, type, spacing, and control states across **HUD**, **toolbars**, **popups**, and **menus**.
- **Velocity** — New flows reuse documented patterns instead of one-off styling.
- **Maintainability** — Clear ownership of UI prefabs and scripts; less duplicated layout and ad-hoc **Inspector** wiring.
- **Quality** — Predictable interaction patterns (focus, feedback, errors) within **Unity UI** constraints used in this repo.
- **Agent / IDE usability** — Enough structure in spec + repo that **Cursor** agents and developers can implement or review UI without redundant parallel “context” docs.

### Non-goals (initial phase)

- Replacing the entire stack with **UI Toolkit** in one step (evaluate per workstream).
- Brand or marketing assets outside the game client.
- Full **WCAG** accessibility audit unless scoped in a future issue.

### Success criteria (program-level checklist)

Revisit when closing **TECH-67** or a major child phase:

- [ ] **TECH-68** complete — **`ui-design-system.md`** **§1–§4** and major **§2–§3** surfaces document **as-built** (**shipped**) **UI** (**glossary** **UI design system (reference spec)**).
- [ ] Foundations (tokens / theme rules) reflect **reality** first; **target** rows explicit where **BACKLOG** defines future work.
- [ ] A minimal **component set** (e.g. primary button, panel, list row) exists as prefabs or documented variants with naming conventions (**§4.1** of reference spec).
- [ ] New UI-related **BACKLOG** rows cite the **spec section** they implement (**reference spec §5**).
- [ ] **Decision log** (below) and **TECH-67** **Decision Log** stay current for material trade-offs.

### Suggested phases (adjust as you learn)

1. **As-built reference spec** — **TECH-68**: document **shipped** **UI** in **`ui-design-system.md`** from **MainScene** / **prefabs** / scripts (**as-built** **vs** **target** labels).
2. **Discovery / inventory** — Keep **§ Codebase inventory** in this workbook in sync; align with **TECH-33** as needed.
3. **Target layout** — **TECH-07** (**ControlPanel**); update spec **§3.3** after implementation.
4. **Runtime UI kit** — **child** issue **TBD**; standardize prefabs / helpers.
5. **Editor / agent tooling** — **Skills**, optional **MCP**, validators (**child** issue **TBD**).
6. **Hardening** — Retire legacy one-off styles where replaced.

### Backlog bridge (executable work — not a second source of ids)

| Workstream | Backlog | Reference spec | Notes |
|------------|---------|----------------|-------|
| **As-built** **UI** documentation | **TECH-68** | **§1–§4**, **§2–§3** | Baseline before **TECH-07**; **glossary** **UI design system (reference spec)** |
| **Toolbar / ControlPanel** — left sidebar layout | **TECH-07** | **§3.3**, **§1.3**, **§4.3** | **Soft:** after **TECH-68**; scene **`MainScene.unity`**, **`ControlPanel`** |

Add rows here when new UI program work is ticketed; always link **BACKLOG** by id.

### Decision log (charter)

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-03-20 | Cross-link charter, context, spec **§3.3**, **AGENTS**, **ARCHITECTURE**, rules; track **toolbar** on **BACKLOG** | Traceable **ControlPanel** work; split meta vs implementation | Single issue mixing docs + Unity layout |
| 2026-04-04 | Retire `docs/ui-design-system-project.md` and `docs/ui-design-system-context.md`; consolidate into this workbook + **`ui-design-system.md`** | **Skills**, **territory-ia**, and **TECH-67** replace ad-hoc “context” docs; one exploration surface | Keep parallel docs under `docs/` (rejected — drift and duplicate IA) |
| 2026-04-04 | **TECH-68** = first **TECH-67** child — **as-built** **`ui-design-system.md`** | Spec must describe **shipped** **UI** before heavy **target** refactors | Start with **UI kit** without baseline (rejected) |

---

## Codebase inventory and constraints

*Rescued and condensed from the former `docs/ui-design-system-context.md`. Scene object names and **Inspector** wiring can drift — verify in **Unity** when implementing.*

### Stack and orchestration

Player-facing UI uses **Unity UI (uGUI)** — **Canvas**, **Graphic** components (**Image**, **Text** / **TMP**, etc.), and **EventSystem**. Primary orchestrator: **`UIManager.cs`** (`Territory.UI`) with many serialized references to texts, images, **popups**, and tool state. **`CursorManager`**, **`GameNotificationManager`**, and **UnitControllers** handle focused interactions.

### Architectural placement

From **`ARCHITECTURE.md`**: **UI layer** — **`UIManager`**, **`CursorManager`**, **`GameNotificationManager`**, controllers. **Input** — **`GridManager`** and others gate world input when the pointer is over UI (**`IsPointerOverGameObject`**); scroll vs camera is a recurring UX area (see **BACKLOG**).

### Primary entry points

| File | Role |
|------|------|
| `Assets/Scripts/Managers/GameManagers/UIManager.cs` | Main **HUD**, **popups** (**PopupType**: load game, details, building selector, stats, tax), **toolbar** / zone and tool selection, demand visualization |
| `Assets/Scripts/Managers/GameManagers/CursorManager.cs` | Cursor state with tools and UI |
| `Assets/Scripts/Managers/GameManagers/GameNotificationManager.cs` | **Game notification** path (**singleton**, `DontDestroyOnLoad`) |

### Controllers (representative inventory)

**`Assets/Scripts/Controllers/GameControllers/`** — e.g. **`CameraController.cs`** (zoom vs UI scroll), **`CityStatsUIController.cs`**, **`MiniMapController.cs`**.

**`Assets/Scripts/Controllers/UnitControllers/`** — e.g. **`BuildingSelectorMenuController`**, **`DetailsPopupController`**, **`DataPopupController`**, **`GrowthBudgetSlidersController`**, **`SpeedButtonsController`**, **`*SelectorButton.cs`** (RCI, roads, power, water, environment, etc.), **`MiniMapLayerButton`**, **`ShowStatsButton`**, **`ShowTaxes`**, **`SimulateGrowthToggle`**.

Other managers feed data shown in the **HUD** (e.g. **`StatisticsManager`**, **`EconomyManager`**, **`TimeManager`**) without owning every widget.

### Scene and **ControlPanel**

- Primary layout: **`Assets/Scenes/MainScene.unity`** — **Canvas** hierarchy authoritative in Editor.
- **`ControlPanel`**: main construction **toolbar** (demolition, **RCI** **zoning**, utility buildings, **streets**, forest/environment). Wired via **`UIManager`** and **`UnitControllers/*SelectorButton.cs`** (and related) in **Inspector**.
- **Layout target** (not necessarily implemented): migrate from **bottom-centered horizontal ribbon** to **left-docked vertical** panel — **one row per category**, **horizontal** buttons per row; re-anchor overlays (e.g. zone density) to the sidebar; avoid **mini-map** / corner **HUD** overlap. **Normative layout description:** **`.cursor/specs/ui-design-system.md`** **§3.3**. **Issue:** **TECH-07**.

### Technical constraints (project norms)

- **Canvas Scaler** — Mode and reference resolution affect every layout; lock in **reference spec §4.3** when decided.
- **EventSystem** — UI must consume pointer events so world tools (e.g. camera zoom) do not fire through panels (**BACKLOG** for regressions).
- **Performance** — No **`FindObjectOfType`** in **`Update`** / per-frame paths (**.cursor/rules/invariants.mdc**); cache in **`Awake`/`Start`** or serialized fields.
- **Coupling** — **`UIManager`** is large; prefer small controllers or shared helpers over growing a single class (**AGENTS.md** anti-patterns).

### Known pain points (backlog- and code-informed)

- Scroll wheel over UI lists also moving **camera** (e.g. **BUG-19** class issues).
- **`FindObjectOfType`** in hot paths affecting UI-related managers (**BUG-14**).
- **Happiness** / stats display inconsistencies (logic vs **UI** coupling — **BACKLOG**).

---

## 1. Problem statement

- **Authoring friction:** Much UI lives only in **Scene** / **Prefab** hierarchies; agents and **IDE** workflows lack a single, up-to-date map besides scattered scripts — the retired `docs/*` files tried to fill that gap but predated **Skills** / **MCP** and duplicated the reference spec’s intent.
- **Spec vs implementation gap:** **`ui-design-system.md`** remains **Draft** with many **TBD** rows; **ControlPanel** target layout is specified in **§3.3** but **TECH-07** may still be open.
- **Duplication risk:** Without consolidation, charter text, inventory, and normative patterns drift across `docs/`, **BACKLOG** **Notes**, and **`.cursor/specs/`**.

---

## 2. Success metrics

| Metric | Target | How measured |
|--------|--------|--------------|
| Spec completeness | **§1** tokens and **§4.3** scaler non-TBD for pilot surfaces | Reference spec review |
| Traceability | ≥90% of new UI **BACKLOG** rows cite **`ui-design-system.md`** section | **BACKLOG** audit |
| **ControlPanel** | Layout matches **§3.3** target or **Decision Log** documents intentional deviation | **Play Mode** + scene diff |
| Agent time | Fewer round-trips for “where is toolbar / which controller” | Informal; optional **Skill** adoption |

---

## 3. Technical options (compare)

### 3.1 Runtime composition (C# factories, prefab variants, ScriptableObjects)

| Option | Pros | Cons | Notes |
|--------|------|------|-------|
| **Current baseline** — serialized refs on **`UIManager`** + prefabs | Matches Unity norms; fast for small changes | Large orchestrator; hard for agents to see full graph | **TECH-67** child may introduce thin factories or theme **ScriptableObjects** |
| Prefab variants per component | Consistent styling | Requires naming convention (**reference spec §4.1**) | TBD |

### 3.2 Editor automation (menus, `AssetDatabase`, validators)

| Option | Pros | Cons | Notes |
|--------|------|------|-------|
| **Reports**-style exports for UI tree summaries | **IDE**-readable; aligns with **unity-development-context** §10 | Needs implementation | Optional **TECH-67** child |
| Scaffold menu for panel + layout groups | Repeatable hierarchy | Maintenance of templates | TBD |

### 3.3 Headless / CLI / `batchmode` (optional)

| Option | Pros | Cons | Notes |
|--------|------|------|-------|
| Validate “no missing script” on UI prefabs | CI signal | **Unity** runner cost | Overlap **TECH-33** |

### 3.4 Agent layer (Cursor Skills, optional territory-ia MCP tools)

| Option | Pros | Cons | Notes |
|--------|------|------|-------|
| **Skill** “UI change” recipe | Enforces spec sections + **invariants** | Must be kept in sync with spec | **TECH-67** Phase 3 |
| New **MCP** `spec_section` only (no new tool) | Already maps **UI** → **`ui-design-system.md`** | No UI-specific graph | Default today |

---

## 4. Scope boundaries

### 4.1 In scope (umbrella + children)

- Enrich **`ui-design-system.md`**; implement **TECH-07** and other ticketed UI work with spec references.
- **Editor** / **Skills** / optional **MCP** for UI authoring and validation (**TECH-67**).

### 4.2 Out of scope

- **UI Toolkit** wholesale migration without a dedicated issue.
- Gameplay rule changes (remain in domain specs + **FEAT-**/**BUG-**).

### 4.3 Relationship to existing issues

| Issue | Relationship |
|-------|----------------|
| **TECH-67** | Umbrella for **UI-as-code** + this workbook |
| **TECH-68** | **As-built** **`ui-design-system.md`** — **first** program deliverable |
| **TECH-07** | **ControlPanel** layout — **§3.3**; **soft:** after **TECH-68** |
| **TECH-33** | **Prefab** / scene introspection — supports automation and agent manifests |
| **BUG-53** | **Editor** **Reports** menus — agent diagnostics adjacent to **IDE-first** UI story |
| **BUG-19** | Scroll / **camera** vs UI — pattern **§3.2** / **§3.5** in reference spec |

---

## 5. Child issue breakdown (draft)

| Phase | Backlog id | Title (working) | Likely deliverables | Depends on |
|-------|------------|-----------------|---------------------|------------|
| A | **TECH-68** | **As-built** reference spec | **`ui-design-system.md`** **§1–§4**, **§2–§3** from **MainScene** / prefabs / scripts | — |
| B | **TECH-07** (existing) | **ControlPanel** target layout | Scene + **`UIManager`** as needed | A (soft) |
| C | TBD | Runtime UI kit | `Assets/Scripts/…`, prefabs, naming | A (soft) |
| D | TBD | Editor + agent tooling | **Editor** scripts, **Skills**, optional **MCP** | A, C (soft) |

---

## 6. Risks and mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **`UIManager`** keeps growing | Harder merges and agent reasoning | Extract controllers / helpers; track in **TECH-07** and follow-up **TECH-** rows |
| Scene-only truth | **Git** diffs opaque for hierarchy | Document **Canvas** paths here + prefabize where possible |
| Exploration doc becomes normative | Conflicts with **reference spec** | **Rule:** product-facing rules live in **`ui-design-system.md`** after agreement |

---

## 7. Open threads (non-normative)

- Whether **TextMeshPro** vs legacy **Text** is mandatory for new UI (reference spec should state when decided).
- Minimum **Canvas Scaler** reference resolutions for acceptance testing.
- Optional **UI** slice for **territory-ia** (`router_for_task` already points at **`ui-design-system.md`** — sufficient until UI graph tools are justified).
