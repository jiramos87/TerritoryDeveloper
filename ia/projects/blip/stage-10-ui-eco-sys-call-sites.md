### Stage 10 — Patches + integration + golden fixtures + promotion / UI + Eco + Sys call sites


**Status:** Done — all tasks archived 2026-04-15 (TECH-215..TECH-218)

**Objectives:** `BlipEngine.Play` wired at MainMenu button hover/click + money earn/spend + save-complete. Six `BlipId` values active in game: `UiButtonHover`, `UiButtonClick`, `EcoMoneyEarned`, `EcoMoneySpent`, `SysSaveGame`. No world-lane sounds yet.

**Exit:**

- `MainMenuController.cs` — `BlipEngine.Play(BlipId.UiButtonClick)` as first statement in each of `OnContinueClicked`, `OnNewGameClicked`, `OnLoadCityClicked`, `OnOptionsClicked`, `CloseLoadCityPanel`, `CloseOptionsPanel`. `EventTrigger` PointerEnter callbacks on each `Button` reference fire `BlipEngine.Play(BlipId.UiButtonHover)` — registered programmatically alongside `onClick.AddListener` calls (get-or-add `EventTrigger` component, add `EventTriggerType.PointerEnter` entry). No new singletons (invariant #4); `BlipEngine` static facade self-caches (invariant #3).
- `EconomyManager.cs` — `BlipEngine.Play(BlipId.EcoMoneyEarned)` after `cityStats.AddMoney(amount)` (line ~205); `BlipEngine.Play(BlipId.EcoMoneySpent)` after `cityStats.RemoveMoney(amount)` in the success branch of `SpendMoney` (line ~169). Monthly-maintenance `SpendMoney` path excluded (non-interactive budget charge — guard by `notifyInsufficientFunds` param or call-context flag).
- `GameSaveManager.cs` — `BlipEngine.Play(BlipId.SysSaveGame)` after `File.WriteAllText` in `SaveGame` (line ~69) and in `TryWriteGameSaveToPath` (line ~91). 2 s cooldown enforced by `BlipCooldownRegistry` via patch SO — no additional guard.
- `npm run unity:compile-check` green.
- Phase 1 — UI lane: `MainMenuController` click + hover call sites.
- Phase 2 — Eco + Sys lane: `EconomyManager` earn/spend + `GameSaveManager` save-complete.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | MainMenu click call sites | **TECH-215** | Done | `MainMenuController.cs` — add `BlipEngine.Play(BlipId.UiButtonClick)` as first statement in each of: `OnContinueClicked`, `OnNewGameClicked`, `OnLoadCityClicked`, `OnOptionsClicked`, `CloseLoadCityPanel`, `CloseOptionsPanel`. No `FindObjectOfType` introduced — `BlipEngine` is static facade (invariant #3). |
| T10.2 | MainMenu hover EventTrigger | **TECH-216** | Done (archived) | `MainMenuController.cs` — in `RegisterButtonListeners` / `Start` (where `onClick.AddListener` calls live, line ~133): for each `Button` field (`continueButton`, `newGameButton`, `loadCityButton`, `optionsButton`, `loadCityBackButton`, `optionsBackButton`), call `GetOrAddComponent<EventTrigger>()` + add `EventTriggerType.PointerEnter` entry invoking `BlipEngine.Play(BlipId.UiButtonHover)`. No new fields; no new singletons (invariant #4). |
| T10.3 | Economy earn/spend call sites | **TECH-217** | Done (archived) | `EconomyManager.cs` — add `BlipEngine.Play(BlipId.EcoMoneyEarned)` after `cityStats.AddMoney(amount)` in `AddMoney` (line ~205). Add `BlipEngine.Play(BlipId.EcoMoneySpent)` after `cityStats.RemoveMoney(amount)` in success branch of `SpendMoney` (line ~169). Monthly-maintenance path (`ChargeMonthlyMaintenance` → `SpendMoney`) must NOT fire — guard with `notifyInsufficientFunds == true` condition or add private overload with `bool fireBlip = true`. |
| T10.4 | SaveGame call sites | **TECH-218** | Done (archived) | `GameSaveManager.cs` — add `BlipEngine.Play(BlipId.SysSaveGame)` after `File.WriteAllText(path, ...)` in `SaveGame` (line ~69) and after equivalent write in `TryWriteGameSaveToPath` (line ~91). 2 s cooldown in patch SO `cooldownMs = 2000`; `BlipCooldownRegistry` gates rapid manual saves — no additional guard. `npm run unity:compile-check` green. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._
