# grid-asset-visual-registry — Stage 3.1 Plan Digest

Compiled 2026-04-22 from 5 task spec(s).

Orchestrator: `ia/projects/grid-asset-visual-registry-master-plan.md`.

---

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

---
## §Plan Digest

### §Goal

`PlacementFailReason` enum + `PlacementResult` (or struct) with XML docs; `CanPlace` returns structured outcome; EditMode tests table-driven in existing Economy EditMode assembly.

### §Acceptance

- [ ] Enum values: footprint, zoning, locked, unaffordable, occupied (+ ok/none per design)
- [ ] Public API XML complete on `CanPlace` and result types
- [ ] EditMode test file extends coverage; `npm run unity:compile-check` exits 0

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| reason_matrix | stubbed validator deps | each reason reachable | Unity EditMode |
| compile_gate | n/a | exit 0 | `npm run unity:compile-check` |

### §Examples

| Reason | When set |
|--------|----------|
| Zoning | Channel mismatch (wired in TECH-690) |
| Unaffordable | Treasury check fails (TECH-691) |

### §Mechanical Steps

#### Step 1 — Structured return type

**Goal:** Replace bool stub with `PlacementResult` carrying reason + optional detail string.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/PlacementValidator.cs` — **before:**
```
        public bool CanPlace(int assetId, Vector2 gridPosition, int rotation)
        {
            return true;
        }
```
**after:**
```
        public PlacementResult CanPlace(int assetId, Vector2 gridPosition, int rotation)
        {
            return PlacementResult.Allowed();
        }
```
(Define `PlacementFailReason`, `PlacementResult`, and factory/helpers immediately above `PlacementValidator` class in the same file per §7.)

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** On compile error, align namespaces and usings with `Territory.Core` / `Territory.Economy` then re-run gate.

#### Step 2 — EditMode tests

**Goal:** Table-driven tests lock reason enum behavior independent of Play Mode.

**Edits:**

- `Assets/Tests/EditMode/Economy/ZoneSServicePlacementTests.cs` — **before:**
```
        private static T GetPrivateField<T>(System.Type type, object target, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Reflection: {fieldName} not found on {type.Name}");
            return (T)field.GetValue(target);
        }
    }
}
```
**after:**
```
        private static T GetPrivateField<T>(System.Type type, object target, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Reflection: {fieldName} not found on {type.Name}");
            return (T)field.GetValue(target);
        }

        // TECH-689: add PlacementValidator reason tests in dedicated fixture when ready; placeholder anchor for plan-digest.
    }
}
```

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** If test assembly fails to compile, add a new EditMode fixture under `Assets/Tests/EditMode/Economy/` per §7 after the file exists on disk, then replace this anchor.

---
## §Plan Digest

### §Goal

Zone S commit path (`PlaceStateServiceZoneAt` or successor) consults `PlacementValidator` before mutating `CityCell`; blocks illegal zoning channel match using `PlacementResult` from **TECH-689**; no `grid.cellArray` access inside validator.

### §Acceptance

- [ ] `ZoneManager` (or documented alternate Zone S commit site) calls validator before state mutation
- [ ] Failure maps to zoning-related `PlacementFailReason`
- [ ] `npm run unity:compile-check` exits 0

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| manual_zone_s | disallowed channel | commit blocked | Editor checklist in §8 |
| compile_gate | n/a | exit 0 | `npm run unity:compile-check` |

### §Examples

| Site | Hook |
|------|------|
| `PlaceStateServiceZoneAt` | Guard immediately after `GetCell` null check |

### §Mechanical Steps

#### Step 1 — Inject validator dependency

**Goal:** `ZoneManager` holds optional `PlacementValidator` reference resolved once.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/ZoneManager.cs` — **before:**
```
    public InterstateManager interstateManager;
    public SlopePrefabRegistry slopePrefabRegistry;
    #endregion
```
**after:**
```
    public InterstateManager interstateManager;
    public SlopePrefabRegistry slopePrefabRegistry;
    [SerializeField] private PlacementValidator placementValidator;
    #endregion
```

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** On compile error, add `using` only if needed (types live in `Territory.Core`) then re-run gate.

#### Step 2 — Guard commit path

**Goal:** Abort Zone S placement when validator denies.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/ZoneManager.cs` — **before:**
```
        if (cell == null) return false;

        cell.zoneType = zoneType;
```
**after:**
```
        if (cell == null) return false;

        if (placementValidator != null)
        {
            // TECH-690: call CanPlace with catalog-backed assetId + grid args; return false when PlacementResult denies (see §7)
        }

        cell.zoneType = zoneType;
