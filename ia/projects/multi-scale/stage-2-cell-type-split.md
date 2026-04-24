### Stage 2 — Parent-scale conceptual stubs / Cell-type split

**Status:** Final

**Objectives:** `Cell` → `CityCell` / `RegionCell` / `CountryCell`. City sim unchanged in behavior. Invariants #1 (`HeightMap` ↔ `Cell.height` sync) and #5 (`GetCell` only) preserved.

**Exit:**

- `Cell` base type (abstract class or interface) carries coord + shared primitives.
- `CityCell` carries all existing city-scale fields.
- `RegionCell` + `CountryCell` land as thin placeholders (coord + parent id refs; no behavior).
- City sim compiles + runs against `CityCell`. Zero behavior regression (testmode smoke).
- `GridManager` typed surface — generic `GetCell<T>(x,y)` or scale-indexed overloads; existing `GetCell(x,y)` back-compat defaults to `CityCell`.
- Glossary rows land for three cell types.
- Phase 1 — Base type extraction + `Cell` → `CityCell` rename (compile-only refactor).
- Phase 2 — `RegionCell` + `CountryCell` placeholder types + glossary rows.
- Phase 3 — `GridManager` typed surface + back-compat default.
- Phase 4 — Regression gate (`unity:compile-check` + testmode smoke + `HeightMap` integrity).

**Tasks:**


| Task | Name | Issue | Status | Intent |
| ------ | ------------------------ | ----------- | ------ | ------------------------------------------------------------------------------------------------------------ |
| T2.1 | Extract Cell base | **TECH-90** | Done | Extract `Cell` abstract base (coord, height, shared primitives). Compile-only; no rename yet. |
| T2.2 | Cell → CityCell rename | **TECH-91** | Done | Rename `Cell` → `CityCell` across all city sim files. Preserve `HeightMap` sync (invariant #1). |
| T2.3 | RegionCell placeholder | **TECH-92** | Done | `RegionCell` placeholder type (coord + parent-region-id; no behavior). Glossary row. |
| T2.4 | CountryCell placeholder | **TECH-93** | Done | `CountryCell` placeholder type (coord + parent-country-id; no behavior). Glossary rows for all 3 cell types. |
| T2.5 | GetCell generic overloads | **TECH-94** | Done | Generic `GetCell<T>(x,y)` or scale-indexed overloads on `GridManager`. Compile gate. |
| T2.6 | GetCell back-compat | **TECH-95** | Done | Back-compat `GetCell(x,y)` defaults to `CityCell`. Update all callers. Invariant #5 preserved. |
| T2.7 | City load smoke test | **TECH-96** | Done | Testmode smoke — city load + sim tick, no regression. |
| T2.8 | HeightMap integrity test | **TECH-97** | Done | Testmode assertion — `HeightMap` / `CityCell.height` integrity (invariant #1). |
