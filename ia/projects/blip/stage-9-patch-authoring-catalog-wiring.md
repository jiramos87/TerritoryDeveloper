### Stage 9 — Patches + integration + golden fixtures + promotion / Patch authoring + catalog wiring


**Status:** Done (all tasks archived 2026-04-15 — TECH-209..TECH-212)

**Objectives:** Ten `BlipPatch` SO assets authored + `BlipCatalog.entries[]` wired in Inspector. After this stage all 10 MVP `BlipId` values resolve a non-null patch + non-null `AudioMixerGroup` from the catalog; `BlipEngine.Play` is unblocked but no call sites exist yet.

**Exit:**

- `Assets/Audio/BlipPatches/` dir + 10 `BlipPatch` SO asset files. Each SO: envelope/oscillator/filter params per exploration §9 recipes; `cooldownMs` per Exit criteria (ToolRoadTick 30 ms, WorldCellSelected 80 ms, SysSaveGame 2000 ms; others per §9); `patchHash` non-zero after `OnValidate`.
- `mixerGroup` authoring ref set on each SO per exploration §14 routing table (`Blip-UI` for `UiButtonHover` + `UiButtonClick`; `Blip-World` for `ToolRoad*` + `ToolBuilding*` + `WorldCellSelected`; confirm §14 for Eco/Sys ids).
- `BlipCatalog.entries[]` array populated in Inspector — 10 `BlipPatchEntry` rows (each: `BlipId` enum + `BlipPatch` asset ref). `BlipBootstrap` prefab Catalog + Player child slots confirmed wired.
- PlayMode smoke: `BlipCatalog.IsReady == true`; all 10 ids resolve non-null patch + non-null `AudioMixerGroup` via `BlipMixerRouter`.
- `npm run unity:compile-check` green.
- Phase 1 — Author 10 `BlipPatch` SO assets with envelope/oscillator params + cooldown from §9 recipes.
- Phase 2 — Assign `mixerGroup` refs + wire `BlipCatalog.entries[]` in Inspector + smoke verify.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | UI/Eco/Sys patch SOs | **TECH-209** | Done (archived) | Create `Assets/Audio/BlipPatches/` dir + author 5 UI/Eco/Sys `BlipPatch` SOs via CreateAssetMenu (`Territory/Audio/Blip Patch`): `UiButtonHover` (§9 ex 1), `UiButtonClick` (§9 ex 2), `EcoMoneyEarned` (§9 ex 17), `EcoMoneySpent` (§9 ex 18), `SysSaveGame` (§9 ex 20). Fill all envelope/oscillator/filter/jitter params from §9 recipe table. `patchHash` recomputed on `OnValidate` — verify non-zero in Inspector after fill. |
| T9.2 | World patch SOs | **TECH-210** | Done (archived) | Author 5 World `BlipPatch` SOs: `ToolRoadTick` (§9 ex 5; `cooldownMs` 30), `ToolRoadComplete` (§9 ex 6), `ToolBuildingPlace` (§9 ex 9), `ToolBuildingDenied` (§9 ex 10), `WorldCellSelected` (§9 ex 15; `cooldownMs` 80). Set all envelope/oscillator/filter/jitter/variantCount/voiceLimit params per §9. `patchHash` non-zero after `OnValidate`. |
| T9.3 | MixerGroup refs + catalog wire | **TECH-211** | Done (archived) | Set `mixerGroup` authoring ref on all 10 SOs per exploration §14 routing table (open each SO in Inspector, assign `AudioMixerGroup` from `BlipMixer.mixer` asset). Wire `BlipCatalog.entries[]` in Inspector — 10 `BlipPatchEntry` rows (`BlipId` + `BlipPatch` asset ref). Open `BlipBootstrap` prefab; confirm Catalog + Player child slots populated. |
| T9.4 | PlayMode smoke verify | **TECH-212** | Done (archived) | PlayMode smoke: enter Play Mode, load `MainMenu.unity`, poll `BlipCatalog.IsReady`; for all 10 `BlipId` values assert `catalog.Resolve(id).patchHash != 0` + `catalog.MixerRouter.Get(id) != null`. `npm run unity:compile-check` green. Confirms SO → catalog → mixer-router chain complete before any call site lands. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._
