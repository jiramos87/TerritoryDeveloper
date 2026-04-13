---
purpose: "TECH-87 — Parent-scale identity fields on GameSaveData + save migration."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-87 — Parent-scale identity fields on `GameSaveData` + save migration

> **Issue:** [TECH-87](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-12
> **Last updated:** 2026-04-12
> **Orchestrator:** [`multi-scale-master-plan.md`](multi-scale-master-plan.md) — Step 1 / Stage 1.1 / Phase 1.

## 1. Summary

Add non-null `regionId` + `countryId` (GUID, string-serialized) to `GameSaveData`. Introduce save schema version constant (none exists today). **Legacy save** — pre-field save file — loads via migration w/ placeholder GUIDs. First surface of **parent-scale stub** identity in save data. No runtime behavior change beyond fields being present.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `GameSaveData` carries non-null `regionId` + `countryId` (GUID, string-serialized).
2. Save schema version constant introduced on `GameSaveData`; legacy save files load w/ placeholder ids.
3. Glossary rows land for **parent region id** + **parent country id**.
4. `npm run validate:all` + `npm run unity:compile-check` green; save round-trip covered by TECH-89.

### 2.2 Non-Goals

1. `GridManager` runtime surface (TECH-88).
2. Any city-sim consumer of parent ids.
3. Playable region / country scale.
4. Neighbor-city stub (Stage 1.3).
5. Cell-type split (Stage 1.2).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want every city save to carry parent region + country ids so downstream multi-scale work has a stable identity surface. | Save round-trip preserves ids; legacy save migrates cleanly. |

## 4. Current State

### 4.1 Domain behavior

`GameSaveData` has no parent-scale identity. City stands alone. No concept of which region / country owns it. No save schema version either.

### 4.2 Systems map

- `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` — carries `[Serializable] class GameSaveData` (lines 262–283) + serialize / deserialize orchestrator.
- `Assets/Scripts/Testing/TestModeSessionState.cs`, `Assets/Scripts/Testing/ScenarioPathResolver.cs`, `Assets/Scripts/Editor/ScenarioDescriptorBatchBuilder.cs` — other `GameSaveData` consumers (read-only; watch for compile breakage on field addition).
- `ia/specs/persistence-system.md` §Save / §Key files — canonical save schema doc (no `ia/specs/save-system.md` exists; backlog Files line is stale, to be corrected at implement time).
- `ia/specs/glossary.md` — add parent-id rows.

### 4.3 Implementation investigation notes

- No existing save-version constant in `GameSaveManager.cs` (grep confirms). This issue **introduces** `schemaVersion` (int) on `GameSaveData`, default `1` for new saves; missing field on deserialize → `0` → legacy path.
- GUID generation: `System.Guid.NewGuid().ToString()` — placeholder sufficient for MVP (see Open Question Q1 resolution).
- Migration path: deserialize → if `schemaVersion < 1` OR `regionId`/`countryId` null/empty → allocate `Guid.NewGuid().ToString()` for each missing id, set `schemaVersion = 1`, mark save dirty for next write.
- `GameSaveData` uses public-field Unity JSON serialization (Unity `JsonUtility`-style per other fields). New fields must be `public` + serializable types (use `string`, not `System.Guid`).

## 5. Proposed Design

### 5.1 Target behavior (product)

Every city save carries **parent region id** + **parent country id** (GUID, non-null). Placeholder ids allocated at new-game OR at first deserialize of a **legacy save**. Ids persist across save / load cycles. Zero runtime consumer yet (TECH-88 lands read surface; TECH-89 verifies round-trip).

### 5.2 Architecture / implementation

- `GameSaveData` new fields (public, Unity-serializable):
  - `public int schemaVersion;` — absent on legacy → deserializes to `0`.
  - `public string regionId;` — GUID string. Null/empty on legacy → placeholder assigned at migration.
  - `public string countryId;` — same rules as `regionId`.
- `GameSaveManager` migration hook — new helper `MigrateLoadedSaveData(GameSaveData data)` invoked post-deserialize, pre-restore:
  1. If `data.schemaVersion < 1` OR `string.IsNullOrEmpty(data.regionId)` → `data.regionId = Guid.NewGuid().ToString()`.
  2. Same rule for `data.countryId`.
  3. Set `data.schemaVersion = 1`.
- New-game path: `GameSaveData` constructor / initializer in new-game flow populates both ids w/ fresh GUIDs + `schemaVersion = 1` so no save ever writes empty ids.
- Non-null invariant: post-migration + post-new-game assertion — ids non-empty before `GameSaveData` leaves save manager.

### 5.3 Method / algorithm notes

Load pipeline order (per `persistence-system.md` §Load pipeline) unchanged; migration runs BEFORE step 1 (heightmap restore) — operates only on `GameSaveData` scalar fields, no grid/water touch. No invariant (#1 HeightMap sync, #5 GetCell, etc.) affected — zero grid / road / water writes.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-12 | GUID placeholders allocated at load, not at generation time | Simpler; legacy saves migrate w/o fixture authoring | Deterministic seed-derived ids (rejected — adds coupling to generator seed) |
| 2026-04-12 | Store as string, not `System.Guid` struct | Unity `JsonUtility` serialization compatibility; matches existing save field patterns | Binary GUID (rejected — serializer overhead) |
| 2026-04-12 | Introduce `schemaVersion` (int) rather than bump existing constant | None exists — grep on `GameSaveManager.cs` returns zero `version`/`SaveVersion` matches | Tag via separate marker file (rejected — adds FS coupling) |
| 2026-04-12 | Canonical reference spec is `persistence-system.md`, not `save-system.md` | Only `persistence-system.md` exists in `ia/specs/`; backlog Files line is stale | Create `save-system.md` (rejected — duplicates existing reference) |
| 2026-04-12 | Field names camelCase (`regionId`, `countryId`, `schemaVersion`) | Matches existing public-field convention on `GameSaveData` (`saveName`, `gridWidth`, `waterMapData`) | PascalCase properties (rejected — inconsistent w/ sibling fields + Unity serialization pattern) |

## 7. Implementation Plan

### Phase 1 — Schema fields

- [ ] Add `public int schemaVersion;`, `public string regionId;`, `public string countryId;` to `GameSaveData` (`Assets/Scripts/Managers/GameManagers/GameSaveManager.cs`, class body ~line 263).
- [ ] New-game initializer populates all three (`schemaVersion = 1`, fresh GUIDs) — locate constructor / entry point in `GameSaveManager` + `GameManager` new-game flow.
- [ ] `npm run unity:compile-check` green.

### Phase 2 — Legacy-save migration

- [ ] Add `MigrateLoadedSaveData(GameSaveData)` helper in `GameSaveManager`; invoke post-deserialize, pre-restore.
- [ ] Migration rules: `schemaVersion < 1` OR null/empty ids → allocate `Guid.NewGuid().ToString()`, set `schemaVersion = 1`.
- [ ] Post-migration assertion: both ids non-empty (log + throw or fallback — align w/ existing error-handling pattern in file).

### Phase 3 — Reference spec + glossary

- [ ] Glossary rows: **parent region id**, **parent country id** (category: Multi-scale simulation; specReference: `persist §Save`).
- [ ] Update `ia/specs/persistence-system.md` §Save w/ parent-id fields + `schemaVersion` note.
- [ ] Run `npm run validate:all`.

### Phase 4 — Handoff

- [ ] Confirm TECH-88 (`GridManager` read surface) + TECH-89 (testmode batch) can consume fields as-shipped.
- [ ] Note `BACKLOG.md` Files line discrepancy (stale `SaveSystem/` paths + `save-system.md`) in closeout digest for correction.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| IA / glossary / persistence-spec edits consistent | Node | `npm run validate:all` | Chains fixtures + indexes + dead-spec check |
| C# compiles post-field addition | Node | `npm run unity:compile-check` | `Assets/Scripts/Managers/GameManagers/GameSaveManager.cs` + consumers (`TestModeSessionState`, `ScenarioPathResolver`, `ScenarioDescriptorBatchBuilder`) |
| New-game save writes non-null `regionId` + `countryId` + `schemaVersion == 1` | Agent report | `npm run unity:testmode-batch` (TECH-89) | Scenario filed under TECH-89 |
| Save round-trip preserves parent ids | Agent report | `npm run unity:testmode-batch` (TECH-89) | Load → inspect → save → reload parity |
| Legacy save (`schemaVersion` absent) loads w/ placeholder ids | Agent report | `npm run unity:testmode-batch` (TECH-89) | Fixture: pre-TECH-87 save file in TECH-89 |
| Glossary rows present | Node | `npm run validate:all` (glossary index check) | Rows: **parent region id**, **parent country id** |

## 8. Acceptance Criteria

- [ ] `GameSaveData.regionId` + `.countryId` + `.schemaVersion` fields present, non-null/non-zero post-load for new saves.
- [ ] Legacy fixture (no `schemaVersion`, no ids) loads w/ placeholder GUIDs + `schemaVersion = 1` (test in TECH-89).
- [ ] Glossary rows land (**parent region id**, **parent country id**).
- [ ] `ia/specs/persistence-system.md` §Save updated w/ parent-id fields + `schemaVersion`.
- [ ] `npm run validate:all` + `npm run unity:compile-check` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. ~~Placeholder GUIDs assigned at legacy-save migration time vs at generation time of fresh saves — confirm both paths use the same allocation policy.~~ **Resolved (§5.2):** both paths use `Guid.NewGuid().ToString()`. New-game path allocates at `GameSaveData` init; legacy path allocates at `MigrateLoadedSaveData`. Same allocation primitive, different trigger.
2. ~~Should parent ids be exposed in any save-browser / debug UI at this stage?~~ **Resolved (Non-Goal §2.2):** strictly invisible at this stage. No UI surface until a future consumer issue files it.
3. **Deferred to implementer:** does any existing save-browser / scenario-fixture tooling deserialize `GameSaveData` directly (outside `GameSaveManager`)? Implementer must grep for `JsonUtility.FromJson<GameSaveData>` / `JsonConvert.Deserialize...GameSaveData` and route those through `MigrateLoadedSaveData` too — else fixture load paths skip migration.
