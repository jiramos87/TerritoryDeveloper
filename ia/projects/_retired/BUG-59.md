---
purpose: "BUG-59 — Restore MainMenu Options + NewGame click handlers — cross-scene UIManager.Instance gap."
audience: both
loaded_by: ondemand
slices_via: none
---
# BUG-59 — Restore MainMenu Options + NewGame click handlers — cross-scene UIManager.Instance gap

> **Issue:** [BUG-59](../../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-29
> **Last updated:** 2026-04-29

<!--
  Filename: `ia/projects/{ISSUE_ID}-{description}.md` (e.g. `BUG-37-zone-cleanup.md`,
  `FEAT-44-water-junction.md`). Legacy bare `{ISSUE_ID}.md` accepted for back-compat.
  Structure guide: ../../docs/PROJECT-SPEC-STRUCTURE.md
  Glossary: ../specs/glossary.md (spec wins on conflict).
  Separate product behavior (§1–5.1, §8, Open Questions) from impl notes (§5.2+, §7, optional "Implementation investigation").
  Authoring style: caveman prose (drop articles/filler/hedging; fragments OK). Tables, code, seed prompts stay normal.
-->

## 1. Summary

Restore MainMenu click handlers (Options + NewGame) to legacy MainMenu-scoped panels. Stage 12 broke handlers by calling UIManager.Instance (only in CityScene) from MainMenu scene where UIManager absent.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Options + NewGame buttons responsive in MainMenu
2. Legacy MainMenu panels (optionsPanel / newGamePanel) reactivated
3. Stage 8 modal paths (PauseMenu, InfoPanel) unaffected

### 2.2 Non-Goals (Out of Scope)

1. Refactor UIManager singleton into MainMenu scene
2. Duplicate modal roots across scenes
3. Redesign modal system

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | Click Options → menu appears | optionsPanel visible; settings accessible |
| 2 | Player | Click NewGame → menu appears | newGamePanel visible; game start possible |

## 4. Current State

### 4.1 Domain behavior

Observed: MainMenu Options + NewGame buttons silent. UIManager.Instance == null in MainMenu scene. No exceptions logged (null guard active). Expected: Buttons invoke legacy MainMenu-scoped panels.

### 4.2 Systems map

Backlog Files: `Assets/Scripts/Managers/GameManagers/MainMenuController.cs` (lines 557, 693).

Related: Stage 8 modal handlers (PauseMenu, InfoPanel) use UIManager via CityScene context.

### 4.3 Implementation investigation notes

Two handler paths in MainMenuController:
- OnOptionsClicked (line 693) → UIManager.Instance.OpenPopup(SettingsScreen)
- OnNewGameClicked (line 557) → UIManager.Instance.OpenPopup(NewGameScreen)

MainMenu scene has legacy method EnsureSerializedMenuPanels (line 119) + CreateOptionsPanel (line 141) that construct optionsPanel / newGamePanel as GameObject instances. Revert handlers to call these legacy paths.

## 5. Proposed Design

### 5.1 Target behavior (product)

Options button click → optionsPanel appears (MainMenu-scoped, legacy path). NewGame button click → newGamePanel appears (MainMenu-scoped, legacy path). No regressions in Stage 8 in-game modal flows.

### 5.2 Architecture / implementation

OnOptionsClicked + OnNewGameClicked call legacy panel activation methods instead of UIManager.Instance. MainMenu scene GameObject references preserved. Stage 8 UIManager flows isolated to CityScene.

### 5.3 Method / algorithm notes

Pseudo-code:
```
OnOptionsClicked():
  if (!optionsPanel) optionsPanel = CreateOptionsPanel()
  optionsPanel.SetActive(true)
```

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-29 | Revert to legacy MainMenu panels | UIManager only in CityScene; MainMenu scene needs self-contained modals | Refactor UIManager to MainMenu (larger scope, deferred to Stage 14) |

## 7. Implementation Plan

### Phase 1 — Revert handler paths

- [ ] Open MainMenuController.cs (line 557)
- [ ] Revert OnNewGameClicked to legacy panel activation
- [ ] Open MainMenuController.cs (line 693)
- [ ] Revert OnOptionsClicked to legacy panel activation
- [ ] Verify EnsureSerializedMenuPanels + CreateOptionsPanel intact

### Phase 2 — Test in-game Stage 8 flows unaffected

- [ ] PauseMenu Esc handler works (UIManager.Instance context)
- [ ] InfoPanel Alt+click handler works
- [ ] Save/Load menus accessible via PauseMenu

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Handlers revert to legacy MainMenu paths | Code review | Manual inspection of MainMenuController.cs lines 557, 693 | Validate against Stage 8 modal spec |
| Play mode: Options button responsive | Unity Play | Click MainMenu Options → optionsPanel visible | No null-ref in console |
| Play mode: NewGame button responsive | Unity Play | Click MainMenu NewGame → newGamePanel visible | No null-ref in console |
| Stage 8 in-game modals unaffected | Unity Play | Esc → PauseMenu; Alt+click → InfoPanel | UIManager paths isolated to CityScene |

## 8. Acceptance Criteria

- [ ] Click MainMenu Options button → optionsPanel appears (legacy MainMenu-scoped)
- [ ] Click MainMenu NewGame button → newGamePanel appears (legacy MainMenu-scoped)
- [ ] No null-ref exceptions logged
- [ ] Stage 8 modal handler paths (PauseMenu, InfoPanel) unaffected

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Digest

_pending — populated by `/stage-authoring {MASTER_PLAN_PATH} {STAGE_ID}`. Sub-sections: §Goal / §Acceptance / §Test Blueprint / §Examples / §Mechanical Steps (each step carries Goal / Edits / Gate / STOP / MCP hints). Template: `ia/templates/plan-digest-section.md`._

### §Goal

### §Acceptance

### §Test Blueprint

### §Examples

### §Mechanical Steps

## Open Questions (resolve before / during implementation)

None — bug fix with clear legacy path. See §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
