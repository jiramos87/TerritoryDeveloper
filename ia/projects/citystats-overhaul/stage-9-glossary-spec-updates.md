### Stage 9 — Multi-scale Rollup + Web Stats Surface / Glossary + spec updates

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Add canonical glossary rows for all new types; cross-link from existing entries; update `managers-reference §Helper Services` table. Validate IA clean.

**Exit:**

- `glossary_discover` returns **StatsFacade**, **ColumnarStatsStore**, **StatKey**, **IStatsReadModel** for relevant queries.
- **City metrics history** glossary entry updated to note `StatsFacade.SnapshotForBridge()` as data source.
- `ia/specs/managers-reference.md §Helper Services` table includes `CityStatsFacade`, `RegionStatsFacade`, `CountryStatsFacade` rows.
- `npm run validate:all` clean.
- Phase 1 — Add new glossary rows.
- Phase 2 — Cross-link existing entries + managers-reference update + validate:all.

**Tasks:**

| Task | Issue | Status | Intent |
| --- | --- | --- | --- |
| T9.1 | _pending_ | _pending_ | Add glossary row **StatsFacade**: "Typed read-model facade (`CityStatsFacade` / `RegionStatsFacade` / `CountryStatsFacade`) implementing `IStatsReadModel`; backed by `ColumnarStatsStore`; `[SerializeField]` Inspector-wired; exposes `BeginTick`/`Publish`/`Set`/`EndTick` + `GetScalar`/`GetSeries`/`SnapshotForBridge`." Category: City systems. Spec ref: `mgrs §Helper Services`. |
| T9.2 | _pending_ | _pending_ | Add glossary rows: **ColumnarStatsStore** ("Plain C# ring-buffer store keyed by `StatKey`; capacity 256 city / 64 dormant scale; `FlushToSeries()` on `EndTick`."), **StatKey** ("Enum of canonical metric identifiers shared across facade, store, and consumers."), **IStatsReadModel** ("Pull contract for facade consumers: `GetScalar`, `GetSeries`, `EnumerateRows`."); update **City metrics history** entry: append "Data sourced via `CityStatsFacade.SnapshotForBridge(tick)` since citystats-overhaul." |
| T9.3 | _pending_ | _pending_ | Cross-link in existing glossary: append to **Simulation tick** definition "Fires `CityStatsFacade.EndTick` on each tick (since citystats-overhaul)."; append to **Scale switch** definition "Triggers `RegionStatsFacade.Rollup(activeCities)` in save-leaving step (since citystats-overhaul).". Add `CityStatsFacade`, `RegionStatsFacade`, `CountryStatsFacade` rows to `ia/specs/managers-reference.md §Helper Services` table with role descriptions. |
| T9.4 | _pending_ | _pending_ | Run `npm run validate:all`; confirm zero errors on frontmatter + link checks. Run `npm run validate:web` to confirm web build still clean after Stage 3.2 additions. Report exit codes. |

---
