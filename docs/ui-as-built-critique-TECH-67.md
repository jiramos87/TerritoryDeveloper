# UI as-built critique — design-system reality vs product goals

**Program:** [TECH-67](../.cursor/projects/TECH-67.md) (**UI-as-code program** umbrella)  
**Source of truth for this critique:** [`.cursor/specs/ui-design-system.md`](../.cursor/specs/ui-design-system.md) **as-built** sections (§1–§4, §2–§3), grounded in committed inventory [`docs/reports/ui-inventory-as-built-baseline.json`](reports/ui-inventory-as-built-baseline.json).  
**Execution / capstone:** [TECH-69](../.cursor/projects/TECH-69.md) (**BACKLOG** — **UI improvements using UI-as-code**) implements the **P1–P9** track below (refined **§5.3** in that spec) plus **Editor** / **Cursor Skill** / optional **MCP** tooling (**TECH-69** **§5.2**, **§7** Phase **H**).  
**Language:** Planning note only; gameplay rules stay in **FEAT-**/**BUG-** issues.

**Last updated:** 2026-04-04

---

## 1. Purpose

The reference spec describes **what shipped**, not an ideal design system. This document **critiques that reality** so **TECH-67** children target **real pain**. **Improvements** are **not** open-ended: they roll into **TECH-69** at **umbrella** closeout (see **§8**).

---

## 2. What the spec shows (summary)

| Area | As-built reality (from spec) |
|------|------------------------------|
| **Foundations** | **Colors** and **fonts** are **emergent** from per-widget **`Graphic.color`** and **legacy `Text`** — token names in §1.1 are **retroactive labels**, not a single theme asset. |
| **Typography** | Dominated by **`LegacyRuntime`** at **10** / **36** / **14** / **12** px; **TMP** is **rare** and **mixed** with misleading hierarchy names (`Text (Legacy)` next to **TMP**). |
| **Spacing** | **No** global grid token; **Main menu** uses **code constants** (200×40, spacing 10); **city** **HUD** uses **per-panel** anchors and clusters. |
| **Components** | **No** shared prefab library — **Button** + **Image** + **Text** repeated; **ScrollRect** patterns duplicated (**LoadGame**, **BuildingSelector**). |
| **Scenes** | **MainScene** **`UI/City/Canvas`** holds almost all **UI**; **MainMenu** may have **zero** serialized **Canvas** — **runtime `BuildUI()`** vs **Inspector** wiring is a **bifurcated** authoring model. |
| **Canvas scaler** | **800×600** (**city**) vs **1280×720** (**menu** code path) — different reference baselines for one product. |
| **Orchestration** | **`UIManager`** centralizes **many** serialized **`Text`** references and **popup** routing; controllers exist but **coupling** and **surface area** remain high. |
| **Input / UX** | **Scroll** vs **camera** is a **known** friction class (**§3.5**, **BUG-19**); **tooltip** is a **single float** on **`UIManager`**, not a component pattern. |

---

## 3. Critique

### 3.1 Foundations are descriptive, not prescriptive

**Issue:** §1.1 deduplicates **RGBA** strings from exports; nothing enforces that new widgets use **ui-text-primary** vs ad-hoc tints. **Accent blues** appear as **two** near-identical values — classic drift from copy-paste.

**Impact:** Visual inconsistency grows with every feature; agents cannot “apply the theme” because there is **no** theme object — only **Inspector** colors.

**Severity:** High for long-term maintainability; medium for player-facing quality today (the game reads as “functional Unity UI”).

### 3.2 Typography stack is split and legacy-heavy

**Issue:** **City** **HUD** is **`LegacyRuntime` + `Text`**-first; **TMP** is an exception. That blocks crisp scaling, consistent SDF outlines, and shared **Text Style** assets.

**Impact:** Harder localization, outline/glow consistency, and **Editor** tooling that assumes **TMP** (Unity’s forward path).

**Severity:** High for any “UI kit” or codegen; medium if the team commits to **legacy Text** deliberately (then the spec should **state** that policy and stop **TMP** creep).

### 3.3 Two authoring models for the main menu

**Issue:** §3.0 documents **runtime** **Canvas** creation when references are null, else **Inspector** UI — the **scene file** may contain **no** **Canvas**, so **Edit Mode** inventory can show **`canvases: []`** for **MainMenu**.

**Impact:** **Git**-reviewable **UI** for the first screen players see is **optional**; onboarding and **agent** workflows cannot assume **YAML** truth for the menu.

**Severity:** High for **UI-as-code** goals; forces **Play Mode** or **code** reading for menu truth.

### 3.4 No component reuse story

**Issue:** §2 states clearly: **no** single shared **UI** prefab library — each panel repeats stacks.

**Impact:** Changing **button** height, padding, or **color** requires touching many **GameObjects**; regression risk on **toolbar** / **ControlPanel** work and any **HUD** reshuffle.

**Severity:** High for **toolbar** / **popup** refactors.

### 3.5 **`UIManager` as a “god” surface**

**Issue:** §3.1 lists a **large** cluster under **`DataPanelButtons`** all orchestrated from **`UIManager`** + **`Text`** fields (per **Related files** and **managers-reference** alignment).

**Impact:** Every new stat line increases **merge conflicts** and **Inspector** wiring burden; testing **UI** in isolation is hard.

**Severity:** High for team velocity; aligns with **TECH-67** **§4.4** “**UIManager** keeps growing” risk.

### 3.6 HUD density and hierarchy

**Issue:** **Stats**, **details**, **tax**, **game** buttons, and **mini-map** layers share one **Canvas** tree; §3.1 reads as **feature-complete** but **visually** crowded at small resolutions (spec already asks for **800×600** and **1920×1080** checks).

**Impact:** **Toolbar** / **ControlPanel** moves may **collide** with corner **HUD** unless **safe areas** / **anchors** are redesigned together.

**Severity:** Medium–high depending on minimum supported resolution.

### 3.7 Popups are path-mapped but not pattern-unified

**Issue:** §3.2 maps **`PopupType`** to **Canvas** paths — good traceability — but **modal** behavior (dimmer, **Esc**, focus) is **not** specified as a **shared** pattern.

**Impact:** Inconsistent **UX** between **LoadGame**, **BuildingSelector**, and **Tax** flows.

**Severity:** Medium.

### 3.8 Naming and hygiene

**Issue:** §4.1 notes **Unity** duplicate names (`TotalGrowthLabel (1)`) and **typos** in object names (e.g. export showed `UnenmploymentPanel` in JSON — if still present in scene, it propagates to scripts and docs).

**Impact:** Search, refactor, and **agent** navigation suffer.

**Severity:** Low–medium (easy wins with rename passes scoped by issue).

---

## 4. Concrete improvement proposals → **TECH-69**

Original IDs **P1–P9** are **refined** and **scheduled** in [`.cursor/projects/TECH-69.md`](../.cursor/projects/TECH-69.md) **§5.3** / **§7** (Phases **A–H**). Summary:

| # | Proposal | Rationale (ties to §3) | Capstone phase |
|---|----------|----------------------|----------------|
| **P1** | **Unify menu authoring:** Serialize **MainMenu** **Canvas** in **`MainMenu.unity`** (or prefab); narrow **`BuildUI()`**. | §3.3 | **TECH-69** Phase **B** |
| **P2** | **Typography policy:** **TMP** migration **or** **legacy Text** freeze — **Decision Log** then waves. | §3.2 | **TECH-69** Phase **D** |
| **P3** | **Minimal theme:** **`ScriptableObject`** (**`UiTheme`**) for **Color** + sizes (+ optional **TMP** refs). | §3.1 | **TECH-69** Phase **C** |
| **P4** | **Prefab library v0:** tool button, stat row, scroll shell, modal shell. | §3.4 | **TECH-69** Phase **C** |
| **P5** | **Canvas Scaler** strategy + resolution matrix; align with **`ui-design-system.md`** **§3.3** / **§4.3**. | §2 / §4.3 | **TECH-69** Phase **E** |
| **P6** | **Split **`UIManager`** by surface** (facades; **no** rule changes). | §3.5 | **TECH-69** Phase **F** |
| **P7** | **Modal contract** (overlay, close, optional **Esc**) for **`PopupType`**. | §3.7 | **TECH-69** Phase **G** |
| **P8** | **Rename / cleanup** (typos, `(1)` duplicates) + spec path update. | §3.8 | **TECH-69** Phase **A** (front-loaded) |
| **P9** | **Scroll vs camera** — product checklist + **BUG-19** alignment. | §3.5 | **TECH-69** Phase **G** |

**§3.3** documents the **shipped** **toolbar** in **`MainScene.unity`**; **P5**/**P8** coordinate with scaler and rename hygiene around that layout.

