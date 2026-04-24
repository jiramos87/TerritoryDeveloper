### Stage 1 — Parent-scale conceptual stubs / Parent-scale identity fields

**Status:** Final

**Objectives:** city save + `GridManager` carry non-null `region_id` + `country_id` (placeholder GUIDs). Legacy saves migrate cleanly.

**Exit:**

- `GameSaveData` has non-null `region_id` + `country_id` (GUID).
- `GridManager` exposes read-only `ParentRegionId` / `ParentCountryId` set at load / new-game.
- Save/load round-trips both ids.
- Legacy saves migrate w/ placeholder ids; no data loss; save version bumped.
- Glossary rows land for **parent region id** + **parent country id**.
- Phase 1 — Schema + migration (data shape, version bump, legacy load path).
- Phase 2 — Runtime surface (`GridManager` properties + new-game placeholder allocation).
- Phase 3 — Round-trip + migration tests (testmode batch).

**Tasks:**


| Task | Name | Issue | Status | Intent |
| ------ | ----------------------- | ----------- | ------ | --------------------------------------------------------------------------------------------- |
| T1.1 | Parent-id fields | **TECH-87** | Done | `GameSaveData` parent-id fields + save version bump + legacy migration + glossary rows. |
| T1.2 | GridManager parent-id | **TECH-88** | Done | `GridManager` `ParentRegionId` / `ParentCountryId` surface + new-game placeholder allocation. |
| T1.3 | Round-trip migration | **TECH-89** | Done | Round-trip + legacy-migration tests (testmode batch scenario). |
