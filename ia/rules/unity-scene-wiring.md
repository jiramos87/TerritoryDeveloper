---
purpose: "Unity scene-wiring contract for lifecycle agents — triggers, checklist, evidence. Makes scene wiring a first-class Stage deliverable, not a human follow-up."
audience: agent
loaded_by: on-demand
slices_via: none
description: Unity scene-wiring contract — when a Stage ships a runtime object that must live in a scene, the Stage (not the human) wires it.
alwaysApply: false
---

# Unity Scene Wiring Contract

**Principle:** Stage shipping runtime object that must live in Unity scene = NOT shipped until object wired + committed. Agents executing `/ship-stage` (or `/ship` for N=1) own wiring end-to-end — **not** human follow-up.

Historical gap: `grid-asset-visual-registry` Stage 2.2 landed `GridAssetCatalog` scripts + tests + closeout without adding object to `Assets/Scenes/MainScene.unity`. Stage reported PASSED but runtime path dead until human asked about wiring. Rule closes gap.

---

## Triggers (wiring required when ANY fire)

1. **New runtime MonoBehaviour** under `Assets/Scripts/**/*.cs` **not** instantiated programmatically by existing manager/prefab. `Awake`/`Start` reads `[SerializeField]` fields, loads StreamingAssets path, or exposes `UnityEvent` → object must live in scene.
2. **New consumer of `Assets/StreamingAssets/**`** — class reads streaming path → needs scene host so load fires at runtime.
3. **New Inspector-exposed dependency** (`[SerializeField] private T _foo;`) on existing scene object — field must be assigned in scene, not left null.
4. **New prefab required at scene boot** — Stage adds prefab game code assumes exists in hierarchy (not spawned) → prefab must be placed.
5. **New `UnityEvent` hook** firing from scene-level triggers (UI button, scene-lifecycle) — event wired in Inspector.

**Non-triggers (no scene wiring needed):**
- Pure tooling under `tools/**`, `web/**`, `ia/**`, `.claude/**`, `docs/**`.
- ScriptableObject authoring assets under `Assets/**/*.asset` (data, not scene residents).
- Classes instantiated programmatically by existing manager (e.g. `new BondData()` inside `EconomyManager`).
- Editor-only scripts under `Assets/Editor/**` or `Assets/Scripts/Editor/**`.

---

## Target scene resolution

| Object kind | Default scene | Notes |
|-------------|---------------|-------|
| Game-runtime manager / catalog / service (MonoBehaviour feeding gameplay) | `Assets/Scenes/MainScene.unity` under `Game Managers` parent | Sibling of `EconomyManager`, `GridManager`, `CityManager`. |
| Main-menu-only UI / controller | `Assets/Scenes/MainMenu.unity` | Menu-specific widgets. |
| Test-only harness component | `Assets/Scenes/SampleScene.unity` | Rare; only when gameplay doesn't need it. |
| Needed in both menu + main | Boot prefab under `Assets/Prefabs/Boot/**` referenced by both scenes | Escalate to user before introducing — keeps scene delta minimal. |

**Default:** `MainScene.unity` → `Game Managers` parent. Escalate to user only when object is menu-only or needs boot-prefab treatment.

---

## Wiring checklist (all must hold before Stage PASSED)

- [ ] Target scene `.unity` file edited (or new prefab placed) — diff shows new GameObject + component reference.
- [ ] Script reference resolves via script's `.cs.meta` GUID — never paste stale GUID.
- [ ] Every `[SerializeField]` on new object has value matching spec:
  - Paths: string matches `Assets/StreamingAssets/...` relative path.
  - Asset refs: `fileID`/`guid` pair resolves to existing asset on disk.
  - Dev placeholders: `none` only when spec explicitly allows; otherwise assign real default.
- [ ] `UnityEvent` fields populated when spec calls for specific listeners; empty when spec says "assign in Inspector" + no default listener.
- [ ] Parent hierarchy matches target-scene convention (e.g. `Game Managers` for runtime managers).
- [ ] Local `Transform` = identity (`position (0,0,0)`, `rotation (0,0,0)`, `scale (1,1,1)`) unless spec requires otherwise.
- [ ] `npm run unity:compile-check` passes after edit.
- [ ] Changed `.unity` file part of same Stage commit surface (not dangling post-Stage fix).

---

## Bridge vs. text-edit

