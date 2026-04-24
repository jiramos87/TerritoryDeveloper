### Stage 12 — Save/load + landmarks hook + glossary/spec closeout / Save/load schema + restore pipeline

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Serialize + deserialize pool state against Bucket 3's v3 schema envelope. Wire restore into existing load pipeline after grid cells land. No new `schemaVersion` bump owned here — defer to Bucket 3.

**Exit:**

- `UtilityPoolsDto` + `PoolStateDto` structs serialized by `GameSaveManager`.
- Write path: serializes `UtilityPoolService[scale].pools` → `utilityPoolsData` per-scale per-kind `{net, ema, status}`.
- Read path: restore step 5 (after grid cells) — hydrates pools, THEN existing building-restore path re-instantiates infrastructure buildings which re-register via lifecycle hooks (no separate contributor-id persistence beyond building placements; contributor list rebuilds from buildings).
- Schema bump: code comment `// schemaVersion bump owned by Bucket 3 (zone-s-economy) — utilities stages against v3 envelope only` on the new section.
- Guard against reading `utilityPoolsData` when version < 3 — skip section, leave pools at `Healthy` default.
- Phase 1 — DTOs + write path.
- Phase 2 — Read path + restore hook.
- Phase 3 — Round-trip PlayMode test.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | PoolState DTOs | _pending_ | _pending_ | Add `Assets/Scripts/Data/Utilities/PoolStateDto.cs` + `UtilityPoolsDto.cs` — serializable mirrors of `PoolState` + `Dictionary<ScaleTag, Dictionary<UtilityKind, PoolStateDto>>`. |
| T12.2 | GameSaveManager write | _pending_ | _pending_ | Edit `GameSaveManager.cs` — serialize all three `UtilityPoolService.pools` into `utilityPoolsData` section. Comment pins Bucket 3 v3 schema coordination. |
| T12.3 | Load pipeline restore hook | _pending_ | _pending_ | Edit load pipeline — add restore step AFTER grid cells. Hydrate pool dictionaries; guard `if (saveData.schemaVersion >= 3)`. Note: building re-registration handled by existing building restore path, which runs before pool restore so contributor list rebuilds. Reorder if needed. |
| T12.4 | Contributor rebuild ordering verification | _pending_ | _pending_ | Verify existing building-restore path re-instantiates infrastructure building prefabs → `InfrastructureContributor.OnEnable` calls `registry.Register`. If ordering reversed, move pool-state restore AFTER building restore so registry repopulates first. Document final step number in `ia/specs/persistence-system.md`. |
| T12.5 | Save round-trip PlayMode test | _pending_ | _pending_ | Add `UtilitySaveRoundTripTests.cs` — place coal plant + water treatment, tick to Warning state, save, reload, assert `pools[Power].status == Warning`, contributor registry repopulated, `ExpansionFrozen` restored. |
