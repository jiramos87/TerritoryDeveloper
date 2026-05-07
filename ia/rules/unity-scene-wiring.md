---
purpose: "Unity scene-wiring contract for lifecycle agents — triggers, checklist, evidence. Makes scene wiring a first-class Stage deliverable, not a human follow-up."
audience: agent
loaded_by: on-demand
slices_via: none
description: Unity scene-wiring contract — when a Stage ships a runtime object that must live in a scene, the Stage (not the human) wires it.
alwaysApply: false
---

# Unity Scene Wiring Contract

**Principle:** A Stage that ships a runtime object which must live in a Unity scene has NOT shipped until that object is wired into the scene and committed. Agents executing `/ship-stage` (or `/ship` for N=1) own this wiring end-to-end — it is **not** a human follow-up.

Historical gap: `grid-asset-visual-registry` Stage 2.2 landed `GridAssetCatalog` C# scripts + tests + closeout without adding the object to `Assets/Scenes/CityScene.unity`. The Stage reported PASSED but the runtime path was dead until a human asked about wiring. This rule closes that gap.

---

## Triggers (wiring required when ANY fire)

1. **New runtime MonoBehaviour** under `Assets/Scripts/**/*.cs` that is **not** instantiated programmatically by an existing manager/prefab. If `Awake`/`Start` reads `[SerializeField]` fields, loads a StreamingAssets path, or exposes a `UnityEvent`, the object must live in a scene.
2. **New consumer of `Assets/StreamingAssets/**`** — if the class reads a streaming path, it needs a scene host so the load actually fires at runtime.
3. **New Inspector-exposed dependency** (`[SerializeField] private T _foo;`) on an existing scene object — the field must be assigned in the scene, not left null.
4. **New prefab required at scene boot** — if Stage work adds a prefab that game code assumes exists in the hierarchy (not spawned), the prefab must be placed.
5. **New `UnityEvent` hook** that must fire from scene-level triggers (UI button, scene-lifecycle) — the event must be wired in the Inspector.

**Non-triggers (no scene wiring needed):**
- Pure tooling under `tools/**`, `web/**`, `ia/**`, `.claude/**`, `docs/**`.
- ScriptableObject authoring assets under `Assets/**/*.asset` (data, not scene residents).
- Classes instantiated programmatically by an existing manager (e.g. `new BondData()` inside `EconomyManager`).
- Editor-only scripts under `Assets/Editor/**` or `Assets/Scripts/Editor/**`.

---

## Target scene resolution

| Object kind | Default scene | Notes |
|-------------|---------------|-------|
| Game-runtime manager / catalog / service (MonoBehaviour that feeds gameplay) | `Assets/Scenes/CityScene.unity` under `Game Managers` parent | Sibling of `EconomyManager`, `GridManager`, `CityManager`. |
| Main-menu-only UI / controller | `Assets/Scenes/MainMenu.unity` | Examples: menu-specific widgets. |
| Test-only harness component | `Assets/Scenes/SampleScene.unity` | Rare; only when gameplay doesn't need it. |
| Needed in both menu + main | Boot prefab under `Assets/Prefabs/Boot/**` referenced by both scenes | Escalate to user before introducing — keeps scene delta minimal. |

**Default:** `CityScene.unity` → `Game Managers` parent. Escalate to user only when the object is menu-only or needs boot-prefab treatment.

---

## Wiring checklist (all must hold before Stage PASSED)

- [ ] Target scene `.unity` file edited (or new prefab placed) — diff shows the new GameObject + component reference.
- [ ] Script reference resolves via the script's `.cs.meta` GUID — never paste a stale GUID.
- [ ] Every `[SerializeField]` on the new object has a value that matches the spec:
  - Paths: string matches `Assets/StreamingAssets/...` relative path.
  - Asset refs: `fileID`/`guid` pair resolves to an existing asset on disk.
  - Dev placeholders: `none` is acceptable only when the spec explicitly allows it; otherwise assign the real default.
- [ ] `UnityEvent` fields populated when the spec calls for specific listeners; empty when the spec says "assign in Inspector" and there is no default listener.
- [ ] Parent hierarchy matches the target-scene convention (e.g. `Game Managers` for runtime managers).
- [ ] Local `Transform` = identity (`position (0,0,0)`, `rotation (0,0,0)`, `scale (1,1,1)`) unless the spec requires otherwise.
- [ ] `npm run unity:compile-check` passes after the edit.
- [ ] Changed `.unity` file is part of the same Stage commit surface (not a dangling post-Stage fix).

---

## Bridge vs. text-edit

**Prefer `unity_bridge_command` when the MCP bridge is available** — same kinds already documented in [`unity-invariants.md`](unity-invariants.md) cover the full wiring path:

`open_scene → create_gameobject → set_gameobject_parent → attach_component → assign_serialized_field → save_scene`.

Bridge emits consistent GUIDs + serialization format and avoids YAML-merge hazards.

**Cabinet-gap protocol — agent owns wiring end-to-end. No checklist handoff to human.**

When agent hits missing bridge kind:
1. Stop wiring attempt.
2. Propose new bridge kind stub: handler signature in `Assets/Scripts/Editor/Bridge/UiBakeHandler.*.cs` (or sibling handler) + tool registration in `tools/mcp-ia-server/src/index.ts` + value_kind enum extension if needed.
3. Land proposal as a discrete commit (or task) with `gap_reason: bridge_kind_missing` annotation.
4. Re-attempt wiring with the new kind.
5. Only escalate to human when the proposal itself is blocked (e.g. fundamental Unity API gap, not just absent code).