```

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** Replace placeholder comment arguments with real `assetId` resolution from `subTypeId` + `GridAssetCatalog` per §7 before merge.

---
## §Plan Digest

### §Goal

`PlacementValidator.CanPlace` rejects when catalog `base_cost_cents` for `assetId` exceeds treasury headroom using `EconomyManager.CanAfford` (or treasury helper already used for building spends). Returns `PlacementFailReason` value for unaffordable.

### §Acceptance

- [ ] Cost read from `GridAssetCatalog` snapshot indexes (not ad-hoc JSON)
- [ ] Uses existing economy afford/spend patterns from `EconomyManager` / `TreasuryFloorClampService`
- [ ] EditMode or unit coverage for afford vs deny; `npm run unity:compile-check` exits 0

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| afford_gate | treasury high vs low | allow / deny | Unity EditMode |
| compile_gate | n/a | exit 0 | `npm run unity:compile-check` |

### §Examples

| Treasury | base_cost_cents | Outcome |
|----------|-----------------|--------|
| 0 | 100 | Unaffordable |

### §Mechanical Steps

#### Step 1 — Document economy field source

**Goal:** Spec references DTO field name for implementer.

**Edits:**

- `ia/projects/TECH-691.md` — **before:** `|  |  |  |  |` — **after:** `| 2026-04-22 | Cost field | Use base_cost_cents from GridAssetCatalog economy row (see GridAssetCatalog.Dto.cs) |  |`

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** On validator failure, fix `spec:` pointer in `ia/backlog/TECH-691.yaml` then re-run gate.

#### Step 2 — Affordability branch in validator

**Goal:** Before returning allowed from `CanPlace`, ensure `economyManager.CanAfford(amount)` for resolved `base_cost_cents`.

**Edits:**

- `Assets/Scripts/Managers/GameManagers/PlacementValidator.cs` — **before:** `            return PlacementResult.Allowed();` — **after:**
```
            int cents = /* resolve base_cost_cents for assetId via GridAssetCatalog public API per §7 */;
            if (economyManager != null && cents > 0 && !economyManager.CanAfford(cents))
                return PlacementResult.Fail(PlacementFailReason.Unaffordable, "Insufficient treasury.");
            return PlacementResult.Allowed();
```

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** On compile error, complete TECH-689 `PlacementResult` / `PlacementFailReason` first, then re-open this edit block.

---
## §Plan Digest

### §Goal

`CanPlace` reads `unlocks_after` from catalog asset row; when tech unlock subsystem absent, document default-allow in Decision Log; when present, emit `PlacementFailReason.Locked`.

### §Acceptance

- [ ] Catalog row field `unlocks_after` consulted (`GridAssetCatalog.Dto.cs`)
- [ ] Decision Log records default-allow vs integrated behavior
- [ ] `npm run unity:compile-check` exits 0

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| unlock_stub | row with non-empty unlocks_after | allowed or locked per integration | EditMode or manual §8 |
| compile_gate | n/a | exit 0 | `npm run unity:compile-check` |

### §Examples

| unlocks_after | Tech system | Result |
|---------------|-------------|--------|
| empty | n/a | Allowed |
| "tech_x" | not wired | Allowed + Decision Log note |

### §Mechanical Steps

#### Step 1 — Anchor implementation plan

**Goal:** Tie unlock work to Phase 1 heading for searchability.

**Edits:**

- `ia/projects/TECH-692.md` — **before:** `### Phase 1 — Unlock stub` — **after:** `### Phase 1 — Unlock stub\n\n<!-- TECH-692: consult catalog row unlocks_after inside PlacementValidator.CanPlace per §7 -->`

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Fix `ia/backlog/TECH-692.yaml` `spec:` path if validator errors.

#### Step 2 — Validator unlock branch

**Goal:** Consult `unlocks_after` before economy + zoning outcomes (ordering per §7).

**Edits:**

- `Assets/Scripts/Managers/GameManagers/GridAssetCatalog.Dto.cs` — **before:** `    public string unlocks_after;` — **after:** `    public string unlocks_after; // PlacementValidator TECH-692 reads via catalog indexes`

**Gate:**

```bash
npm run unity:compile-check
```

**STOP:** If comment-only change is insufficient, implement real unlock lookup in `PlacementValidator` using public catalog query APIs from §7.


## Final gate

```bash
npm run validate:all
```
