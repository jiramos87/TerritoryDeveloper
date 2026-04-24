### Stage 1 — Foundation: enum extension + floor-clamp treasury + envelope budget + save schema / ZoneType enum extension + sub-type registry

**Status:** Final

**Backlog state (Stage 1.1):** 6 filed (TECH-278..283 all Done (archived))

**Objectives:** Extend `ZoneType` with 6 new enum values (3 densities × Building/Zoning). Author `ZoneSubTypeRegistry` SO with 7 entries. Add `subTypeId` sidecar to `Zone` component. Land glossary rows for the new domain vocabulary (Zone S, ZoneSubTypeRegistry, envelope). Scaffolding only — no runtime logic consumes the new values yet.

**Exit:**

- 6 new `ZoneType` enum values added + existing consumers (`IsBuildingZone`, `IsZoningType`) updated + new `IsStateServiceZone` predicate on `EconomyManager`.
- `Zone.subTypeId` field (default -1) + serializable.
- `ZoneSubTypeRegistry` MonoBehaviour + `Assets/Resources/Economy/zone-sub-types.json` with 7 seeded entries.
- Glossary rows added: `Zone S`, `ZoneSubTypeRegistry`, `envelope (budget sense)`.
- `npm run unity:compile-check` green.
- Phase 1 — `ZoneType` enum + predicates + `Zone.subTypeId` sidecar.
- Phase 2 — `ZoneSubTypeRegistry` MonoBehaviour (JSON-loading) + `zone-sub-types.json` config.
- Phase 3 — Glossary + spec-index refresh.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Extend `ZoneType` enum + predicates | **TECH-278** | Done (archived) | Add `StateServiceLightBuilding`, `StateServiceMediumBuilding`, `StateServiceHeavyBuilding`, `StateServiceLightZoning`, `StateServiceMediumZoning`, `StateServiceHeavyZoning` to `Zone.ZoneType` enum in `Assets/Scripts/Managers/UnitManagers/Zone.cs`. Extend `EconomyManager.IsBuildingZone` + `IsZoningType` to include the new values. Add `IsStateServiceZone(Zone.ZoneType)` predicate on `EconomyManager`. No caller change yet. |
| T1.2 | Add `Zone.subTypeId` sidecar field | **TECH-279** | Done (archived) | Add `[SerializeField] private int subTypeId = -1;` to `Zone.cs` + public getter/setter. Default -1 means "RCI, no sub-type". Persists via existing Unity serialization — no save plumbing yet (save bump lands in Stage 1.3). |
| T1.3 | Author `ZoneSubTypeRegistry` MonoBehaviour (JSON-loading) | **TECH-280** | Done (archived) | `ZoneSubTypeRegistry : MonoBehaviour` at `Assets/Scripts/Managers/GameManagers/ZoneSubTypeRegistry.cs`. Loads `Assets/Resources/Economy/zone-sub-types.json` in `Awake` via `Resources.Load<TextAsset>` + `JsonUtility`. Entry fields: `int id`, `string displayName`, `string prefabPath`, `string iconPath`, `int baseCost`, `int monthlyUpkeep`. `GetById(int)` lookup. `LoadFromJson()` public for tests. |
| T1.4 | Seed `zone-sub-types.json` config file | **TECH-281** | Done (archived) | Author `Assets/Resources/Economy/zone-sub-types.json` with 7 entries (police=0…public offices=6). `prefabPath` + `iconPath` empty strings (art deferred). `baseCost` + `monthlyUpkeep` per exploration §IP-1. Human/agent edits costs directly in JSON; no Unity Editor required. |
| T1.5 | Glossary + spec-index refresh | **TECH-282** | Done (archived) | Add rows to `ia/specs/glossary.md` — `Zone S`, `ZoneSubTypeRegistry`, `envelope (budget sense)` — each with definition + authoritative spec link (points at forthcoming `ia/specs/economy-system.md`, cross-refs exploration doc for now). Run `npm run mcp-ia-index` to regenerate `tools/mcp-ia-server/data/glossary-index.json` + `glossary-graph-index.json`. |
| T1.6 | EditMode tests for enum + registry | **TECH-283** | Done (archived) | New test class `ZoneSubTypeRegistryTests` under `Assets/Tests/EditMode/Economy/` (new asmdef if needed). Cover: `GetById` returns correct entry for each of 7 ids, `GetById(-1)` returns null/throws, `IsStateServiceZone` true for 6 new enum values + false for R/C/I, `Zone.subTypeId` default -1 persists via serialization round-trip. |