Text-edit fallback on `.unity` YAML is **last-resort** — only when bridge round-trip itself unavailable (Editor offline + agent must continue). Confirm script `guid` via adjacent `.cs.meta`. Run `npm run unity:compile-check` after.

---

## Evidence block (emit in §Acceptance + §Code Review)

Every Stage that fired a wiring trigger must include this block in the spec's `§Acceptance` and in `opus-code-review` output:

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
| [`stage-authoring`](../skills/stage-authoring/SKILL.md) | Detect wiring triggers from Task scope; emit a **Scene Wiring** mechanical step in `§Plan Digest §Mechanical Steps` with `unity_bridge_command` tuples (or YAML-edit tuples) + `npm run unity:compile-check` gate + evidence block as `after:` literal (target scene, parent, fields, fallback notes). |
| [`project-spec-implement`](../skills/project-spec-implement/SKILL.md) | Execute the Scene Wiring step during Task implement; Task exit fails if the scene file was not edited when triggers fired. |
| [`ship-stage`](../skills/ship-stage/SKILL.md) | Pass 2 cumulative diff must include a `.unity` edit per triggered Stage; Step 3.2 code-review acceptance reference checks the evidence block. |
| [`opus-code-review`](../skills/opus-code-review/SKILL.md) | `critical` verdict when a Stage trigger fired but no `.unity` edit (or no prefab placement) is in the cumulative diff. |
| [`verify-loop`](../skills/verify-loop/SKILL.md) | Path B / Play Mode evidence: the scene-wired component is reachable at runtime (e.g. `debug_context_bundle` confirms component present in scene). |

---

## Quick examples

**Ran this Stage (grid-asset-visual-registry 2.2):**

- Trigger #1: `GridAssetCatalog` is a new runtime MonoBehaviour with `[SerializeField] _streamingRelativePath`.
- Target scene: `Assets/Scenes/CityScene.unity`.
- Parent: `Game Managers` (sibling of `EconomyManager`, `GridManager`).
- Fields: `_streamingRelativePath = catalog/grid-asset-catalog-snapshot.json`; `_missingSpriteDevPlaceholder = none`; `_onCatalogReloaded = empty UnityEvent`.
- Commit message: `fix(unity): add GridAssetCatalog to CityScene under Game Managers`.

**Does NOT fire (sprite-gen master plan Stage 5):**

- All work lives under `tools/sprite-gen/**` (pure Python tooling). No `.cs` under `Assets/**`. No trigger. No `§Scene Wiring` required.

---

## Changelog

### 2026-05-02 — Cabinet-gap protocol + agent-owned wiring tightened

**Symptom:** Session ended with agent handing human a "checklist of which scene object goes in which slot" for HudBarDataAdapter — wrong tactic. User: *"All unity editor interaction should be performed by the agents via unity bridge tools."* Prior framing in `agent-principles.md` line 13 said *"escalate to human only when kind doesn't exist"* — left an out for agent to bail.

**Fix:** Flipped to "propose new bridge kind, then continue". Added §Cabinet-gap protocol above. Agent must author handler + registration stub before escalating. Text-edit fallback demoted to last-resort.

**Findings — bridge cabinet audit vs HudBarDataAdapter (22 SerializeFields):**
- 21 fields covered by existing `assign_serialized_field` value_kind: `asset_ref` (CityStats, UiTheme), `component_ref` (managers + StudioControls + IlluminatedButtons), `object_ref` (GameObject roots).
- 1 field GAP: `_speedButtons` is `IlluminatedButton[]` length 5. `assign_serialized_field` value_kind enum has no array variant.
- **Mitigation found, no new kind needed yet:** Unity SerializedProperty path syntax `_speedButtons.Array.size` then `_speedButtons.Array.data[i]` reaches array elements via existing `field_name` plumbing. Test before proposing new kind. If it fails, propose `assign_serialized_field_array` with `value_kind: component_ref_array` + handler that calls `arraySize` then iterates.

**Cross-link:** Audience split for chat vs docs — `feedback_simple_product_language.md` (Javier reads chat, agents read docs).

### 2026-04-22 — Rule introduced

**Symptom:** `grid-asset-visual-registry` Stage 2.2 shipped `GridAssetCatalog` scripts + tests + closeout, emitted `SHIP_STAGE 2.2: PASSED`, but the runtime path was inert because no agent wired the catalog into `CityScene.unity`. Human caught the gap on the next turn.

**Root cause:** No skill owned scene wiring. `stage-authoring` (and predecessor pair `plan-author` / `plan-digest`) did not author a wiring step; `project-spec-implement` had no Task-exit check for `.unity` edits; `opus-code-review` did not flag missing scene wiring as critical; `/ship-stage` Pass 2 acceptance reference did not require the evidence block.

**Fix:** This rule + cross-references added to the five skills above. Every lifecycle surface now knows the trigger → scene wiring → evidence flow. `grid-asset-visual-registry` Stage 2.2 follow-up commit `7143d72` (`fix(unity): add GridAssetCatalog to CityScene under Game Managers`) is the canonical example.
