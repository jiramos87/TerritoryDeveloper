---
purpose: "TECH-688 — Author PlacementValidator type with serialized manager refs and stub CanPlace surface."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T3.1.1
---
# TECH-688 — Author PlacementValidator type

> **Issue:** [TECH-688](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Introduce **PlacementValidator** as the single owner of **`CanPlace(assetId, cell, rotation)`** (stub body in this task). Class holds **`GridManager`**, **`GridAssetCatalog`**, **`EconomyManager`** via **`[SerializeField]`** with **`FindObjectOfType`** fallback in **`Awake`** where repo pattern already does so. No direct **`grid.cellArray`** access.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New type under **`Assets/Scripts/Managers/GameManagers/`** (or **`Services/`** sibling per plan).
2. Serialized refs: **`GridManager`**, **`GridAssetCatalog`**, **`EconomyManager`**; XML **`/// <summary>`** on class.
3. Public **`CanPlace`** method present; may return placeholder **`true`** until **TECH-689** adds **`PlacementResult`** / **`PlacementFailReason`**.

### 2.2 Non-Goals (Out of Scope)

1. Zoning, economy, unlock logic (downstream tasks).
2. Ghost tint / tooltip (**Stage 3.2**).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Central validator component exists for placement checks | Spec + compile; refs wired in Inspector pattern |

## 4. Current State

### 4.1 Domain behavior

**Stage 3.1** needs deterministic legality answers; this task only scaffolds the type and dependencies.

### 4.2 Systems map

- New: **`PlacementValidator.cs`**
- Existing: **`GridManager`**, **`GridAssetCatalog`**, **`EconomyManager`**
- Ref: **`docs/grid-asset-visual-registry-exploration.md`** §8.3

### 4.3 Implementation investigation notes (optional)

Match **`ZoneSubTypeRegistry`** / catalog injection style (**`[SerializeField]`** + fallback) from prior grid-asset stages.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible behavior change until **`CanPlace`** is wired; scaffold only.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

**`MonoBehaviour`** on same GameObject cluster as other managers OR plain service if project prefers; follow existing **`GameManagers`** layout.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Stub **`CanPlace`** returns true | Unblocks **TECH-689** result shape | Throw NotImplemented — rejected (breaks compile path) |

## 7. Implementation Plan

### Phase 1 — Type scaffold

- [ ] Create **`PlacementValidator.cs`** with class-level XML summary.
- [ ] Add **`[SerializeField]`** refs + **`Awake`** resolution fallback.
- [ ] Add stub **`CanPlace(int assetId, Vector3Int cell, int rotation)`** (signature adjustable to project grid types — align with **`GridManager`** APIs).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Compile + Inspector pattern | Unity | `npm run unity:compile-check` | After C# land |

## 8. Acceptance Criteria

- [ ] PlacementValidator lives under **`Assets/Scripts/Managers/GameManagers/`** (or **`Services/`** sibling).
- [ ] SerializeField refs: **`GridManager`**, **`GridAssetCatalog`**, **`EconomyManager`**; **`Awake`** fallback where pattern exists.
- [ ] Public **`CanPlace`** signature reserved; body may return placeholder true until **TECH-689**.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

`PlacementValidator` MonoBehaviour in GameManagers carries serialized **`GridManager`**, **`GridAssetCatalog`**, **`EconomyManager`**, `Awake` fallback, stub **`CanPlace`** until **TECH-689** replaces return shape. Exploration doc carries implementation pointer. Component wired under **`Game Managers`** in **`MainScene`**.

### §Acceptance

- [ ] `Assets/Scripts/Managers/GameManagers/PlacementValidator.cs` matches §7 (summary, trio, stub API)
- [ ] `Assets/Scenes/MainScene.unity` shows component under `Game Managers` with refs assigned per scene-wiring checklist
- [ ] `npm run unity:compile-check` exits 0 (close other Unity instances if batchmode reports lock)

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| compile_gate | n/a | exit 0 | `npm run unity:compile-check` |

### §Examples

| Case | Result |
|------|--------|
| SerializeFields set in Inspector | `Awake` skips `FindObjectOfType` for that ref |
| Ref null in `Awake` | One-time resolve per ref |

### §Mechanical Steps

#### Step 1 — Exploration anchor

**Goal:** Link exploration §8.3 to filed spec for traceability.

**Edits:**

- `docs/grid-asset-visual-registry-exploration.md` — **before:**
```
### 8.3 Subsystem impact
```
**after:**
```
### 8.3 Subsystem impact

<!-- TECH-688: PlacementValidator implementation — ia/projects/TECH-688.md §7 -->
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** On failure, fix broken `spec:` paths in `ia/backlog/*.yaml` then re-run gate.

#### Step 2 — Scene wiring

**Goal:** Host runtime component per `ia/rules/unity-scene-wiring.md` target table (Game-runtime manager → `MainScene` / `Game Managers`).

**Edits:**

- `Assets/Scenes/MainScene.unity` — prefer `unity_bridge_command` chain `open_scene` → `create_gameobject` → `set_gameobject_parent` (`Game Managers`) → `attach_component` (`PlacementValidator`) → `assign_serialized_field` (gridManager, catalog, economyManager) → `save_scene`. Text-edit fallback: copy existing manager GameObject YAML stanza; set script `guid` from `PlacementValidator.cs.meta`.

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** If batchmode aborts on project lock, quit Editor or pass `-quit` per verify policy; re-run gate after lock clears.

**MCP hints:** `unity_bridge_command`, `get_compilation_status`

## Open Questions (resolve before / during implementation)

1. Exact **`CanPlace`** parameter types (**`Vector3Int`** vs **`Cell`** DTO) — align with **`GridManager`** public API at implement time.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
