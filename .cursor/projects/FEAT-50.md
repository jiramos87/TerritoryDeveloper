# FEAT-50 — UI visual polish: aesthetic refinement (HUD, panels, toolbar, MainMenu)

> **Issue:** [FEAT-50](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04

<!--
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Use glossary terms: ../../.cursor/specs/glossary.md (spec wins if glossary differs).
-->

## 1. Summary

Deliver a coordinated **player**-visible **UI** aesthetic upgrade across **`MainMenu`**, **city** **HUD**, **ControlPanel** / **toolbar**, and shared **panels**—grounded in **ui-design-system** **Foundations** and the committed **as-built** inventory JSON. This issue is intentionally narrower than the **UI-as-code** program **capstone** (normative **`ui-design-system.md`** **§5.2**): it optimizes look-and-feel (palette, type rhythm, spacing, iconography, contrast, optional motion) rather than **infrastructure** (**UiTheme**, **`UIManager`** layout, **modal** stack policy, **Editor** scaffold menus). Product direction and trade-offs are captured in [`docs/ui-visual-polish-exploration-FEAT-50.md`](../../docs/ui-visual-polish-exploration-FEAT-50.md).

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Coherent visual language** across primary surfaces (**MainMenu**, **MainScene** **Canvas** roots) so **HUD** stats, **toolbar** chrome, and **popup**/**panel** chrome feel like one product.
2. **Readable hierarchy**: primary vs muted **text**, **surface** contrast, and touch/click targets that remain comfortable at the **Canvas** scaler targets already used in scenes.
3. **Spec traceability**: where polish changes **color** or **typography** norms, update **ui-design-system** **as-built** tables and refresh **`docs/reports/ui-inventory-as-built-baseline.json`** (re-run **Export UI Inventory** when that workflow is available).
4. **Accessibility-minded defaults**: sufficient contrast for core **HUD** readouts and **toolbar** labels on representative backgrounds (exact ratios recorded in the exploration doc or **Decision Log**).

### 2.2 Non-Goals (Out of Scope)

1. **Simulation**, **grid**, **road**, **water**, or **Save data** behavior changes.
2. Replacing **UI-as-code** **capstone** scope: no requirement to finish **`UIManager`** splits, **MCP** **`ui_theme_tokens`**, or **Editor** automation unless this issue explicitly extends into a later phase.
3. Full **TextMeshPro** migration of the **city** **HUD** (per **`ui-design-system.md`** **§1.2**—legacy **`UnityEngine.UI.Text`** remains unless a future issue scopes **TMP**).
4. New **gameplay** **modal** flows or input routing changes—coordinate with **BUG-19** instead of solving scroll vs **camera** here.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|----------------------|
| 1 | Player | I want the **HUD** and menus to look intentional and easy to scan so that long sessions feel less fatiguing. | Core stat **panels** and **MainMenu** show updated styling per **§8**; no loss of legibility on reference resolution. |
| 2 | Developer / agent | I want **ui-design-system** and the baseline JSON to reflect shipped **UI** tokens so automated inventory and specs stay trustworthy. | **§8** includes doc/JSON updates when **Graphic.color** / font sizes shift materially. |

## 4. Current State

### 4.1 Domain behavior

**As-built** **color** and **typography** frequencies are summarized in **ui-design-system** **§1** and sourced from **`docs/reports/ui-inventory-as-built-baseline.json`**. **`UiTheme`** and prefab **v0** are **shipped** (**§5.2**); **FEAT-50** layers visual refinement on those assets or on scene-authored widgets, depending on schedule (**Open Questions**).

### 4.2 Systems map

| Area | Pointers |
|------|----------|
| Reference spec | `.cursor/specs/ui-design-system.md` (**Foundations**, components, patterns) |
| Program charter | **glossary** **UI-as-code program**; **`ui-design-system.md`** **Overview** + **Codebase inventory (uGUI)** |
| Structural UI baseline | **`ui-design-system.md`** **§5.2**; **glossary** **UI-as-code program** |
| Critique trace | `docs/ui-as-built-ui-critique.md` |
| Exploration | `docs/ui-visual-polish-exploration-FEAT-50.md` |

### 4.3 Implementation investigation notes (optional)

Prefer **Prefab**-level styling and shared materials/sprites over per-scene one-offs. Prefer binding polish constants to **`UiTheme`** where practical to avoid duplicate sources of truth.

## 5. Proposed Design

### 5.1 Target behavior (product)

**Player**-visible **UI** adopts an agreed mood (documented in the exploration file): e.g. warmer/cooler neutrals, restrained accent use, consistent corner radii or sprite borders, and optional subtle transitions that do not block input. Exact numbers are **Target** rows in **ui-design-system** until shipped, then folded into **as-built**.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation order is **agent**-chosen: scene **Prefab** swaps, shared **Image** / **Text** presets, **`UiTheme`** fields when available, or **Animator**-light **CanvasGroup** fades. Respect **invariants**: no new **singletons**; no **`FindObjectOfType`** in per-frame **UI** paths (**BUG-14**).

### 5.3 Method / algorithm notes (optional)

None required at kickoff.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Issue id **FEAT-50** | Next free **FEAT-** id after **FEAT-48** and reserved **FEAT-49** spec file. | **ART-** prefix (rejected: scope spans **UX** + spec, not only art assets). |

## 7. Implementation Plan

### Phase 1 — Direction lock

- [ ] Review [`docs/ui-visual-polish-exploration-FEAT-50.md`](../../docs/ui-visual-polish-exploration-FEAT-50.md); narrow mood, constraints, and “do not change” list.
- [ ] Confirm whether polish binds to **`UiTheme`** / prefab **v0** first (parallel vs sequential) and record in **Decision Log**.

### Phase 2 — **MainMenu** + **city** **HUD** pass

- [ ] Apply agreed **token** updates to **`MainMenu`** and primary **HUD** **Canvas** nodes (prefer **Prefabs**).
- [ ] Spot-check **Canvas Scaler** reference resolution from **ui-design-system** **§4**.

### Phase 3 — **Toolbar** / **ControlPanel** + secondary **panels**

- [ ] Align **toolbar** chrome with the same **palette** / spacing rules.
- [ ] Verify **popup**/**panel** readability over **game** view (no **simulation** changes).

### Phase 4 — Documentation + baseline

- [ ] Update **ui-design-system** **as-built** tables for any adopted **token** changes.
- [ ] Regenerate **`docs/reports/ui-inventory-as-built-baseline.json`** when the **Editor** export path is used.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|--------|
| **Spec:** links and **Spec:** paths valid | Node | `npm run validate:dead-project-specs` (repo root) | After **BACKLOG** / **Spec** edits |
| Visual regression | Manual / Play Mode | Unity playthrough **MainMenu** → **MainScene** | Check **HUD**, **toolbar**, key **modals** at reference scale |
| **IA** index drift (if glossary/spec bodies edited) | Node | `npm run validate:all` | Only if **§4** deliverables touch indexed docs |

## 8. Acceptance Criteria

- [ ] **Player**-visible **UI** on **`MainMenu`** and **`MainScene`** reflects the agreed aesthetic direction in the exploration doc.
- [ ] No regressions to **simulation**, **grid** interaction, or **Save**/**Load** attributable to this work.
- [ ] **ui-design-system** and (when run) **`docs/reports/ui-inventory-as-built-baseline.json`** match shipped **color**/**typography** for changed widgets.
- [ ] **BUG-19** / **camera** vs scroll behavior unchanged or explicitly coordinated (no accidental **modal** scroll capture).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. Should polish ship **after** binding to **`UiTheme`** / prefab **v0**, or start on the current hierarchy and re-apply once tokens stabilize (**schedule**, not **simulation**)?
2. What minimum **Canvas** / safe-area assumptions must the polished **HUD** respect for the supported aspect ratios and scaler mode?
3. For **modal**-heavy surfaces, should motion (**CanvasGroup**, transitions) be **on**, **minimal**, or **off** by default for performance and clarity?
