### Stage 8 — Bake + facade + PlayMode smoke / PlayMode smoke test


**Status:** Done — TECH-196..TECH-199 all archived 2026-04-15.

**Objectives:** New PlayMode test asmdef + smoke fixture exercises full boot path through `BlipEngine.Play` in-scene. Verifies: catalog ready flag, 10 MVP id resolution, mixer routing, 16-source pool non-exhaustion under rapid play, 17th-call cooldown block. Uses `[UnityTest]` + `yield return null` frame waits; no audio listener output assertions (headless-safe).

**Exit:**

- `Assets/Tests/PlayMode/Audio/Blip.Tests.PlayMode.asmdef` — Editor+Standalone, `testAssemblies: true`, refs `Blip` runtime asmdef + `UnityEngine.TestRunner` + `nunit.framework`.
- `BlipPlayModeSmokeTests.cs` fixture loads `MainMenu.unity` (boot scene, build index 0) + polls `BlipCatalog.IsReady`.
- All 10 MVP `BlipId` rows resolve non-null patch + non-null `AudioMixerGroup`.
- 16 rapid plays on a zero-cooldown fixture `BlipId` advance `BlipPlayer._cursor` full wrap w/o exception; 17th play within `cooldownMs` window returns silently (no `AudioSource.Play` increment observed).
- `npm run unity:compile-check` green after fixture lands.
- Phase 1 — PlayMode asmdef + boot-scene fixture setup.
- Phase 2 — Resolution + routing + pool + cooldown assertions.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | PlayMode asmdef bootstrap | **TECH-196** | Done (archived) | New file `Assets/Tests/PlayMode/Audio/Blip.Tests.PlayMode.asmdef` w/ `"testAssemblies": true`, `"includePlatforms": ["Editor"]` (PlayMode runs in Editor per Unity conv), references: `Blip` runtime asmdef (GUID) + `UnityEngine.TestRunner` + `UnityEditor.TestRunner` + `nunit.framework`. Create companion `.meta`. Empty placeholder `Assets/Tests/PlayMode/Audio/BlipPlayModeSmokeTests.cs` declaring `public sealed class BlipPlayModeSmokeTests` + namespace to anchor asmdef resolution. |
| T8.2 | Boot-scene fixture SetUp | **TECH-197** | Done (archived) | In `BlipPlayModeSmokeTests`: `[UnitySetUp] public IEnumerator SetUp()` → `SceneManager.LoadScene("MainMenu", LoadSceneMode.Single)` then `yield return null` × 2 (one frame for `Awake` cascade, one for ready flag). Assert `BlipBootstrap.Instance != null`, `Object.FindObjectOfType<BlipCatalog>().IsReady == true`. Hold catalog + player refs as private fields for per-test access. `[UnityTearDown]` unloads scene cleanly. |
| T8.3 | Resolution + routing assertions | **TECH-198** (archived) | Done (archived) | `[UnityTest] public IEnumerator Play_AllMvpIds_ResolvesAndRoutes()` — for each `BlipId` in `{UiButtonHover, UiButtonClick, ToolRoadTick, ToolRoadComplete, ToolBuildingPlace, ToolBuildingDenied, WorldCellSelected, EcoMoneyEarned, EcoMoneySpent, SysSaveGame}`: assert `catalog.Resolve(id)` returns non-null patch ref (patchHash != 0), `catalog.MixerRouter.Get(id) != null`, `Assert.DoesNotThrow(() => BlipEngine.Play(id))`. `yield return null` once after loop to drain AudioSource.Play side-effects. |
| T8.4 | Pool + cooldown assertions | **TECH-199** (archived) | Done (archived) | `[UnityTest] public IEnumerator Play_RapidFire_ExhaustsPoolAndBlocksOnCooldown()` — use a fixture `BlipId` w/ near-zero cooldown (e.g. `ToolRoadTick` 30 ms) plus one w/ long cooldown. Fire 16 rapid `BlipEngine.Play(tickId)` within one frame (no yield between) — assert no exception + `player._cursor == 0` after wrap (expose via `internal` accessor). For cooldown: fire `Play(longCooldownId)` once, immediately fire again; verify second call returns silently (track `catalog.CooldownRegistry` last-play dict didn't update, OR expose a debug counter `BlipCooldownRegistry.BlockedCount` incremented on block). |

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


**Status:** Final
