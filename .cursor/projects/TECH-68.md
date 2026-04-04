# TECH-68 — As-built UI documentation (`ui-design-system.md`)

> **Issue:** [TECH-68](../../BACKLOG.md)
> **Parent program:** [TECH-67](TECH-67.md) (**UI-as-code program** umbrella)
> **Status:** Draft
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04

<!--
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Use glossary terms: ../specs/glossary.md (spec wins if glossary differs).
-->

## 1. Summary

**`.cursor/specs/ui-design-system.md`** must describe the **shipped** in-game **UI** as it exists today — whether or not that UI was built as a formal “design system.” This issue delivers an **as-built** pass: measured and observed **colors**, **typography** (font assets, sizes), **spacing** and **margins**, **Canvas** / **RectTransform** anchoring, **HUD** and **toolbar** placement, **popup** flows and **UX** behaviors, representative **player-facing strings**, and **Canvas Scaler** settings. The result is the **baseline** reference for agents and developers before **target-state** refactors (**TECH-07**, component library work, etc.).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Replace **TBD** placeholders in **`ui-design-system.md`** **§1–§4** (and **§2** / **§3** where applicable) with **as-built** tables and prose sourced from **`MainScene.unity`**, UI **prefabs**, **`UIManager`**, and listed **controllers**.
2. Clearly label content as **As-built (current)** vs **Target (planned)** where the spec already describes a future layout (e.g. **§3.3** **ControlPanel**).
3. Sync [`projects/ui-as-code-exploration.md`](../../projects/ui-as-code-exploration.md) **Codebase inventory** if hierarchy or file roles change during the audit.
4. Leave **`ui-design-system.md`** in a state where **territory-ia** `spec_section` and **BACKLOG** **Spec sections** describe **reality**, not only aspirations.

### 2.2 Non-Goals (Out of Scope)

1. Implementing **TECH-07** (**ControlPanel** layout migration) — document **current** layout first; target layout stays in spec as **Target** unless this issue explicitly records “still legacy.”
2. Introducing a new **runtime UI kit** or **Editor** scaffold tools (later **TECH-67** children).
3. Changing gameplay rules or **Simulation** behavior (presentation and **UX** documentation only).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want the reference spec to match what players see so that I do not fight outdated **TBD** rows. | **§1–§4** populated with **as-built** values or explicit “inherit from parent / theme default” notes. |
| 2 | IDE agent | I want **`spec_section`** on **UI** to return actionable layout and typography facts. | Spec sections cite **Canvas** paths, scaler mode, and primary font sizes where used. |
| 3 | Maintainer | I want a clear baseline before **TECH-07** so refactors are diffable against documentation. | **§3.3** states **current** vs **target** layout explicitly. |

## 4. Current State

### 4.1 Domain behavior

**`ui-design-system.md`** is **Draft** with mostly **TBD** foundations and pattern stubs. The game has a substantial **uGUI** implementation (**`UIManager`**, many **controllers**, **MainScene** **Canvas**). The gap is **documentation fidelity**, not absence of UI.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Reference spec to update | `.cursor/specs/ui-design-system.md` |
| Scene / hierarchy | `Assets/Scenes/MainScene.unity` |
| Orchestration | `Assets/Scripts/Managers/GameManagers/UIManager.cs` |
| Controllers | `Assets/Scripts/Controllers/UnitControllers/`, `GameControllers/` |
| Workbook | [`projects/ui-as-code-exploration.md`](../../projects/ui-as-code-exploration.md) |
| Umbrella | [`.cursor/projects/TECH-67.md`](TECH-67.md) |

### 4.3 Implementation investigation notes (optional)

- Prefer **Unity Editor** inspection for **Canvas Scaler**, **RectTransform** anchors, and **Text** / **TMP** components; cross-check **YAML** scene dumps in git when helpful.
- Where values vary per widget, document **ranges** or **representative** **HUD** / **popup** samples and note variance.
- **Optional:** **TECH-33** tooling later for automated manifests; **TECH-68** remains **manual**-friendly if automation is not ready.

## 5. Proposed Design

### 5.1 Target behavior (product)

**None** — documentation-only issue.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Add a short **“As-built vs target”** subsection under **`ui-design-system.md`** **Overview** (or **§1** intro) if not already present after edits.
- Use tables for **§1.1–§1.3**, **§4.3**, and expand **§2** / **§3** with **as-built** bullets per surface (**HUD**, **ControlPanel**, major **popups**).

### 5.3 Method / algorithm notes (optional)

_Audit order suggestion:_ **Canvas** root → **HUD** strip → **ControlPanel** → **mini-map** → **popup** prefabs / panels referenced from **`UIManager`**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | First **TECH-67** child is **as-built** documentation (**TECH-68**) | Baseline before **IDE-first** tooling and **TECH-07** | Start with **UI kit** code (rejected — unknown tokens without audit) |

## 7. Implementation Plan

### Phase 1 — Inventory pass

- [ ] Walk **`MainScene.unity`** **Canvas** hierarchy; note paths, **LayoutGroup** usage, anchor presets.
- [ ] Record **Canvas Scaler** (mode, reference resolution, match).

### Phase 2 — Foundations (**§1**) and Unity mapping (**§4**)

- [ ] **Colors:** sample **Image** / **Text** tints and shared materials (table in **§1.1**).
- [ ] **Typography:** font assets, sizes, styles for **HUD** title, body, buttons (**§1.2**).
- [ ] **Spacing / margins:** base unit if discernible; key panel paddings (**§1.3**).

### Phase 3 — Components and patterns (**§2–§3**)

- [ ] Document **as-built** button, panel, list, modal behaviors per major screens.
- [ ] **§3.3:** separate **Current (legacy)** vs **Target** **ControlPanel** layout per **TECH-07** spec text.

### Phase 4 — Acceptance and IA

- [ ] Update [`projects/ui-as-code-exploration.md`](../../projects/ui-as-code-exploration.md) if inventory changed.
- [ ] Run `npm run generate:ia-indexes` (repo root) after **`ui-design-system.md`** edits; `npm run generate:ia-indexes -- --check`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| **IA** index matches spec | Node | `npm run generate:ia-indexes -- --check` | After **`ui-design-system.md`** body edits |
| **BACKLOG** / **Spec** path | Node | `npm run validate:dead-project-specs` | This file linked from **TECH-68** row |

## 8. Acceptance Criteria

- [ ] **`ui-design-system.md`** **§1** (Foundations) documents **as-built** color, typography, and spacing with **no** remaining **TBD** rows **unless** explicitly marked “varies / unconsolidated” with example surfaces listed.
- [ ] **§4.3** **Canvas Scaler** and **§4.1** naming notes reflect **MainScene** (or primary gameplay **Canvas**) as inspected.
- [ ] **§2** and **§3** describe **current** **HUD**, **toolbar** / **ControlPanel**, and primary **popup** **UX** flows with enough detail for a new developer to match layout intent.
- [ ] **§3.3** clearly distinguishes **as-built** **vs** **target** **toolbar** layout (**TECH-07**).
- [ ] **`npm run generate:ia-indexes -- --check`** passes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- _Fill at closure; migrate durable bullets to **`ui-design-system.md`** **§6** or **glossary** if needed._

## Open Questions (resolve before / during implementation)

None — tooling and documentation scope only. **Player-facing** copy changes belong in **FEAT-**/**BUG-** specs if they alter behavior or text policy.