---

## 5. Tooling to implement (umbrella implications)

**Authoritative detail:** [TECH-69](../.cursor/projects/TECH-69.md) **§5.2** / **§7** Phase **H**. At a glance:

| Layer | Deliverable | Why |
|-------|-------------|-----|
| **Runtime** | **`UiTheme`** + prefab **v0** under `Assets/` | Makes **§1** prescriptive; agents instantiate **prefabs** instead of one-off hierarchies. |
| **Editor** | **MenuItem** validate/scaffold (**Territory Developer** subtree per **`unity-development-context.md`** **§10**) | Catches drift vs **UI** inventory / **theme**. |
| **Repo** | Optional **Node** diff (**inventory JSON** vs theme export) | Advisory **CI** when **TECH-69** defines export shape. |
| **Cursor** | **Skill** — “add **HUD** row using **`UI_StatRow`** + **`UiTheme`**” | Encodes safe recipes post-**P4**. |
| **territory-ia** | Optional **`snake_case`** tool (e.g. theme token slice) | Only if **MCP** cost accepted; **`npm run verify`** + docs. |

**TECH-67** **§7** **Phase 3–4** may file **interim** children **or** fold kit + **Editor** work into **TECH-69** (**Option B** default in umbrella spec).

---

## 6. What is already working (keep)

- **Clear functional naming** on **ControlPanel** children (`*SelectorButton`).
- **`PopupType`** enum + **controller** registration — extend, don’t replace.
- **Bounded **UI** inventory export** — drift detection once **theme** + prefabs exist.
- **Explicit **as-built** vs **target** in **`ui-design-system.md`** **§3.3**.

---

## 7. References

- [`.cursor/specs/ui-design-system.md`](../.cursor/specs/ui-design-system.md)
- [`.cursor/projects/TECH-67.md`](../.cursor/projects/TECH-67.md)
- [`.cursor/projects/TECH-69.md`](../.cursor/projects/TECH-69.md) — **capstone**
- [`docs/reports/ui-inventory-as-built-baseline.json`](reports/ui-inventory-as-built-baseline.json)
- **BACKLOG:** **BUG-19**, **BUG-14**

---

## 8. Tracking

| Item | Location |
|------|----------|
| **BACKLOG** row | **TECH-69** — **UI improvements using UI-as-code** |
| **Project spec** | `.cursor/projects/TECH-69.md` |
| **Umbrella** | **TECH-67** **§7** Phase **5**, **§8** third bullet |

When **TECH-69** **§8** is satisfied, update **`ui-design-system.md`** **as-built** tables for **shipped** **theme**/**prefabs** and migrate durable **Lessons** per **project-spec-close** (program-level).