**Prefer `unity_bridge_command` when MCP bridge available** — kinds documented in [`unity-invariants.md`](unity-invariants.md) cover full wiring path:

`open_scene → create_gameobject → set_gameobject_parent → attach_component → assign_serialized_field → save_scene`.

Bridge emits consistent GUIDs + serialization format, avoids YAML-merge hazards. Escalate only when kind absent (`gap_reason: bridge_kind_missing`).

**Text-edit fallback** (bridge unavailable): edit `.unity` YAML directly, using target scene's existing objects as shape template; confirm script `guid` via adjacent `.cs.meta`. Run `npm run unity:compile-check` after.

---

## Evidence block (emit in §Acceptance + §Code Review)

Every Stage firing wiring trigger must include this block in spec's `§Acceptance` + `opus-code-review` output:

```
Scene wiring:
  scene: Assets/Scenes/{SCENE}.unity
  parent: {Game Managers | ...}
  component: {ComponentName} (script guid {GUID})
  serialized_fields:
    _field_a: "{value}"
    _field_b: "{value or (none — dev)}"
  unity_events: {empty | listener_count: N}
  compile_check: passed
```

Missing block when a trigger fired = `critical` verdict on Stage code review.

---

## Skill integration points

| Skill | Responsibility |
|-------|---------------|
| [`stage-authoring`](../skills/stage-authoring/SKILL.md) | Detect wiring triggers from Task scope; emit **Scene Wiring** mechanical step in `§Plan Digest §Mechanical Steps` with `unity_bridge_command` tuples (or YAML-edit tuples) + `npm run unity:compile-check` gate + evidence block as `after:` literal (target scene, parent, fields, fallback notes). |
| [`project-spec-implement`](../skills/project-spec-implement/SKILL.md) | Execute Scene Wiring step during Task implement; Task exit fails when scene file not edited + triggers fired. |
| [`ship-stage`](../skills/ship-stage/SKILL.md) | Pass 2 cumulative diff must include `.unity` edit per triggered Stage; Step 3.2 code-review acceptance reference checks evidence block. |
| [`opus-code-review`](../skills/opus-code-review/SKILL.md) | `critical` verdict when Stage trigger fired + no `.unity` edit (or no prefab placement) in cumulative diff. |
| [`verify-loop`](../skills/verify-loop/SKILL.md) | Path B / Play Mode evidence: scene-wired component reachable at runtime (e.g. `debug_context_bundle` confirms component present in scene). |

---

## Quick examples

**Ran this Stage (grid-asset-visual-registry 2.2):**

- Trigger #1: `GridAssetCatalog` = new runtime MonoBehaviour with `[SerializeField] _streamingRelativePath`.
- Target scene: `Assets/Scenes/MainScene.unity`.
- Parent: `Game Managers` (sibling of `EconomyManager`, `GridManager`).
- Fields: `_streamingRelativePath = catalog/grid-asset-catalog-snapshot.json`; `_missingSpriteDevPlaceholder = none`; `_onCatalogReloaded = empty UnityEvent`.
- Commit message: `fix(unity): add GridAssetCatalog to MainScene under Game Managers`.

**Does NOT fire (sprite-gen master plan Stage 5):**

- All work under `tools/sprite-gen/**` (pure Python tooling). No `.cs` under `Assets/**`. No trigger. No `§Scene Wiring` required.

---

## Changelog

### 2026-04-22 — Rule introduced

**Symptom:** `grid-asset-visual-registry` Stage 2.2 shipped `GridAssetCatalog` scripts + tests + closeout, emitted `SHIP_STAGE 2.2: PASSED`, but runtime path inert — no agent wired catalog into `MainScene.unity`. Human caught gap next turn.

**Root cause:** No skill owned scene wiring. `stage-authoring` (+ predecessor pair `plan-author` / `plan-digest`) did not author wiring step; `project-spec-implement` had no Task-exit check for `.unity` edits; `opus-code-review` did not flag missing scene wiring as critical; `/ship-stage` Pass 2 acceptance reference did not require evidence block.

**Fix:** Rule + cross-references added to five skills above. Every lifecycle surface now knows trigger → scene wiring → evidence flow. `grid-asset-visual-registry` Stage 2.2 follow-up commit `7143d72` (`fix(unity): add GridAssetCatalog to MainScene under Game Managers`) = canonical example.
